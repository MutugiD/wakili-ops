using WakiliDms.Core.Domain;

namespace WakiliDms.Core.Scan;

public interface IScanInboxRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task AddAsync(ScanInboxItem item, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string sourcePath, string sha256Hash, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScanInboxItem>> ListPendingAsync(CancellationToken cancellationToken);

    Task MarkImportedAsync(Guid scanInboxItemId, Guid documentId, DateTimeOffset importedAt, CancellationToken cancellationToken);
}
