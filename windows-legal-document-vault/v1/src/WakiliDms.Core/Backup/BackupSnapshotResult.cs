namespace WakiliDms.Core.Backup;

public sealed record BackupSnapshotResult(
    string BackupDirectory,
    string ManifestPath,
    int BackedUpFileCount,
    long BackedUpByteLength,
    DateTimeOffset CreatedAt);
