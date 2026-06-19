using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WakiliDms.Core.Common;

namespace WakiliDms.Core.Backup;

public sealed class BackupSnapshotService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<Result<BackupSnapshotResult>> CreateSnapshotAsync(
        BackupSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (!validation.Succeeded)
        {
            return Result<BackupSnapshotResult>.Fail(validation.Error ?? "Backup snapshot request is invalid.");
        }

        var createdAt = DateTimeOffset.UtcNow;
        var backupDirectory = CreateBackupDirectory(request.BackupTargetPath, createdAt);
        var entries = new List<BackupManifestEntry>();

        try
        {
            CopyVaultFiles(request.VaultPath, backupDirectory, entries);
            EncryptDatabase(request.DatabasePath, request.RecoveryKey, backupDirectory, entries);

            var manifest = new BackupManifest(
                ManifestVersion: 1,
                CreatedAt: createdAt,
                Entries: entries);

            var manifestPath = Path.Combine(backupDirectory, "backup-manifest.json");
            await using (var manifestStream = File.Create(manifestPath))
            {
                await JsonSerializer.SerializeAsync(
                    manifestStream,
                    manifest,
                    SerializerOptions,
                    cancellationToken);
            }

            return Result<BackupSnapshotResult>.Ok(new BackupSnapshotResult(
                backupDirectory,
                manifestPath,
                entries.Count,
                entries.Sum(entry => entry.ByteLength),
                createdAt));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Result<BackupSnapshotResult>.Fail($"Backup snapshot failed: {ex.Message}");
        }
    }

    internal static async Task<BackupManifest?> ReadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<BackupManifest>(
            stream,
            SerializerOptions,
            cancellationToken);
    }

    internal static async Task<EncryptedBackupFile?> ReadEncryptedBackupFileAsync(
        string encryptedFilePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(encryptedFilePath);
        return await JsonSerializer.DeserializeAsync<EncryptedBackupFile>(
            stream,
            SerializerOptions,
            cancellationToken);
    }

    internal static string Sha256ForFile(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private static Result Validate(BackupSnapshotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VaultPath))
        {
            return Result.Fail("Vault path is required for backup.");
        }

        if (!Directory.Exists(request.VaultPath))
        {
            return Result.Fail("Vault path was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.DatabasePath))
        {
            return Result.Fail("Database path is required for backup.");
        }

        if (!File.Exists(request.DatabasePath))
        {
            return Result.Fail("Database file was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.BackupTargetPath))
        {
            return Result.Fail("Backup target path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecoveryKey))
        {
            return Result.Fail("Recovery key is required to encrypt backup metadata.");
        }

        if (PathEqualsOrIsInside(request.BackupTargetPath, request.VaultPath))
        {
            return Result.Fail("Backup target cannot be inside the encrypted vault folder.");
        }

        return Result.Ok();
    }

    private static bool PathEqualsOrIsInside(string candidatePath, string parentPath)
    {
        var candidateFullPath = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parentFullPath = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return candidateFullPath.Equals(parentFullPath, StringComparison.OrdinalIgnoreCase)
            || candidateFullPath.StartsWith(parentFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || candidateFullPath.StartsWith(parentFullPath + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateBackupDirectory(string backupTargetPath, DateTimeOffset createdAt)
    {
        Directory.CreateDirectory(backupTargetPath);
        var baseDirectoryName = $"vault-backup-{createdAt:yyyyMMdd-HHmmss}";
        var backupDirectory = Path.Combine(backupTargetPath, baseDirectoryName);
        var suffix = 1;
        while (Directory.Exists(backupDirectory))
        {
            backupDirectory = Path.Combine(backupTargetPath, $"{baseDirectoryName}-{suffix}");
            suffix++;
        }

        Directory.CreateDirectory(backupDirectory);
        return backupDirectory;
    }

    private static void CopyVaultFiles(
        string vaultPath,
        string backupDirectory,
        List<BackupManifestEntry> entries)
    {
        var vaultRoot = Path.GetFullPath(vaultPath);
        var backupVaultRoot = Path.Combine(backupDirectory, "vault");
        foreach (var sourcePath in Directory.EnumerateFiles(vaultRoot, "*", SearchOption.AllDirectories))
        {
            var relativeVaultPath = Path.GetRelativePath(vaultRoot, sourcePath);
            var backupRelativePath = Path.Combine("vault", relativeVaultPath);
            CopyFileWithManifestEntry(sourcePath, Path.Combine(backupDirectory, backupRelativePath), backupRelativePath, "vault", entries);
        }
    }

    private static void EncryptDatabase(
        string databasePath,
        string recoveryKey,
        string backupDirectory,
        List<BackupManifestEntry> entries)
    {
        var destinationPath = Path.Combine(backupDirectory, "data", "wakili-dms.db.backup");
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var plainBytes = File.ReadAllBytes(databasePath);
        var encrypted = EncryptBytes(plainBytes, recoveryKey);
        File.WriteAllText(destinationPath, JsonSerializer.Serialize(encrypted, SerializerOptions));
        var fileInfo = new FileInfo(destinationPath);
        entries.Add(new BackupManifestEntry(
            Path.Combine("data", "wakili-dms.db.backup").Replace('\\', '/'),
            "encrypted-database",
            fileInfo.Length,
            Sha256ForFile(destinationPath)));
    }

    private static void CopyFileWithManifestEntry(
        string sourcePath,
        string destinationPath,
        string backupRelativePath,
        string kind,
        List<BackupManifestEntry> entries)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        File.Copy(sourcePath, destinationPath, overwrite: false);
        var fileInfo = new FileInfo(destinationPath);
        entries.Add(new BackupManifestEntry(
            backupRelativePath.Replace('\\', '/'),
            kind,
            fileInfo.Length,
            Sha256ForFile(destinationPath)));
    }

    internal static byte[] DecryptBytes(EncryptedBackupFile encrypted, string recoveryKey)
    {
        var salt = Convert.FromBase64String(encrypted.KdfSalt);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(recoveryKey),
            salt,
            250_000,
            HashAlgorithmName.SHA256,
            32);
        var nonce = Convert.FromBase64String(encrypted.Nonce);
        var tag = Convert.FromBase64String(encrypted.Tag);
        var cipherText = Convert.FromBase64String(encrypted.CipherText);
        var plainBytes = new byte[cipherText.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);
        return plainBytes;
    }

    private static EncryptedBackupFile EncryptBytes(byte[] plainBytes, string recoveryKey)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(recoveryKey),
            salt,
            250_000,
            HashAlgorithmName.SHA256,
            32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        return new EncryptedBackupFile(
            Format: "WakiliDmsEncryptedBackupFileV1",
            KdfSalt: Convert.ToBase64String(salt),
            Nonce: Convert.ToBase64String(nonce),
            Tag: Convert.ToBase64String(tag),
            CipherText: Convert.ToBase64String(cipherText));
    }
}

internal sealed record EncryptedBackupFile(
    string Format,
    string KdfSalt,
    string Nonce,
    string Tag,
    string CipherText);
