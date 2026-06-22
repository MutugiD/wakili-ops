using WakiliDms.Core.CloudBackup;

namespace WakiliDms.App.ViewModels;

public sealed class CloudBackupSnapshotViewModel
{
    public CloudBackupSnapshotViewModel(CloudBackupSnapshotMetadata metadata)
    {
        Metadata = metadata;
    }

    public CloudBackupSnapshotMetadata Metadata { get; }

    public string SnapshotId => Metadata.SnapshotId;

    public string CreatedAt => Metadata.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string EncryptedByteLength => $"{Metadata.EncryptedByteLength:N0} encrypted bytes";

    public string Status => Metadata.Status;
}
