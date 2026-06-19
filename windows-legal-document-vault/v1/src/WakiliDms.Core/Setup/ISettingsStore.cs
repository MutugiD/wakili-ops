namespace WakiliDms.Core.Setup;

public interface ISettingsStore
{
    Task<AppSettings?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
