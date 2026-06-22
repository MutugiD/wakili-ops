namespace WakiliDms.Core.Backup;

public sealed record BackupRetentionPlan(
    BackupRetentionPolicy Policy,
    IReadOnlyList<BackupRetentionCandidate> DeleteCandidates,
    int KeptSnapshotCount);

