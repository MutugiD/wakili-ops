using WakiliDms.Core.Common;

namespace WakiliDms.Core.Backup;

public sealed class BackupRetentionPlanner
{
    public Result<BackupRetentionPlan> Plan(
        IReadOnlyList<LocalBackupSnapshotSummary> snapshots,
        BackupRetentionPolicy policy,
        DateTimeOffset now)
    {
        if (policy.KeepLatestCount < 0)
        {
            return Result<BackupRetentionPlan>.Fail("Keep-latest count cannot be negative.");
        }

        if (policy.DeleteOlderThanDays < 0)
        {
            return Result<BackupRetentionPlan>.Fail("Delete-older-than days cannot be negative.");
        }

        var ordered = snapshots
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ThenBy(snapshot => snapshot.SnapshotId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var keepSet = ordered
            .Take(policy.KeepLatestCount)
            .Select(snapshot => snapshot.SnapshotId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<BackupRetentionCandidate>();
        var threshold = now - TimeSpan.FromDays(policy.DeleteOlderThanDays);

        foreach (var snapshot in ordered)
        {
            if (keepSet.Contains(snapshot.SnapshotId))
            {
                continue;
            }

            if (snapshot.CreatedAt <= threshold)
            {
                candidates.Add(new BackupRetentionCandidate(
                    snapshot.SnapshotId,
                    snapshot.BackupDirectory,
                    snapshot.CreatedAt,
                    $"Older than {policy.DeleteOlderThanDays} day(s) and outside the newest {policy.KeepLatestCount} snapshot(s)."));
            }
        }

        return Result<BackupRetentionPlan>.Ok(new BackupRetentionPlan(
            policy,
            candidates,
            ordered.Length - candidates.Count));
    }
}

