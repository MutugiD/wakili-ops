using WakiliDms.Core.Setup;

namespace WakiliDms.Core.CloudBackup;

public sealed record CloudBackupUploadRequest(
    AppSettings Settings,
    string LocalSnapshotDirectory,
    string RecoveryKey);
