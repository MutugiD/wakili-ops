namespace WakiliDms.Core.CloudBackup;

public sealed record CloudBackupStoredSnapshot(
    CloudBackupSnapshotMetadata Metadata,
    byte[] EncryptedPackageBytes);
