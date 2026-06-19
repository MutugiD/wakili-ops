namespace WakiliDms.Core.Backup;

public sealed record RestoreDrillRequest(
    string BackupDirectory,
    string RestoreTargetPath,
    string RecoveryKey);
