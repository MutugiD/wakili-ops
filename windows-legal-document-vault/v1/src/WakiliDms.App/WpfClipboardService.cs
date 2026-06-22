using System.Windows;
using WakiliDms.App.ViewModels;

namespace WakiliDms.App;

public sealed class WpfClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
