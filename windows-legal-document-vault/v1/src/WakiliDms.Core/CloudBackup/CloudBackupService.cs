using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WakiliDms.Core.Common;
using WakiliDms.Core.Licensing;
using WakiliDms.Core.Setup;

namespace WakiliDms.Core.CloudBackup;

public sealed class CloudBackupService
{
    private const int KeySizeBytes = 32;
    private const int SaltSizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int KdfIterations = 250_000;
    private const string PackageFormat = "WakiliDmsCloudEncryptedSnapshotV1";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<Result<CloudBackupUploadResult>> UploadSnapshotAsync(
        CloudBackupUploadRequest request,
        ICloudBackupProvider provider,
        CancellationToken cancellationToken)
    {
        var validation = ValidateUpload(request);
        if (!validation.Succeeded)
        {
            return Result<CloudBackupUploadResult>.Fail(validation.Error ?? "Cloud backup upload request is invalid.");
        }

        var zipBytes = CreateSnapshotZip(request.LocalSnapshotDirectory);
        var encryptedPackageBytes = EncryptPackage(zipBytes, request.RecoveryKey);
        var metadata = new CloudBackupSnapshotMetadata(
            request.Settings.InstallationId,
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            encryptedPackageBytes.LongLength,
            Sha256(encryptedPackageBytes),
            "Uploaded");

        var upload = await provider.UploadSnapshotAsync(metadata, encryptedPackageBytes, cancellationToken);
        if (!upload.Succeeded)
        {
            return Result<CloudBackupUploadResult>.Fail(upload.Error ?? "Cloud backup upload failed.");
        }

        return Result<CloudBackupUploadResult>.Ok(new CloudBackupUploadResult(metadata));
    }

