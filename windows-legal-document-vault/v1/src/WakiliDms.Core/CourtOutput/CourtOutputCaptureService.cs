using WakiliDms.Core.Common;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;

namespace WakiliDms.Core.CourtOutput;

public sealed class CourtOutputCaptureService
{
    private static readonly HashSet<DocumentType> AllowedTypes =
    [
        DocumentType.CourtOrder,
        DocumentType.Ruling,
        DocumentType.Judgment,
        DocumentType.FilingReceipt,
        DocumentType.PaymentReceipt,
        DocumentType.Notice
    ];

    private readonly DocumentImportService _documentImportService;

    public CourtOutputCaptureService(DocumentImportService documentImportService)
    {
        _documentImportService = documentImportService;
    }

    public async Task<Result<LegalDocument>> CaptureAsync(
        CourtOutputCaptureRequest request,
        CancellationToken cancellationToken)
    {
        if (!AllowedTypes.Contains(request.DocumentType))
        {
            return Result<LegalDocument>.Fail("Court output capture only accepts receipts, notices, orders, rulings, and judgments.");
        }

        return await _documentImportService.ImportAsync(
            new DocumentImportRequest(
                request.MatterId,
                request.SourceFilePath,
                request.VaultPath,
                request.RecoveryKey,
                request.DocumentType),
            cancellationToken);
    }
}
