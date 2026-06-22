namespace WakiliDms.Core.Backup;

public sealed record LocalBackupSnapshotSummary(
    string BackupDirectory,
    string SnapshotId,
    DateTimeOffset CreatedAt,
    int FileCount,
    long ByteLength);

