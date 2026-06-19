using WakiliDms.Core.Domain;

namespace WakiliDms.Core.CourtOutput;

public sealed record CourtOutputCaptureRequest(
    Guid MatterId,
    string SourceFilePath,
    string VaultPath,
    string RecoveryKey,
    DocumentType DocumentType);
