using WakiliDms.Core.Domain;

namespace WakiliDms.App.ViewModels;

public sealed class ScanInboxItemViewModel
{
    public ScanInboxItemViewModel(ScanInboxItem item)
    {
        Id = item.Id;
        SourcePath = item.SourcePath;
        OriginalFileName = item.OriginalFileName;
        Extension = item.Extension;
        ByteLength = item.ByteLength.ToString("N0");
        DetectedAt = item.DetectedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }

    public Guid Id { get; }

    public string SourcePath { get; }

    public string OriginalFileName { get; }

    public string Extension { get; }

    public string ByteLength { get; }

    public string DetectedAt { get; }
}
