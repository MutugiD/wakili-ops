using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using WakiliDms.Core.Common;

namespace WakiliDms.Core.Search;

public sealed partial class LocalDocumentTextExtractor : IDocumentTextExtractor
{
    public Task<Result<string>> ExtractTextAsync(
        string originalFileName,
        byte[] fileBytes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return Task.FromResult(Result<string>.Fail("Original file name is required."));
        }

        if (fileBytes.Length == 0)
        {
            return Task.FromResult(Result<string>.Fail("Cannot extract text from an empty document."));
        }

        var extension = Path.GetExtension(originalFileName);
        return extension.ToLowerInvariant() switch
        {
            ".docx" => Task.FromResult(ExtractDocxText(fileBytes)),
            ".pdf" => Task.FromResult(ExtractTextLikePdf(fileBytes)),
            ".doc" => Task.FromResult(Result<string>.Fail("Legacy DOC text extraction is not available in V1 search.")),
            _ => Task.FromResult(Result<string>.Fail("Document type is not supported for text extraction."))
        };
    }

    private static Result<string> ExtractDocxText(byte[] fileBytes)
    {
        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry is null)
            {
                return Result<string>.Fail("DOCX document body was not found.");
            }

            var builder = new StringBuilder();
            using var documentStream = documentEntry.Open();
            using var reader = XmlReader.Create(documentStream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            });

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                {
                    var value = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        builder.Append(value).Append(' ');
                    }
                }
            }

            return NormalizeExtractedText(builder.ToString());
        }
        catch (InvalidDataException)
        {
            return Result<string>.Fail("DOCX file could not be read.");
        }
        catch (XmlException)
        {
            return Result<string>.Fail("DOCX document XML could not be parsed.");
        }
    }

    private static Result<string> ExtractTextLikePdf(byte[] fileBytes)
    {
        var raw = Encoding.Latin1.GetString(fileBytes);
        var builder = new StringBuilder();

        foreach (Match match in PdfLiteralTextRegex().Matches(raw))
        {
            var value = match.Groups["text"].Value
                .Replace("\\(", "(", StringComparison.Ordinal)
                .Replace("\\)", ")", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal);

            builder.Append(value).Append(' ');
        }

        if (builder.Length == 0)
        {
            foreach (Match match in PrintableRunRegex().Matches(raw))
            {
                if (match.Value.Any(char.IsLetter))
                {
                    builder.Append(match.Value).Append(' ');
                }
            }
        }

        return NormalizeExtractedText(builder.ToString());
    }

    private static Result<string> NormalizeExtractedText(string text)
    {
        var normalized = WhitespaceRegex().Replace(text, " ").Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? Result<string>.Fail("No searchable text could be extracted.")
            : Result<string>.Ok(normalized);
    }

    [GeneratedRegex(@"\((?<text>(?:\\.|[^\\)]){2,})\)")]
    private static partial Regex PdfLiteralTextRegex();

    [GeneratedRegex(@"[\p{L}\p{N}\p{P}\p{Zs}]{4,}")]
    private static partial Regex PrintableRunRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
