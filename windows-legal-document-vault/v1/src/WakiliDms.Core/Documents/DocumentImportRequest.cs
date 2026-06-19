using WakiliDms.Core.Domain;

namespace WakiliDms.Core.Documents;

public sealed record DocumentImportRequest(
    Guid MatterId,
    string SourceFilePath,
    string VaultPath,
    string RecoveryKey,
    DocumentType DocumentType = DocumentType.Unknown);
