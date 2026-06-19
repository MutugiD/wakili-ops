namespace WakiliDms.Core.Filing;

public sealed record FilingPackExportRequest(
    Guid MatterId,
    IReadOnlyList<Guid> DocumentIds,
    string VaultPath,
    string RecoveryKey,
    string ExportRootPath);
