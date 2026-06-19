namespace WakiliDms.Core.Licensing;

public sealed record AdminRegistryResult(
    AdminInstallationRecord Record,
    bool Created);
