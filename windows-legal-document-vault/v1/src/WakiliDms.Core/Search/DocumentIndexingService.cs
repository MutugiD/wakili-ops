using WakiliDms.Core.Common;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Vault;

namespace WakiliDms.Core.Search;

public sealed class DocumentIndexingService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IVaultService _vaultService;
    private readonly IDocumentTextExtractor _textExtractor;
    private readonly IDocumentSearchRepository _searchRepository;

    public DocumentIndexingService(
        IDocumentRepository documentRepository,
        IVaultService vaultService,
        IDocumentTextExtractor textExtractor,
        IDocumentSearchRepository searchRepository)
    {
        _documentRepository = documentRepository;
        _vaultService = vaultService;
        _textExtractor = textExtractor;
        _searchRepository = searchRepository;
    }

    public async Task<Result<int>> IndexDocumentAsync(
        Guid documentId,
        string vaultPath,
        string recoveryKey,
        CancellationToken cancellationToken)
    {
        if (documentId == Guid.Empty)
        {
            return Result<int>.Fail("Document ID is required.");
        }

        var document = await _documentRepository.GetAsync(documentId, cancellationToken);
        if (document is null)
        {
            return Result<int>.Fail("Document was not found.");
        }

        var bytes = await _vaultService.ReadObjectAsync(
            vaultPath,
            recoveryKey,
            document.VaultObjectId,
            cancellationToken);
        if (!bytes.Succeeded || bytes.Value is null)
        {
            return Result<int>.Fail(bytes.Error ?? "Document bytes could not be read from the vault.");
        }

        var extracted = await _textExtractor.ExtractTextAsync(
            document.OriginalFileName,
            bytes.Value,
            cancellationToken);
        if (!extracted.Succeeded || string.IsNullOrWhiteSpace(extracted.Value))
        {
            return Result<int>.Fail(extracted.Error ?? "Document text could not be extracted.");
        }

        await _searchRepository.UpsertAsync(
            new DocumentTextIndexEntry(
                document.Id,
                document.MatterId,
                document.OriginalFileName,
                extracted.Value,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return Result<int>.Ok(extracted.Value.Length);
    }
}
