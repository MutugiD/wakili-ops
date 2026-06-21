namespace WakiliDms.Core.CloudBackup;

public sealed record CloudBackupSnapshotMetadata(
    Guid InstallationId,
    string SnapshotId,
    DateTimeOffset CreatedAt,
    long EncryptedByteLength,
    string Sha256Hash,
    string Status);
