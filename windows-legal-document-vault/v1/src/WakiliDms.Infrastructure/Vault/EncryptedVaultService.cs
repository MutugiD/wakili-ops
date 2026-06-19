using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WakiliDms.Core.Common;
using WakiliDms.Core.Vault;

namespace WakiliDms.Infrastructure.Vault;

public sealed class EncryptedVaultService : IVaultService
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int SaltSizeBytes = 32;
    private const int KdfIterations = 210_000;
    private const string VerificationText = "wakili-vault-check";
    private const string ManifestFileName = "vault.manifest.json";
    private const string ObjectsDirectoryName = "objects";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<Result> CreateVaultAsync(
        string vaultPath,
        string recoveryKey,
        CancellationToken cancellationToken)
    {
        var validation = ValidateVaultInputs(vaultPath, recoveryKey);
        if (!validation.Succeeded)
        {
            return validation;
        }

        Directory.CreateDirectory(vaultPath);
        Directory.CreateDirectory(ObjectsPath(vaultPath));

        var manifestPath = ManifestPath(vaultPath);
        if (File.Exists(manifestPath))
        {
            return Result.Fail("Vault already exists at this path.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = DeriveKey(recoveryKey, salt);

        try
        {
            var verification = EncryptBytes(Encoding.UTF8.GetBytes(VerificationText), key);
            var manifest = new VaultManifest
            {
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                KdfSalt = Convert.ToBase64String(salt),
                KdfIterations = KdfIterations,
                VerificationNonce = Convert.ToBase64String(verification.Nonce),
                VerificationTag = Convert.ToBase64String(verification.Tag),
                VerificationCiphertext = Convert.ToBase64String(verification.Ciphertext)
            };

            await WriteJsonAsync(manifestPath, manifest, cancellationToken);
            return Result.Ok();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public async Task<Result<StoredVaultObject>> StoreObjectAsync(
        string vaultPath,
        string recoveryKey,
        string originalName,
        byte[] plainBytes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(originalName))
        {
            return Result<StoredVaultObject>.Fail("Original file name is required.");
        }

        if (plainBytes.Length == 0)
        {
            return Result<StoredVaultObject>.Fail("Cannot store an empty object.");
        }

        var keyResult = await LoadAndVerifyKeyAsync(vaultPath, recoveryKey, cancellationToken);
        if (!keyResult.Succeeded || keyResult.Value is null)
        {
            return Result<StoredVaultObject>.Fail(keyResult.Error ?? "Vault unlock failed.");
        }

        var key = keyResult.Value;
        try
        {
            var encrypted = EncryptBytes(plainBytes, key);
            var objectId = Guid.NewGuid().ToString("N");
            var objectPath = ObjectPath(vaultPath, objectId);
            var payload = new EncryptedObjectPayload
            {
                ObjectId = objectId,
                OriginalName = Path.GetFileName(originalName),
                Sha256Hash = Convert.ToHexString(SHA256.HashData(plainBytes)),
                PlainLength = plainBytes.LongLength,
                CreatedAt = DateTimeOffset.UtcNow,
                Nonce = Convert.ToBase64String(encrypted.Nonce),
                Tag = Convert.ToBase64String(encrypted.Tag),
                Ciphertext = Convert.ToBase64String(encrypted.Ciphertext)
            };

            await WriteJsonAsync(objectPath, payload, cancellationToken);

            return Result<StoredVaultObject>.Ok(new StoredVaultObject(
                payload.ObjectId,
                payload.OriginalName,
                payload.Sha256Hash,
                payload.PlainLength,
                payload.CreatedAt));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public async Task<Result<byte[]>> ReadObjectAsync(
        string vaultPath,
        string recoveryKey,
        string objectId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return Result<byte[]>.Fail("Vault object ID is required.");
        }

        var keyResult = await LoadAndVerifyKeyAsync(vaultPath, recoveryKey, cancellationToken);
        if (!keyResult.Succeeded || keyResult.Value is null)
        {
            return Result<byte[]>.Fail(keyResult.Error ?? "Vault unlock failed.");
        }

        var key = keyResult.Value;
        try
        {
            var objectPath = ObjectPath(vaultPath, objectId);
            if (!File.Exists(objectPath))
            {
                return Result<byte[]>.Fail("Vault object was not found.");
            }

            var payload = await ReadJsonAsync<EncryptedObjectPayload>(objectPath, cancellationToken);
            if (payload is null)
            {
                return Result<byte[]>.Fail("Vault object metadata could not be read.");
            }

            var plainBytes = DecryptBytes(
                Convert.FromBase64String(payload.Nonce),
                Convert.FromBase64String(payload.Tag),
                Convert.FromBase64String(payload.Ciphertext),
                key);

            var actualHash = Convert.ToHexString(SHA256.HashData(plainBytes));
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(actualHash),
                    Encoding.ASCII.GetBytes(payload.Sha256Hash)))
            {
                return Result<byte[]>.Fail("Vault object integrity check failed.");
            }

            return Result<byte[]>.Ok(plainBytes);
        }
        catch (CryptographicException)
        {
            return Result<byte[]>.Fail("Vault object could not be decrypted.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static async Task<Result<byte[]>> LoadAndVerifyKeyAsync(
        string vaultPath,
        string recoveryKey,
        CancellationToken cancellationToken)
    {
        var validation = ValidateVaultInputs(vaultPath, recoveryKey);
        if (!validation.Succeeded)
        {
            return Result<byte[]>.Fail(validation.Error ?? "Vault input validation failed.");
        }

        var manifestPath = ManifestPath(vaultPath);
        if (!File.Exists(manifestPath))
        {
            return Result<byte[]>.Fail("Vault manifest was not found.");
        }

        var manifest = await ReadJsonAsync<VaultManifest>(manifestPath, cancellationToken);
        if (manifest is null)
        {
            return Result<byte[]>.Fail("Vault manifest could not be read.");
        }

        var salt = Convert.FromBase64String(manifest.KdfSalt);
        var key = DeriveKey(recoveryKey, salt);

        try
        {
            var plain = DecryptBytes(
                Convert.FromBase64String(manifest.VerificationNonce),
                Convert.FromBase64String(manifest.VerificationTag),
                Convert.FromBase64String(manifest.VerificationCiphertext),
                key);

            var expected = Encoding.UTF8.GetBytes(VerificationText);
            if (!CryptographicOperations.FixedTimeEquals(plain, expected))
            {
                CryptographicOperations.ZeroMemory(key);
                return Result<byte[]>.Fail("Recovery key did not unlock this vault.");
            }

            return Result<byte[]>.Ok(key);
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(key);
            return Result<byte[]>.Fail("Recovery key did not unlock this vault.");
        }
    }

    private static Result ValidateVaultInputs(string vaultPath, string recoveryKey)
    {
        if (string.IsNullOrWhiteSpace(vaultPath))
        {
            return Result.Fail("Vault path is required.");
        }

        if (string.IsNullOrWhiteSpace(recoveryKey))
        {
            return Result.Fail("Recovery key is required.");
        }

        return Result.Ok();
    }

    private static byte[] DeriveKey(string recoveryKey, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            recoveryKey,
            salt,
            KdfIterations,
            HashAlgorithmName.SHA256,
            KeySizeBytes);
    }

    private static EncryptedBytes EncryptBytes(byte[] plainBytes, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[plainBytes.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        return new EncryptedBytes(nonce, tag, ciphertext);
    }

    private static byte[] DecryptBytes(byte[] nonce, byte[] tag, byte[] ciphertext, byte[] key)
    {
        var plainBytes = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes);
        return plainBytes;
    }

    private static string ManifestPath(string vaultPath)
    {
        return Path.Combine(vaultPath, ManifestFileName);
    }

    private static string ObjectsPath(string vaultPath)
    {
        return Path.Combine(vaultPath, ObjectsDirectoryName);
    }

    private static string ObjectPath(string vaultPath, string objectId)
    {
        return Path.Combine(ObjectsPath(vaultPath), $"{objectId}.json");
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
    }

    private sealed record EncryptedBytes(byte[] Nonce, byte[] Tag, byte[] Ciphertext);

    private sealed record VaultManifest
    {
        public int Version { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public string KdfSalt { get; init; } = string.Empty;

        public int KdfIterations { get; init; }

        public string VerificationNonce { get; init; } = string.Empty;

        public string VerificationTag { get; init; } = string.Empty;

        public string VerificationCiphertext { get; init; } = string.Empty;
    }

    private sealed record EncryptedObjectPayload
    {
        public string ObjectId { get; init; } = string.Empty;

        public string OriginalName { get; init; } = string.Empty;

        public string Sha256Hash { get; init; } = string.Empty;

        public long PlainLength { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public string Nonce { get; init; } = string.Empty;

        public string Tag { get; init; } = string.Empty;

        public string Ciphertext { get; init; } = string.Empty;
    }
}
