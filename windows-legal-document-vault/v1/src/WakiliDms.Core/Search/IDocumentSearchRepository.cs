namespace WakiliDms.Core.Search;

public interface IDocumentSearchRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task UpsertAsync(DocumentTextIndexEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        Guid matterId,
        string query,
        CancellationToken cancellationToken);
}
