namespace WakiliDms.App.ViewModels;

public sealed class NoOpClipboardService : IClipboardService
{
    public static NoOpClipboardService Instance { get; } = new();

    private NoOpClipboardService()
    {
    }

    public void SetText(string text)
    {
    }
}
