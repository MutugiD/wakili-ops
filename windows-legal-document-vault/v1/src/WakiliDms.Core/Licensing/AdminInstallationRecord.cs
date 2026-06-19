namespace WakiliDms.Core.Licensing;

public sealed record AdminInstallationRecord(
    Guid InstallationId,
    string FirmDisplayName,
    string DeviceNickname,
    string LicenseKey,
    string AppVersion,
    LicenseStatus LicenseStatus,
    bool CloudBackupEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastCheckInAt,
    DateTimeOffset UpdatedAt,
    string SupportNotes);
