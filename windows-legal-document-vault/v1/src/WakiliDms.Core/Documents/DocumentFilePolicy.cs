namespace WakiliDms.Core.Documents;

public static class DocumentFilePolicy
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc",
        ".docx",
        ".pdf"
    };

    public static bool IsSupported(string filePath)
    {
        return AllowedExtensions.Contains(Path.GetExtension(filePath));
    }
}
