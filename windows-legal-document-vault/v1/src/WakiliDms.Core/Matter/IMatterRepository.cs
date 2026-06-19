using WakiliDms.Core.Domain;

namespace WakiliDms.Core.Matter;

public interface IMatterRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task AddAsync(Domain.Matter matter, CancellationToken cancellationToken);

    Task<IReadOnlyList<Domain.Matter>> ListAsync(CancellationToken cancellationToken);

    Task<Domain.Matter?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(Domain.Matter matter, CancellationToken cancellationToken);
}
