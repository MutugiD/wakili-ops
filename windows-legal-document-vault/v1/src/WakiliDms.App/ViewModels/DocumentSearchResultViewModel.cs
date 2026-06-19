using WakiliDms.Core.Search;

namespace WakiliDms.App.ViewModels;

public sealed class DocumentSearchResultViewModel
{
    public DocumentSearchResultViewModel(DocumentSearchResult result)
    {
        DocumentId = result.DocumentId;
        OriginalFileName = result.OriginalFileName;
        Snippet = result.Snippet;
    }

    public Guid DocumentId { get; }

    public string OriginalFileName { get; }

    public string Snippet { get; }
}
