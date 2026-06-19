using WakiliDms.Core.Common;

namespace WakiliDms.Core.Search;

public interface IDocumentTextExtractor
{
    Task<Result<string>> ExtractTextAsync(
        string originalFileName,
        byte[] fileBytes,
        CancellationToken cancellationToken);
}
