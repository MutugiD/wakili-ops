namespace WakiliDms.Core.Licensing;

public sealed record LicenseGateResult(
    bool Allowed,
    string Message);
