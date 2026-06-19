using System.Windows;
using WakiliDms.App.ViewModels;
using WakiliDms.Infrastructure.Documents;
using WakiliDms.Infrastructure.Matter;
using WakiliDms.Infrastructure.Scan;
using WakiliDms.Infrastructure.Settings;
using WakiliDms.Infrastructure.Vault;

namespace WakiliDms.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsStore = new JsonSettingsStore(DefaultAppPaths.SettingsPath());
        var matterRepository = new SqliteMatterRepository(DefaultAppPaths.DatabasePath());
        var documentRepository = new SqliteDocumentRepository(DefaultAppPaths.DatabasePath());
        var scanInboxRepository = new SqliteScanInboxRepository(DefaultAppPaths.DatabasePath());
        var vaultService = new EncryptedVaultService();
        var viewModel = new MainWindowViewModel(
            settingsStore,
            matterRepository,
            documentRepository,
            scanInboxRepository,
            vaultService);
        await viewModel.LoadAsync();

        var window = new MainWindow(viewModel);
        window.Show();
    }
}
