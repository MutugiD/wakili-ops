using WakiliDms.Core.Common;

namespace WakiliDms.Core.Setup;

public static class SetupValidator
{
    public static Result Validate(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.FirmName))
        {
            return Result.Fail("Firm name is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.PrimaryUser))
        {
            return Result.Fail("Primary user is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.VaultPath))
        {
            return Result.Fail("Vault path is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.ScanFolderPath))
        {
            return Result.Fail("Watched scan folder path is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.BackupTargetPath))
        {
            return Result.Fail("Backup target path is required.");
        }

        if (!settings.RecoveryKeyConfirmed)
        {
            return Result.Fail("Recovery key confirmation is required.");
        }

        if (settings.CloudBackupEnabled)
        {
            return Result.Fail("Cloud backup is not available in V1.");
        }

        return Result.Ok();
    }
}
