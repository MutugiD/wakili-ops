namespace WakiliDms.Core.CloudBackup;

public sealed record CloudBackupDownloadResult(
    string RestoreTargetPath,
    CloudBackupSnapshotMetadata Metadata,
    int ExtractedFileCount);
