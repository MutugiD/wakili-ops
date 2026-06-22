using System.Text.Json;

namespace WakiliDms.Core.Backup;

public sealed class LocalBackupCatalogService
{
    public async Task<IReadOnlyList<LocalBackupSnapshotSummary>> ListSnapshotsAsync(
        string backupTargetPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupTargetPath) || !Directory.Exists(backupTargetPath))
        {
            return [];
        }

        var snapshots = new List<LocalBackupSnapshotSummary>();
        foreach (var directory in Directory.EnumerateDirectories(backupTargetPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var manifestPath = Path.Combine(directory, "backup-manifest.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            try
            {
                var manifest = await BackupSnapshotService.ReadManifestAsync(manifestPath, cancellationToken);
                if (manifest is null)
                {
                    continue;
                }

                snapshots.Add(new LocalBackupSnapshotSummary(
                    Path.GetFullPath(directory),
                    Path.GetFileName(directory),
                    manifest.CreatedAt,
                    manifest.Entries.Count,
                    manifest.Entries.Sum(entry => entry.ByteLength)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                continue;
            }
        }

        return snapshots
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ThenBy(snapshot => snapshot.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

