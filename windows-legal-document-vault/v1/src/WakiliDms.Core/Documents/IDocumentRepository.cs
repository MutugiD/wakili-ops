using WakiliDms.Core.Domain;

namespace WakiliDms.Core.Documents;

public interface IDocumentRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task AddAsync(LegalDocument document, CancellationToken cancellationToken);

    Task<IReadOnlyList<LegalDocument>> ListByMatterAsync(Guid matterId, CancellationToken cancellationToken);

    Task<LegalDocument?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateClassificationAsync(LegalDocument document, CancellationToken cancellationToken);
}
