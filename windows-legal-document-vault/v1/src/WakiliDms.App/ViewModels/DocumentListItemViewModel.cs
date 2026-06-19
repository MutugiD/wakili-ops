using WakiliDms.Core.Domain;

namespace WakiliDms.App.ViewModels;

public sealed class DocumentListItemViewModel
{
    public DocumentListItemViewModel(LegalDocument document)
    {
        Id = document.Id;
        OriginalFileName = document.OriginalFileName;
        Extension = document.Extension;
        DocumentType = document.DocumentType.ToString();
        Status = document.Status.ToString();
        ByteLength = document.ByteLength.ToString("N0");
        ImportedAt = document.ImportedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }

    public Guid Id { get; }

    public string OriginalFileName { get; }

    public string Extension { get; }

    public string DocumentType { get; }

    public string Status { get; }

    public string ByteLength { get; }

    public string ImportedAt { get; }
}
