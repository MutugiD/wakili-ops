using WakiliDms.Core.Backup;

namespace WakiliDms.App.ViewModels;

public sealed class LocalBackupSnapshotViewModel
{
    public LocalBackupSnapshotViewModel(LocalBackupSnapshotSummary summary)
    {
        Summary = summary;
    }

    public LocalBackupSnapshotSummary Summary { get; }

    public string BackupDirectory => Summary.BackupDirectory;

    public string SnapshotId => Summary.SnapshotId;

    public string CreatedAt => Summary.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string FileCount => $"{Summary.FileCount:N0} file(s)";

    public string ByteLength => $"{Summary.ByteLength:N0} bytes";
}

