namespace WakiliDms.Core.Backup;

public sealed record RestoreDrillResult(
    string RestoreDirectory,
    string ManifestPath,
    int VerifiedFileCount,
    long RestoredByteLength);
