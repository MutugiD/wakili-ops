using System.Text.Json;
using WakiliDms.Core.CloudBackup;
using WakiliDms.Core.Common;

namespace WakiliDms.Infrastructure.CloudBackup;

public sealed class LocalFilesystemCloudBackupProvider : ICloudBackupProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _storageRootPath;

    public LocalFilesystemCloudBackupProvider(string storageRootPath)
    {
        if (string.IsNullOrWhiteSpace(storageRootPath))
        {
            throw new ArgumentException("Cloud backup storage root path is required.", nameof(storageRootPath));
        }

        _storageRootPath = storageRootPath;
    }

    public async Task<Result> UploadSnapshotAsync(
        CloudBackupSnapshotMetadata metadata,
        byte[] encryptedPackageBytes,
        CancellationToken cancellationToken)
    {
        if (metadata.InstallationId == Guid.Empty)
        {
            return Result.Fail("Installation ID is required for cloud backup upload.");
        }

        if (string.IsNullOrWhiteSpace(metadata.SnapshotId))
        {
            return Result.Fail("Snapshot ID is required for cloud backup upload.");
        }

        if (encryptedPackageBytes.Length == 0)
        {
            return Result.Fail("Encrypted cloud backup package is empty.");
        }

        try
        {
            var snapshotDirectory = SnapshotDirectory(metadata.InstallationId, metadata.SnapshotId);
            Directory.CreateDirectory(snapshotDirectory);
            await File.WriteAllBytesAsync(
                PackagePath(metadata.InstallationId, metadata.SnapshotId),
                encryptedPackageBytes,
                cancellationToken);

            await using var stream = File.Create(MetadataPath(metadata.InstallationId, metadata.SnapshotId));
            await JsonSerializer.SerializeAsync(stream, metadata, SerializerOptions, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Result.Fail($"Cloud backup upload failed: {ex.Message}");
        }
    }

    public async Task<Result<CloudBackupStoredSnapshot>> DownloadSnapshotAsync(
        Guid installationId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        if (installationId == Guid.Empty)
        {
            return Result<CloudBackupStoredSnapshot>.Fail("Installation ID is required for cloud backup download.");
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return Result<CloudBackupStoredSnapshot>.Fail("Snapshot ID is required for cloud backup download.");
        }

        var metadataPath = MetadataPath(installationId, snapshotId);
        var packagePath = PackagePath(installationId, snapshotId);
        if (!File.Exists(metadataPath) || !File.Exists(packagePath))
        {
            return Result<CloudBackupStoredSnapshot>.Fail("Cloud backup snapshot was not found.");
        }

        try
        {
            await using var metadataStream = File.OpenRead(metadataPath);
            var metadata = await JsonSerializer.DeserializeAsync<CloudBackupSnapshotMetadata>(
                metadataStream,
                SerializerOptions,
                cancellationToken);
            if (metadata is null)
            {
                return Result<CloudBackupStoredSnapshot>.Fail("Cloud backup metadata could not be read.");
            }

            var encryptedBytes = await File.ReadAllBytesAsync(packagePath, cancellationToken);
            return Result<CloudBackupStoredSnapshot>.Ok(new CloudBackupStoredSnapshot(metadata, encryptedBytes));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Result<CloudBackupStoredSnapshot>.Fail($"Cloud backup download failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<CloudBackupSnapshotMetadata>> ListSnapshotsAsync(
        Guid installationId,
        CancellationToken cancellationToken)
    {
        var installationDirectory = InstallationDirectory(installationId);
        if (!Directory.Exists(installationDirectory))
        {
            return [];
        }

        var snapshots = new List<CloudBackupSnapshotMetadata>();
        foreach (var metadataPath in Directory.EnumerateFiles(installationDirectory, "metadata.json", SearchOption.AllDirectories))
        {
            await using var stream = File.OpenRead(metadataPath);
            var metadata = await JsonSerializer.DeserializeAsync<CloudBackupSnapshotMetadata>(
                stream,
                SerializerOptions,
                cancellationToken);
            if (metadata is not null)
            {
                snapshots.Add(metadata);
            }
        }

        return snapshots
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ToList();
    }

    public Task<Result> DeleteSnapshotAsync(
        Guid installationId,
        string snapshotId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (installationId == Guid.Empty)
        {
            return Task.FromResult(Result.Fail("Installation ID is required for cloud backup delete."));
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return Task.FromResult(Result.Fail("Snapshot ID is required for cloud backup delete."));
        }

        var snapshotDirectory = SnapshotDirectory(installationId, snapshotId);
        if (!Directory.Exists(snapshotDirectory))
        {
            return Task.FromResult(Result.Fail("Cloud backup snapshot was not found."));
        }

        try
        {
            Directory.Delete(snapshotDirectory, recursive: true);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Result.Fail($"Cloud backup delete failed: {ex.Message}"));
        }
    }

    private string InstallationDirectory(Guid installationId)
    {
        return Path.Combine(_storageRootPath, installationId.ToString("D"));
    }

    private string SnapshotDirectory(Guid installationId, string snapshotId)
    {
        return Path.Combine(InstallationDirectory(installationId), SafeSnapshotId(snapshotId));
    }

    private string MetadataPath(Guid installationId, string snapshotId)
    {
        return Path.Combine(SnapshotDirectory(installationId, snapshotId), "metadata.json");
    }

    private string PackagePath(Guid installationId, string snapshotId)
    {
        return Path.Combine(SnapshotDirectory(installationId, snapshotId), "snapshot.package");
    }

    private static string SafeSnapshotId(string snapshotId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(snapshotId.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "snapshot" : sanitized;
    }
}
