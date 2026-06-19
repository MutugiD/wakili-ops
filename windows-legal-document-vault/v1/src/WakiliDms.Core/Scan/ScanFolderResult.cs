namespace WakiliDms.Core.Scan;

public sealed record ScanFolderResult(int QueuedCount, int DuplicateCount, int IgnoredCount);