    public async Task<Result<CloudBackupDownloadResult>> DownloadSnapshotAsync(
        CloudBackupDownloadRequest request,
        ICloudBackupProvider provider,
        CancellationToken cancellationToken)
    {
        var validation = ValidateDownload(request);
        if (!validation.Succeeded)
        {
            return Result<CloudBackupDownloadResult>.Fail(validation.Error ?? "Cloud backup download request is invalid.");
        }

        var stored = await provider.DownloadSnapshotAsync(
            request.Settings.InstallationId,
            request.SnapshotId,
            cancellationToken);
        if (!stored.Succeeded || stored.Value is null)
        {
            return Result<CloudBackupDownloadResult>.Fail(stored.Error ?? "Cloud backup snapshot was not found.");
        }

        var actualHash = Sha256(stored.Value.EncryptedPackageBytes);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(actualHash),
                Encoding.ASCII.GetBytes(stored.Value.Metadata.Sha256Hash)))
        {
            return Result<CloudBackupDownloadResult>.Fail("Cloud backup snapshot hash mismatch.");
        }

        byte[] zipBytes;
        try
        {
            zipBytes = DecryptPackage(stored.Value.EncryptedPackageBytes, request.RecoveryKey);
        }
        catch (CryptographicException)
        {
            return Result<CloudBackupDownloadResult>.Fail("Cloud backup snapshot could not be decrypted with this recovery key.");
        }
        catch (JsonException)
        {
            return Result<CloudBackupDownloadResult>.Fail("Cloud backup snapshot package is invalid.");
        }

        int extractedCount;
        try
        {
            extractedCount = ExtractZipSafely(zipBytes, request.RestoreTargetPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Result<CloudBackupDownloadResult>.Fail($"Cloud backup snapshot could not be extracted: {ex.Message}");
        }

        return Result<CloudBackupDownloadResult>.Ok(new CloudBackupDownloadResult(
            request.RestoreTargetPath,
            stored.Value.Metadata,
            extractedCount));
    }

    private static Result ValidateUpload(CloudBackupUploadRequest request)
    {
        var entitlement = ValidateEntitlement(request.Settings);
        if (!entitlement.Succeeded)
        {
            return entitlement;
        }

        if (string.IsNullOrWhiteSpace(request.LocalSnapshotDirectory))
        {
            return Result.Fail("Local backup snapshot directory is required.");
        }

        if (!Directory.Exists(request.LocalSnapshotDirectory))
        {
            return Result.Fail("Local backup snapshot directory was not found.");
        }

        if (!File.Exists(Path.Combine(request.LocalSnapshotDirectory, "backup-manifest.json")))
        {
            return Result.Fail("Local backup snapshot manifest was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.RecoveryKey))
        {
            return Result.Fail("Recovery key is required for cloud backup encryption.");
        }

        return Result.Ok();
    }

    private static Result ValidateDownload(CloudBackupDownloadRequest request)
    {
        var entitlement = ValidateEntitlement(request.Settings);
        if (!entitlement.Succeeded)
        {
            return entitlement;
        }

        if (string.IsNullOrWhiteSpace(request.SnapshotId))
        {
            return Result.Fail("Cloud backup snapshot ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecoveryKey))
        {
            return Result.Fail("Recovery key is required to decrypt cloud backup.");
        }

        if (string.IsNullOrWhiteSpace(request.RestoreTargetPath))
        {
            return Result.Fail("Restore target path is required.");
        }

        return Result.Ok();
    }

    private static Result ValidateEntitlement(AppSettings settings)
    {
        if (settings.InstallationId == Guid.Empty)
        {
            return Result.Fail("Installation ID is required for cloud backup.");
        }

        if (!settings.CloudBackupEnabled)
        {
            return Result.Fail("Cloud backup is not enabled for this installation.");
        }

        if (settings.LicenseStatus is not (LicenseStatus.Active or LicenseStatus.Trial))
        {
            return Result.Fail("Cloud backup requires an active or trial license.");
        }

        return Result.Ok();
    }

    private static byte[] CreateSnapshotZip(string snapshotDirectory)
    {
        var snapshotRoot = Path.GetFullPath(snapshotDirectory);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var sourcePath in Directory.EnumerateFiles(snapshotRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(snapshotRoot, sourcePath).Replace('\\', '/');
                var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(sourcePath);
                fileStream.CopyTo(entryStream);
            }
        }

        return output.ToArray();
    }

    private static int ExtractZipSafely(byte[] zipBytes, string restoreTargetPath)
    {
        var targetRoot = Path.GetFullPath(restoreTargetPath);
        Directory.CreateDirectory(targetRoot);
        var extractedCount = 0;
        using var input = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var destinationPath = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
            if (!destinationPath.StartsWith(targetRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Cloud backup package contains an unsafe path: {entry.FullName}");
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            entry.ExtractToFile(destinationPath, overwrite: false);
            extractedCount++;
        }

        return extractedCount;
    }

    private static byte[] EncryptPackage(byte[] plainBytes, string recoveryKey)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = DeriveKey(recoveryKey, salt);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        try
        {
            using var aes = new AesGcm(key, tag.Length);
            aes.Encrypt(nonce, plainBytes, cipherText, tag);
            var package = new CloudEncryptedSnapshotPackage(
                PackageFormat,
                Convert.ToBase64String(salt),
                KdfIterations,
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(cipherText));

            return JsonSerializer.SerializeToUtf8Bytes(package, SerializerOptions);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DecryptPackage(byte[] encryptedPackageBytes, string recoveryKey)
    {
        var package = JsonSerializer.Deserialize<CloudEncryptedSnapshotPackage>(
            encryptedPackageBytes,
            SerializerOptions);
        if (package is null || package.Format != PackageFormat)
        {
            throw new JsonException("Unknown cloud backup snapshot format.");
        }

        var salt = Convert.FromBase64String(package.KdfSalt);
        var key = DeriveKey(recoveryKey, salt);
        try
        {
            var nonce = Convert.FromBase64String(package.Nonce);
            var tag = Convert.FromBase64String(package.Tag);
            var cipherText = Convert.FromBase64String(package.CipherText);
            var plainBytes = new byte[cipherText.Length];
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, cipherText, tag, plainBytes);
            return plainBytes;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DeriveKey(string recoveryKey, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(recoveryKey),
            salt,
            KdfIterations,
            HashAlgorithmName.SHA256,
            KeySizeBytes);
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed record CloudEncryptedSnapshotPackage(
        string Format,
        string KdfSalt,
        int KdfIterations,
        string Nonce,
        string Tag,
        string CipherText);
}
