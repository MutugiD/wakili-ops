namespace WakiliDms.Core.Backup;

public sealed record BackupRetentionCandidate(
    string SnapshotId,
    string BackupDirectory,
    DateTimeOffset CreatedAt,
    string Reason);

