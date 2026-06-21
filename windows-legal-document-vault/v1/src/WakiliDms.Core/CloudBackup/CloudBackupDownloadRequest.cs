using WakiliDms.Core.Setup;

namespace WakiliDms.Core.CloudBackup;

public sealed record CloudBackupDownloadRequest(
    AppSettings Settings,
    string SnapshotId,
    string RecoveryKey,
    string RestoreTargetPath);
