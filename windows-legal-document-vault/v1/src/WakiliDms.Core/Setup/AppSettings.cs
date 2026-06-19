namespace WakiliDms.Core.Setup;

public sealed record AppSettings
{
    public string FirmName { get; init; } = string.Empty;

    public string PrimaryUser { get; init; } = string.Empty;

    public string VaultPath { get; init; } = string.Empty;

    public string ScanFolderPath { get; init; } = string.Empty;

    public string BackupTargetPath { get; init; } = string.Empty;

    public bool RecoveryKeyConfirmed { get; init; }

    public DateTimeOffset? SetupCompletedAt { get; init; }

    public bool CloudBackupEnabled { get; init; }
}
