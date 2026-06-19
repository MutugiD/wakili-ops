using System.Windows;
using WakiliDms.App.ViewModels;

namespace WakiliDms.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
