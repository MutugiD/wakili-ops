namespace WakiliDms.Core.Vault;

public sealed record StoredVaultObject(
    string ObjectId,
    string OriginalName,
    string Sha256Hash,
    long PlainLength,
    DateTimeOffset CreatedAt);
