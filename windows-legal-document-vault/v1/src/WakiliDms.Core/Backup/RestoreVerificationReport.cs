namespace WakiliDms.Core.Backup;

public sealed record RestoreVerificationReport(
    int ReportVersion,
    DateTimeOffset CreatedAt,
    string SourceKind,
    string SourceIdentifier,
    string RestoreDirectory,
    int VerifiedFileCount,
    long RestoredByteLength,
    string PrivacyNote);

