namespace WakiliDms.Core.Backup;

public sealed record BackupRetentionPolicy(
    int KeepLatestCount,
    int DeleteOlderThanDays);

