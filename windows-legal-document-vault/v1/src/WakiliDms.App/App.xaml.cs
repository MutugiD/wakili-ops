using System.Windows;
using WakiliDms.App.ViewModels;
using WakiliDms.Infrastructure.Matter;
using WakiliDms.Infrastructure.Settings;

namespace WakiliDms.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsStore = new JsonSettingsStore(DefaultAppPaths.SettingsPath());
        var matterRepository = new SqliteMatterRepository(DefaultAppPaths.DatabasePath());
        var viewModel = new MainWindowViewModel(settingsStore, matterRepository);
        await viewModel.LoadAsync();

        var window = new MainWindow(viewModel);
        window.Show();
    }
}
