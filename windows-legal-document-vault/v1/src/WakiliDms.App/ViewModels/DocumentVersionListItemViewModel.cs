using WakiliDms.Core.Domain;

namespace WakiliDms.App.ViewModels;

public sealed class DocumentVersionListItemViewModel
{
    public DocumentVersionListItemViewModel(DocumentVersion version)
    {
        Id = version.Id;
        VersionNumber = $"Version {version.VersionNumber}";
        OriginalFileName = version.OriginalFileName;
        Status = version.Status.ToString();
        ByteLength = version.ByteLength.ToString("N0");
        CreatedAt = version.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        Notes = version.Notes ?? "No notes";
    }

    public Guid Id { get; }

    public string VersionNumber { get; }

    public string OriginalFileName { get; }

    public string Status { get; }

    public string ByteLength { get; }

    public string CreatedAt { get; }

    public string Notes { get; }
}
