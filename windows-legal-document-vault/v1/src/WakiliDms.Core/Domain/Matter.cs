namespace WakiliDms.Core.Domain;

public sealed record Matter
{
    private Matter(
        Guid id,
        string name,
        string? internalReference,
        string? courtCaseNumber,
        string? court,
        string? courtStation,
        string? division,
        string? practiceArea,
        string? clientName,
        string? responsibleAdvocate,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        InternalReference = internalReference;
        CourtCaseNumber = courtCaseNumber;
        Court = court;
        CourtStation = courtStation;
        Division = division;
        PracticeArea = practiceArea;
        ClientName = clientName;
        ResponsibleAdvocate = responsibleAdvocate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? InternalReference { get; }

    public string? CourtCaseNumber { get; }

    public string? Court { get; }

    public string? CourtStation { get; }

    public string? Division { get; }

    public string? PracticeArea { get; }

    public string? ClientName { get; }

    public string? ResponsibleAdvocate { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static Matter Create(
        string name,
        string? internalReference = null,
        string? courtCaseNumber = null,
        string? court = null,
        string? courtStation = null,
        string? division = null,
        string? practiceArea = null,
        string? clientName = null,
        string? responsibleAdvocate = null,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Matter name is required.", nameof(name));
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;

        return new Matter(
            Guid.NewGuid(),
            name.Trim(),
            Normalize(internalReference),
            Normalize(courtCaseNumber),
            Normalize(court),
            Normalize(courtStation),
            Normalize(division),
            Normalize(practiceArea),
            Normalize(clientName),
            Normalize(responsibleAdvocate),
            timestamp,
            timestamp);
    }

    public static Matter Rehydrate(
        Guid id,
        string name,
        string? internalReference,
        string? courtCaseNumber,
        string? court,
        string? courtStation,
        string? division,
        string? practiceArea,
        string? clientName,
        string? responsibleAdvocate,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Matter ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Matter name is required.", nameof(name));
        }

        return new Matter(
            id,
            name.Trim(),
            Normalize(internalReference),
            Normalize(courtCaseNumber),
            Normalize(court),
            Normalize(courtStation),
            Normalize(division),
            Normalize(practiceArea),
            Normalize(clientName),
            Normalize(responsibleAdvocate),
            createdAt,
            updatedAt);
    }

    public Matter WithUpdatedDetails(
        string name,
        string? internalReference = null,
        string? courtCaseNumber = null,
        string? court = null,
        string? courtStation = null,
        string? division = null,
        string? practiceArea = null,
        string? clientName = null,
        string? responsibleAdvocate = null,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Matter name is required.", nameof(name));
        }

        return new Matter(
            Id,
            name.Trim(),
            Normalize(internalReference),
            Normalize(courtCaseNumber),
            Normalize(court),
            Normalize(courtStation),
            Normalize(division),
            Normalize(practiceArea),
            Normalize(clientName),
            Normalize(responsibleAdvocate),
            CreatedAt,
            now ?? DateTimeOffset.UtcNow);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
