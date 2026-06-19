namespace WakiliDms.Core.Search;

public sealed record DocumentTextIndexEntry(
    Guid DocumentId,
    Guid MatterId,
    string OriginalFileName,
    string TextContent,
    DateTimeOffset IndexedAt);
