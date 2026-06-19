namespace WakiliDms.Core.Domain;

public enum ScanInboxStatus
{
    Pending = 0,
    Imported,
    Ignored
}

public sealed record ScanInboxItem
{
    private ScanInboxItem(
        Guid id,
        string sourcePath,
        string originalFileName,
        string extension,
        string sha256Hash,
        long byteLength,
        ScanInboxStatus status,
        Guid? documentId,
        DateTimeOffset detectedAt,
        DateTimeOffset? importedAt)
    {
        Id = id;
        SourcePath = sourcePath;
        OriginalFileName = originalFileName;
        Extension = extension;
        Sha256Hash = sha256Hash;
        ByteLength = byteLength;
        Status = status;
        DocumentId = documentId;
        DetectedAt = detectedAt;
        ImportedAt = importedAt;
    }

    public Guid Id { get; }

    public string SourcePath { get; }

    public string OriginalFileName { get; }

    public string Extension { get; }

    public string Sha256Hash { get; }

    public long ByteLength { get; }

    public ScanInboxStatus Status { get; }

    public Guid? DocumentId { get; }

    public DateTimeOffset DetectedAt { get; }

    public DateTimeOffset? ImportedAt { get; }

    public static ScanInboxItem CreatePending(
        string sourcePath,
        string sha256Hash,
        long byteLength,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(sha256Hash))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256Hash));
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Scan byte length must be greater than zero.");
        }

        var fullPath = Path.GetFullPath(sourcePath);
        var originalFileName = Path.GetFileName(fullPath);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Scan file extension is required.", nameof(sourcePath));
        }

        return new ScanInboxItem(
            Guid.NewGuid(),
            fullPath,
            originalFileName,
            extension,
            sha256Hash.Trim().ToUpperInvariant(),
            byteLength,
            ScanInboxStatus.Pending,
            null,
            now ?? DateTimeOffset.UtcNow,
            null);
    }

    public static ScanInboxItem Rehydrate(
        Guid id,
        string sourcePath,
        string originalFileName,
        string extension,
        string sha256Hash,
        long byteLength,
        ScanInboxStatus status,
        Guid? documentId,
        DateTimeOffset detectedAt,
        DateTimeOffset? importedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Scan inbox ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Extension is required.", nameof(extension));
        }

        if (string.IsNullOrWhiteSpace(sha256Hash))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256Hash));
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Scan byte length must be greater than zero.");
        }

        return new ScanInboxItem(
            id,
            sourcePath.Trim(),
            Path.GetFileName(originalFileName.Trim()),
            extension.Trim().ToLowerInvariant(),
            sha256Hash.Trim().ToUpperInvariant(),
            byteLength,
            status,
            documentId,
            detectedAt,
            importedAt);
    }
}
