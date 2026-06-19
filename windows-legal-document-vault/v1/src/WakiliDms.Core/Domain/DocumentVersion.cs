namespace WakiliDms.Core.Domain;

public sealed record DocumentVersion
{
    private DocumentVersion(
        Guid id,
        Guid documentId,
        int versionNumber,
        string vaultObjectId,
        string sha256Hash,
        long byteLength,
        string originalFileName,
        DocumentStatus status,
        DateTimeOffset createdAt,
        string? notes)
    {
        Id = id;
        DocumentId = documentId;
        VersionNumber = versionNumber;
        VaultObjectId = vaultObjectId;
        Sha256Hash = sha256Hash;
        ByteLength = byteLength;
        OriginalFileName = originalFileName;
        Status = status;
        CreatedAt = createdAt;
        Notes = notes;
    }

    public Guid Id { get; }

    public Guid DocumentId { get; }

    public int VersionNumber { get; }

    public string VaultObjectId { get; }

    public string Sha256Hash { get; }

    public long ByteLength { get; }

    public string OriginalFileName { get; }

    public DocumentStatus Status { get; }

    public DateTimeOffset CreatedAt { get; }

    public string? Notes { get; }

    public static DocumentVersion CreateInitial(LegalDocument document)
    {
        return Create(
            document.Id,
            1,
            document.VaultObjectId,
            document.Sha256Hash,
            document.ByteLength,
            document.OriginalFileName,
            document.Status,
            document.ImportedAt,
            "Initial imported file.");
    }

    public static DocumentVersion Create(
        Guid documentId,
        int versionNumber,
        string vaultObjectId,
        string sha256Hash,
        long byteLength,
        string originalFileName,
        DocumentStatus status,
        DateTimeOffset? createdAt = null,
        string? notes = null)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document ID is required.", nameof(documentId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(vaultObjectId))
        {
            throw new ArgumentException("Vault object ID is required.", nameof(vaultObjectId));
        }

        if (string.IsNullOrWhiteSpace(sha256Hash))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256Hash));
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Version byte length must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        return new DocumentVersion(
            Guid.NewGuid(),
            documentId,
            versionNumber,
            vaultObjectId.Trim(),
            sha256Hash.Trim().ToUpperInvariant(),
            byteLength,
            Path.GetFileName(originalFileName.Trim()),
            status,
            createdAt ?? DateTimeOffset.UtcNow,
            Normalize(notes));
    }

    public static DocumentVersion Rehydrate(
        Guid id,
        Guid documentId,
        int versionNumber,
        string vaultObjectId,
        string sha256Hash,
        long byteLength,
        string originalFileName,
        DocumentStatus status,
        DateTimeOffset createdAt,
        string? notes)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Version ID is required.", nameof(id));
        }

        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document ID is required.", nameof(documentId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "Version number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(vaultObjectId))
        {
            throw new ArgumentException("Vault object ID is required.", nameof(vaultObjectId));
        }

        if (string.IsNullOrWhiteSpace(sha256Hash))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256Hash));
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Version byte length must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        return new DocumentVersion(
            id,
            documentId,
            versionNumber,
            vaultObjectId.Trim(),
            sha256Hash.Trim().ToUpperInvariant(),
            byteLength,
            Path.GetFileName(originalFileName.Trim()),
            status,
            createdAt,
            Normalize(notes));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
