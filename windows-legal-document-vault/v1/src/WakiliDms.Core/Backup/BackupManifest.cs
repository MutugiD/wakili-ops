namespace WakiliDms.Core.Backup;

internal sealed record BackupManifest(
    int ManifestVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<BackupManifestEntry> Entries);

internal sealed record BackupManifestEntry(
    string RelativePath,
    string Kind,
    long ByteLength,
    string Sha256Hash);
