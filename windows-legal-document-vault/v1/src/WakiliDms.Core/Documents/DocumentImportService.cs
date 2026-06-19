using WakiliDms.Core.Common;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Matter;
using WakiliDms.Core.Vault;

namespace WakiliDms.Core.Documents;

public sealed class DocumentImportService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc",
        ".docx",
        ".pdf"
    };

    private readonly IMatterRepository _matterRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IVaultService _vaultService;

    public DocumentImportService(
        IMatterRepository matterRepository,
        IDocumentRepository documentRepository,
        IVaultService vaultService)
    {
        _matterRepository = matterRepository;
        _documentRepository = documentRepository;
        _vaultService = vaultService;
    }

    public async Task<Result<LegalDocument>> ImportAsync(
        DocumentImportRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (!validation.Succeeded)
        {
            return Result<LegalDocument>.Fail(validation.Error ?? "Document import request is invalid.");
        }

        var matter = await _matterRepository.GetAsync(request.MatterId, cancellationToken);
        if (matter is null)
        {
            return Result<LegalDocument>.Fail("Matter was not found.");
        }

        var plainBytes = await File.ReadAllBytesAsync(request.SourceFilePath, cancellationToken);
        if (plainBytes.Length == 0)
        {
            return Result<LegalDocument>.Fail("Cannot import an empty document.");
        }

        var stored = await _vaultService.StoreObjectAsync(
            request.VaultPath,
            request.RecoveryKey,
            Path.GetFileName(request.SourceFilePath),
            plainBytes,
            cancellationToken);
        if (!stored.Succeeded || stored.Value is null)
        {
            return Result<LegalDocument>.Fail(stored.Error ?? "Document could not be stored in the vault.");
        }

        var document = LegalDocument.CreateImported(
            request.MatterId,
            stored.Value.OriginalName,
            stored.Value.ObjectId,
            stored.Value.Sha256Hash,
            stored.Value.PlainLength,
            request.DocumentType,
            stored.Value.CreatedAt);

        await _documentRepository.AddAsync(document, cancellationToken);
        return Result<LegalDocument>.Ok(document);
    }

    private static Result Validate(DocumentImportRequest request)
    {
        if (request.MatterId == Guid.Empty)
        {
            return Result.Fail("Matter ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceFilePath))
        {
            return Result.Fail("Source file path is required.");
        }

        if (!File.Exists(request.SourceFilePath))
        {
            return Result.Fail("Source document was not found.");
        }

        var extension = Path.GetExtension(request.SourceFilePath);
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Fail("Only DOC, DOCX, and PDF files can be imported in this slice.");
        }

        if (string.IsNullOrWhiteSpace(request.VaultPath))
        {
            return Result.Fail("Vault path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecoveryKey))
        {
            return Result.Fail("Recovery key is required.");
        }

        return Result.Ok();
    }
}
