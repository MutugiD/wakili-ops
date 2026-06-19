using WakiliDms.Core.Domain;

namespace WakiliDms.Core.Documents;

public interface IDocumentVersionRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task AddAsync(DocumentVersion version, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentVersion>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken);
}
