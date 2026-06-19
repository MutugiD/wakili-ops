using WakiliDms.Core.Setup;

namespace WakiliDms.Core.Licensing;

public sealed class InstallationControlService
{
    public AppSettings EnsureInstallationIdentity(
        AppSettings settings,
        DateTimeOffset now)
    {
        var installationId = settings.InstallationId == Guid.Empty
            ? Guid.NewGuid()
            : settings.InstallationId;
        var deviceNickname = string.IsNullOrWhiteSpace(settings.DeviceNickname)
            ? Environment.MachineName
            : settings.DeviceNickname.Trim();
        var createdAt = settings.InstallationCreatedAt ?? now;

        return settings with
        {
            InstallationId = installationId,
            DeviceNickname = deviceNickname,
            InstallationCreatedAt = createdAt,
            LicenseStatus = settings.LicenseStatus
        };
    }

    public LicenseGateResult EvaluateLocalAccess(AppSettings settings)
    {
        return settings.LicenseStatus switch
        {
            LicenseStatus.Disabled => new LicenseGateResult(
                false,
                "This installation ID is disabled. Local vault data remains on this computer."),
            LicenseStatus.Revoked => new LicenseGateResult(
                false,
                "This installation ID is revoked. Local vault data remains on this computer."),
            _ => new LicenseGateResult(true, $"Installation status: {settings.LicenseStatus}.")
        };
    }

    public InstallationCheckInPayload CreateCheckInPayload(
        AppSettings settings,
        string appVersion,
        BackupHealthSummary health,
        DateTimeOffset checkedInAt)
    {
        var enabled = EvaluateLocalAccess(settings).Allowed;
        return new InstallationCheckInPayload(
            settings.InstallationId,
            settings.LicenseKey.Trim(),
            settings.FirmName.Trim(),
            settings.DeviceNickname.Trim(),
            appVersion,
            settings.LicenseStatus,
            enabled,
            new FeatureEntitlements(settings.CloudBackupEnabled && settings.LicenseStatus is LicenseStatus.Active or LicenseStatus.Trial),
            health,
            checkedInAt);
    }
}
