namespace WakiliDms.Core.Search;

public sealed record DocumentSearchResult(
    Guid DocumentId,
    Guid MatterId,
    string OriginalFileName,
    string Snippet,
    double Rank);
