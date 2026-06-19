namespace WakiliDms.Core.Licensing;

public sealed record BackupHealthSummary(
    DateTimeOffset? LastLocalBackupAt,
    DateTimeOffset? LastCloudBackupAt,
    string BackupStatus);
