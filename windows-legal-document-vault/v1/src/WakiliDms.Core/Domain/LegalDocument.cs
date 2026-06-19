namespace WakiliDms.Core.Domain;

public sealed record LegalDocument
{
    private LegalDocument(
        Guid id,
        Guid matterId,
        string originalFileName,
        string extension,
        string vaultObjectId,
        string sha256Hash,
        long byteLength,
        DocumentType documentType,
        DocumentStatus status,
        DateTimeOffset importedAt)
    {
        Id = id;
        MatterId = matterId;
        OriginalFileName = originalFileName;
        Extension = extension;
        VaultObjectId = vaultObjectId;
        Sha256Hash = sha256Hash;
        ByteLength = byteLength;
        DocumentType = documentType;
        Status = status;
        ImportedAt = importedAt;
    }

    public Guid Id { get; }

    public Guid MatterId { get; }

    public string OriginalFileName { get; }

    public string Extension { get; }

    public string VaultObjectId { get; }

    public string Sha256Hash { get; }

    public long ByteLength { get; }

    public DocumentType DocumentType { get; }

    public DocumentStatus Status { get; }

    public DateTimeOffset ImportedAt { get; }

    public static LegalDocument CreateImported(
        Guid matterId,
        string originalFileName,
        string vaultObjectId,
        string sha256Hash,
        long byteLength,
        DocumentType documentType = DocumentType.Unknown,
        DateTimeOffset? now = null)
    {
        if (matterId == Guid.Empty)
        {
            throw new ArgumentException("Matter ID is required.", nameof(matterId));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
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
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Document byte length must be greater than zero.");
        }

        var safeFileName = Path.GetFileName(originalFileName.Trim());
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Document file extension is required.", nameof(originalFileName));
        }

        return new LegalDocument(
            Guid.NewGuid(),
            matterId,
            safeFileName,
            extension,
            vaultObjectId.Trim(),
            sha256Hash.Trim().ToUpperInvariant(),
            byteLength,
            documentType,
            DocumentStatus.Imported,
            now ?? DateTimeOffset.UtcNow);
    }

    public static LegalDocument Rehydrate(
        Guid id,
        Guid matterId,
        string originalFileName,
        string extension,
        string vaultObjectId,
        string sha256Hash,
        long byteLength,
        DocumentType documentType,
        DocumentStatus status,
        DateTimeOffset importedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Document ID is required.", nameof(id));
        }

        if (matterId == Guid.Empty)
        {
            throw new ArgumentException("Matter ID is required.", nameof(matterId));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Document extension is required.", nameof(extension));
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
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Document byte length must be greater than zero.");
        }

        return new LegalDocument(
            id,
            matterId,
            Path.GetFileName(originalFileName.Trim()),
            extension.Trim().ToLowerInvariant(),
            vaultObjectId.Trim(),
            sha256Hash.Trim().ToUpperInvariant(),
            byteLength,
            documentType,
            status,
            importedAt);
    }

    public LegalDocument WithClassification(DocumentType documentType, DocumentStatus status)
    {
        if (DocumentLifecycle.IsImmutable(Status) && (DocumentType != documentType || Status != status))
        {
            throw new InvalidOperationException("Filed and served documents cannot be reclassified.");
        }

        return new LegalDocument(
            Id,
            MatterId,
            OriginalFileName,
            Extension,
            VaultObjectId,
            Sha256Hash,
            ByteLength,
            documentType,
            status,
            ImportedAt);
    }
}
