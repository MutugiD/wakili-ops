using System.Text.Json;
using WakiliDms.Core.Common;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Matter;
using WakiliDms.Core.Vault;

namespace WakiliDms.Core.Filing;

public sealed class FilingPackExportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IMatterRepository _matterRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IVaultService _vaultService;

    public FilingPackExportService(
        IMatterRepository matterRepository,
        IDocumentRepository documentRepository,
        IVaultService vaultService)
    {
        _matterRepository = matterRepository;
        _documentRepository = documentRepository;
        _vaultService = vaultService;
    }

    public async Task<Result<FilingPackExportResult>> ExportAsync(
        FilingPackExportRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (!validation.Succeeded)
        {
            return Result<FilingPackExportResult>.Fail(validation.Error ?? "Filing pack export request is invalid.");
        }

        var matter = await _matterRepository.GetAsync(request.MatterId, cancellationToken);
        if (matter is null)
        {
            return Result<FilingPackExportResult>.Fail("Matter was not found.");
        }

        var matterDocuments = await _documentRepository.ListByMatterAsync(request.MatterId, cancellationToken);
        var selectedDocuments = matterDocuments
            .Where(document => request.DocumentIds.Contains(document.Id))
            .OrderBy(document => document.ImportedAt)
            .ToList();
        if (selectedDocuments.Count == 0)
        {
            return Result<FilingPackExportResult>.Fail("No matching matter documents were selected for export.");
        }

        var exportDirectory = Path.Combine(
            request.ExportRootPath,
            $"{SanitizeFileName(matter.Name)}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(exportDirectory);

        var exportedFiles = new List<FilingPackManifestDocument>();
        var index = 1;
        foreach (var document in selectedDocuments)
        {
            var read = await _vaultService.ReadObjectAsync(
                request.VaultPath,
                request.RecoveryKey,
                document.VaultObjectId,
                cancellationToken);
            if (!read.Succeeded || read.Value is null)
            {
                return Result<FilingPackExportResult>.Fail(read.Error ?? $"Could not read {document.OriginalFileName} from the vault.");
            }

            var exportFileName = $"{index:00}-{SanitizeFileName(document.OriginalFileName)}";
            var exportPath = Path.Combine(exportDirectory, exportFileName);
            await File.WriteAllBytesAsync(exportPath, read.Value, cancellationToken);
            exportedFiles.Add(new FilingPackManifestDocument(
                document.Id,
                document.OriginalFileName,
                exportFileName,
                document.DocumentType.ToString(),
                document.Status.ToString(),
                document.Sha256Hash,
                document.ByteLength));
            index++;
        }

        var manifestPath = Path.Combine(exportDirectory, "filing-pack-manifest.json");
        var manifest = new FilingPackManifest(
            matter.Id,
            matter.Name,
            matter.CourtCaseNumber,
            DateTimeOffset.UtcNow,
            exportedFiles);
        await using (var stream = File.Create(manifestPath))
        {
            await JsonSerializer.SerializeAsync(stream, manifest, SerializerOptions, cancellationToken);
        }

        var checklistPath = Path.Combine(exportDirectory, "filing-readiness-checklist.txt");
        await File.WriteAllLinesAsync(
            checklistPath,
            BuildChecklist(matter.Name, exportedFiles.Count),
            cancellationToken);

        return Result<FilingPackExportResult>.Ok(new FilingPackExportResult(
            exportDirectory,
            manifestPath,
            checklistPath,
            exportedFiles.Count,
            ["Export folder is not encrypted. Upload or move files carefully, then delete temporary copies when done."]));
    }

    private static Result Validate(FilingPackExportRequest request)
    {
        if (request.MatterId == Guid.Empty)
        {
            return Result.Fail("Matter ID is required.");
        }

        if (request.DocumentIds.Count == 0)
        {
            return Result.Fail("At least one document must be selected for export.");
        }

        if (string.IsNullOrWhiteSpace(request.VaultPath))
        {
            return Result.Fail("Vault path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecoveryKey))
        {
            return Result.Fail("Recovery key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ExportRootPath))
        {
            return Result.Fail("Export root path is required.");
        }

        return Result.Ok();
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
    }

    private static string[] BuildChecklist(string matterName, int documentCount)
    {
        return
        [
            $"Filing readiness checklist for {matterName}",
            $"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm}",
            $"Documents exported: {documentCount}",
            "",
            "[ ] Confirm document order matches the intended e-filing sequence.",
            "[ ] Confirm every exported document is final for filing.",
            "[ ] Confirm signatures, commissioning, annexure markings, and dates.",
            "[ ] Confirm court, case number, parties, and filing category.",
            "[ ] Upload through the official Judiciary e-filing portal manually.",
            "[ ] Save portal receipts back into the vault after filing.",
            "[ ] Delete temporary export copies if no longer needed."
        ];
    }

    private sealed record FilingPackManifest(
        Guid MatterId,
        string MatterName,
        string? CourtCaseNumber,
        DateTimeOffset CreatedAt,
        IReadOnlyList<FilingPackManifestDocument> Documents);

    private sealed record FilingPackManifestDocument(
        Guid DocumentId,
        string OriginalFileName,
        string ExportedFileName,
        string DocumentType,
        string Status,
        string Sha256Hash,
        long ByteLength);
}
