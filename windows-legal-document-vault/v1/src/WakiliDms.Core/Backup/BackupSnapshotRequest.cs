namespace WakiliDms.Core.Backup;

public sealed record BackupSnapshotRequest(
    string VaultPath,
    string DatabasePath,
    string BackupTargetPath,
    string RecoveryKey);
