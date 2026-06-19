using System.Windows;
using WakiliDms.App.ViewModels;
using WakiliDms.Infrastructure.Documents;
using WakiliDms.Infrastructure.Matter;
using WakiliDms.Infrastructure.Scan;
using WakiliDms.Infrastructure.Search;
using WakiliDms.Infrastructure.Settings;
using WakiliDms.Infrastructure.Vault;
using WakiliDms.Core.Search;

namespace WakiliDms.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsStore = new JsonSettingsStore(DefaultAppPaths.SettingsPath());
        var matterRepository = new SqliteMatterRepository(DefaultAppPaths.DatabasePath());
        var documentRepository = new SqliteDocumentRepository(DefaultAppPaths.DatabasePath());
        var documentVersionRepository = new SqliteDocumentVersionRepository(DefaultAppPaths.DatabasePath());
        var scanInboxRepository = new SqliteScanInboxRepository(DefaultAppPaths.DatabasePath());
        var documentSearchRepository = new SqliteDocumentSearchRepository(DefaultAppPaths.DatabasePath());
        var documentTextExtractor = new LocalDocumentTextExtractor();
        var vaultService = new EncryptedVaultService();
        var viewModel = new MainWindowViewModel(
            settingsStore,
            matterRepository,
            documentRepository,
            documentVersionRepository,
            scanInboxRepository,
            documentSearchRepository,
            documentTextExtractor,
            vaultService);
        await viewModel.LoadAsync();

        var window = new MainWindow(viewModel);
        window.Show();
    }
}
