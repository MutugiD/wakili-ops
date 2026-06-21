using WakiliDms.Core.Common;

namespace WakiliDms.Core.Backup;

public sealed class RestoreDrillService
{
    public async Task<Result<RestoreDrillResult>> RunAsync(
        RestoreDrillRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (!validation.Succeeded)
        {
            return Result<RestoreDrillResult>.Fail(validation.Error ?? "Restore drill request is invalid.");
        }

        var backupDirectory = Path.GetFullPath(request.BackupDirectory);
        var manifestPath = Path.Combine(backupDirectory, "backup-manifest.json");

        try
        {
            var manifest = await BackupSnapshotService.ReadManifestAsync(manifestPath, cancellationToken);
            if (manifest is null)
            {
                return Result<RestoreDrillResult>.Fail("Backup manifest could not be read.");
            }

            if (Directory.Exists(request.RestoreTargetPath))
            {
                Directory.Delete(request.RestoreTargetPath, recursive: true);
            }

            Directory.CreateDirectory(request.RestoreTargetPath);
            foreach (var entry in manifest.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var backupEntryPath = Path.GetFullPath(Path.Combine(backupDirectory, entry.RelativePath));
                if (!PathEqualsOrIsInside(backupEntryPath, backupDirectory))
                {
                    return Result<RestoreDrillResult>.Fail($"Backup manifest contains an unsafe path: {entry.RelativePath}");
                }

                if (!File.Exists(backupEntryPath))
                {
                    return Result<RestoreDrillResult>.Fail($"Backup file is missing: {entry.RelativePath}");
                }

                var backupHash = BackupSnapshotService.Sha256ForFile(backupEntryPath);
                if (!string.Equals(backupHash, entry.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<RestoreDrillResult>.Fail($"Backup hash mismatch: {entry.RelativePath}");
                }

                var restorePath = Path.Combine(request.RestoreTargetPath, entry.RelativePath);
                var restoreDirectory = Path.GetDirectoryName(restorePath);
                if (!string.IsNullOrWhiteSpace(restoreDirectory))
                {
                    Directory.CreateDirectory(restoreDirectory);
                }

                File.Copy(backupEntryPath, restorePath, overwrite: false);
                var restoreHash = BackupSnapshotService.Sha256ForFile(restorePath);
                if (!string.Equals(restoreHash, entry.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<RestoreDrillResult>.Fail($"Restore hash mismatch: {entry.RelativePath}");
                }

                if (string.Equals(entry.Kind, "encrypted-database", StringComparison.OrdinalIgnoreCase))
                {
                    var encrypted = await BackupSnapshotService.ReadEncryptedBackupFileAsync(backupEntryPath, cancellationToken);
                    if (encrypted is null)
                    {
                        return Result<RestoreDrillResult>.Fail("Encrypted database backup could not be read.");
                    }

                    var plainDatabaseBytes = BackupSnapshotService.DecryptBytes(encrypted, request.RecoveryKey);
                    if (plainDatabaseBytes.Length == 0)
                    {
                        return Result<RestoreDrillResult>.Fail("Encrypted database backup decrypted to an empty file.");
                    }

                    Array.Clear(plainDatabaseBytes);
                }
            }

            return Result<RestoreDrillResult>.Ok(new RestoreDrillResult(
                Path.GetFullPath(request.RestoreTargetPath),
                manifestPath,
                manifest.Entries.Count,
                manifest.Entries.Sum(entry => entry.ByteLength)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or System.Security.Cryptography.CryptographicException)
        {
            return Result<RestoreDrillResult>.Fail($"Restore drill failed: {ex.Message}");
        }
    }

    private static Result Validate(RestoreDrillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BackupDirectory))
        {
            return Result.Fail("Backup directory is required for restore drill.");
        }

        if (!Directory.Exists(request.BackupDirectory))
        {
            return Result.Fail("Backup directory was not found.");
        }

        if (!File.Exists(Path.Combine(request.BackupDirectory, "backup-manifest.json")))
        {
            return Result.Fail("Backup manifest was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.RestoreTargetPath))
        {
            return Result.Fail("Restore target path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecoveryKey))
        {
            return Result.Fail("Recovery key is required for restore drill verification.");
        }

        var backupDirectory = Path.GetFullPath(request.BackupDirectory);
        var restoreTargetPath = Path.GetFullPath(request.RestoreTargetPath);
        if (PathEqualsOrIsInside(backupDirectory, restoreTargetPath))
        {
            return Result.Fail("Restore target cannot be the backup directory or a parent of the backup directory.");
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
}
