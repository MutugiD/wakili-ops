using System.Windows;
using WakiliDms.App.ViewModels;

namespace WakiliDms.App;

public sealed class WpfUserConfirmationService : IUserConfirmationService
{
    public bool ConfirmDestructiveAction(string title, string message)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }
}
