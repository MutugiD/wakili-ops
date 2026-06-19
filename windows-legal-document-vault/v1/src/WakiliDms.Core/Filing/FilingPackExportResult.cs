namespace WakiliDms.Core.Filing;

public sealed record FilingPackExportResult(
    string ExportDirectory,
    string ManifestPath,
    string ChecklistPath,
    int ExportedDocumentCount,
    IReadOnlyList<string> Warnings);
