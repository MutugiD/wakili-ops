using WakiliDms.Core.Common;

namespace WakiliDms.Core.Backup;

public static class BackupStoragePathSafety
{
    public static Result ValidateSeparateStoragePath(
        string candidatePath,
        string vaultPath,
        string backupTargetPath,
        string storageLabel)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return Result.Fail($"{storageLabel} folder is required.");
        }

        if (string.IsNullOrWhiteSpace(vaultPath))
        {
            return Result.Fail("Vault path is required for backup storage validation.");
        }

        if (string.IsNullOrWhiteSpace(backupTargetPath))
        {
            return Result.Fail("Local backup target path is required for backup storage validation.");
        }

        if (PathsOverlap(candidatePath, vaultPath))
        {
            return Result.Fail($"{storageLabel} folder must be separate from the encrypted vault folder.");
        }

        if (PathsOverlap(candidatePath, backupTargetPath))
        {
            return Result.Fail($"{storageLabel} folder must be separate from the local backup target folder.");
        }

        return Result.Ok();
    }

    private static bool PathsOverlap(string firstPath, string secondPath)
    {
        var firstFullPath = Normalize(firstPath);
        var secondFullPath = Normalize(secondPath);
        return IsSameOrInside(firstFullPath, secondFullPath)
            || IsSameOrInside(secondFullPath, firstFullPath);
    }

    private static bool IsSameOrInside(string candidatePath, string parentPath)
    {
        return candidatePath.Equals(parentPath, StringComparison.OrdinalIgnoreCase)
            || candidatePath.StartsWith(parentPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || candidatePath.StartsWith(parentPath + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
