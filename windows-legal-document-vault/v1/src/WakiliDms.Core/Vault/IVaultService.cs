using WakiliDms.Core.Common;

namespace WakiliDms.Core.Vault;

public interface IVaultService
{
    Task<Result> CreateVaultAsync(
        string vaultPath,
        string recoveryKey,
        CancellationToken cancellationToken);

    Task<Result<StoredVaultObject>> StoreObjectAsync(
        string vaultPath,
        string recoveryKey,
        string originalName,
        byte[] plainBytes,
        CancellationToken cancellationToken);

    Task<Result<byte[]>> ReadObjectAsync(
        string vaultPath,
        string recoveryKey,
        string objectId,
        CancellationToken cancellationToken);
}
