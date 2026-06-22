using WakiliDms.Core.CloudBackup;
using WakiliDms.Core.Licensing;

namespace WakiliDms.Core.Backup;

public sealed class BackupHealthEvaluationService
{
    private static readonly TimeSpan StaleLocalBackupAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan StaleCloudBackupAge = TimeSpan.FromDays(14);

    public BackupHealthSummary Evaluate(
        IReadOnlyList<LocalBackupSnapshotSummary> localSnapshots,
        IReadOnlyList<CloudBackupSnapshotMetadata> cloudSnapshots,
        DateTimeOffset now)
    {
        DateTimeOffset? lastLocalBackupAt = localSnapshots.Count == 0
            ? null
            : localSnapshots.Max(snapshot => snapshot.CreatedAt);
        DateTimeOffset? lastCloudBackupAt = cloudSnapshots.Count == 0
            ? null
            : cloudSnapshots.Max(snapshot => snapshot.CreatedAt);

        var status = BuildStatus(lastLocalBackupAt, lastCloudBackupAt, cloudSnapshots.Count, now);
        return new BackupHealthSummary(lastLocalBackupAt, lastCloudBackupAt, status);
    }

    private static string BuildStatus(
        DateTimeOffset? lastLocalBackupAt,
        DateTimeOffset? lastCloudBackupAt,
        int cloudSnapshotCount,
        DateTimeOffset now)
    {
        if (lastLocalBackupAt is null)
        {
            return "Attention: no local backup snapshots found.";
        }

        if (now - lastLocalBackupAt.Value > StaleLocalBackupAge)
        {
            return "Attention: latest local backup is older than 7 days.";
        }

        if (cloudSnapshotCount > 0
            && lastCloudBackupAt is not null
            && now - lastCloudBackupAt.Value > StaleCloudBackupAge)
        {
            return "Attention: latest cloud backup is older than 14 days.";
        }

        return cloudSnapshotCount > 0
            ? "Healthy: local and cloud backup snapshots are available."
            : "Healthy: local backup snapshot is available.";
    }
}
