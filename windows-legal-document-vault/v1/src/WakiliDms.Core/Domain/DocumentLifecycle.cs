namespace WakiliDms.Core.Domain;

public enum DocumentType
{
    Unknown = 0,
    Pleading,
    Affidavit,
    Annexure,
    Submission,
    Authority,
    CourtOrder,
    Ruling,
    Judgment,
    FilingReceipt,
    PaymentReceipt,
    Notice,
    Letter,
    Evidence
}

public enum DocumentStatus
{
    Imported = 0,
    Draft,
    Reviewed,
    Approved,
    Signed,
    ScannedSignedCopy,
    FilingPackCandidate,
    Filed,
    Served,
    Amended,
    RejectedByRegistry,
    Corrected,
    Archived
}

public static class DocumentLifecycle
{
    public static bool IsImmutable(DocumentStatus status)
    {
        return status is DocumentStatus.Filed or DocumentStatus.Served;
    }
}
