namespace WakiliDms.Core.Licensing;

public sealed record InstallationCheckInPayload(
    Guid InstallationId,
    string LicenseKey,
    string FirmDisplayName,
    string DeviceNickname,
    string AppVersion,
    LicenseStatus LicenseStatus,
    bool Enabled,
    FeatureEntitlements Features,
    BackupHealthSummary Health,
    DateTimeOffset CheckedInAt);
