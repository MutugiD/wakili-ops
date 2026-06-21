using WakiliDms.Core.Common;

namespace WakiliDms.Core.CloudBackup;

public interface ICloudBackupProvider
{
    Task<Result> UploadSnapshotAsync(
        CloudBackupSnapshotMetadata metadata,
        byte[] encryptedPackageBytes,
        CancellationToken cancellationToken);

    Task<Result<CloudBackupStoredSnapshot>> DownloadSnapshotAsync(
        Guid installationId,
        string snapshotId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CloudBackupSnapshotMetadata>> ListSnapshotsAsync(
        Guid installationId,
        CancellationToken cancellationToken);

    Task<Result> DeleteSnapshotAsync(
        Guid installationId,
        string snapshotId,
        CancellationToken cancellationToken);
}
