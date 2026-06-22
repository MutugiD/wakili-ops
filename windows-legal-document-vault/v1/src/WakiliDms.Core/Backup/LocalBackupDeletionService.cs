using WakiliDms.Core.Common;

namespace WakiliDms.Core.Backup;

public sealed class LocalBackupDeletionService
{
    public Result DeleteSnapshot(string backupTargetPath, string backupDirectory)
    {
        if (string.IsNullOrWhiteSpace(backupTargetPath))
        {
            return Result.Fail("Backup target path is required.");
        }

        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            return Result.Fail("Backup snapshot folder is required.");
        }

        var targetFullPath = Path.GetFullPath(backupTargetPath);
        var backupFullPath = Path.GetFullPath(backupDirectory);
        if (!PathEqualsOrIsInside(backupFullPath, targetFullPath)
            || string.Equals(backupFullPath, targetFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail("Only backup snapshots inside the configured backup target can be deleted.");
        }

        if (!Directory.Exists(backupFullPath))
        {
            return Result.Fail("Backup snapshot folder was not found.");
        }

        if (!File.Exists(Path.Combine(backupFullPath, "backup-manifest.json")))
        {
            return Result.Fail("Selected folder is not a valid backup snapshot.");
        }

        try
        {
            Directory.Delete(backupFullPath, recursive: true);
            return Result.Ok();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Fail($"Backup snapshot delete failed: {ex.Message}");
        }
    }

    private static bool PathEqualsOrIsInside(string candidatePath, string parentPath)
    {
        var candidateFullPath = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parentFullPath = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return candidateFullPath.Equals(parentFullPath, StringComparison.OrdinalIgnoreCase)
            || candidateFullPath.StartsWith(parentFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || candidateFullPath.StartsWith(parentFullPath + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}

