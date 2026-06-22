namespace WakiliDms.App.ViewModels;

public sealed class AllowAllUserConfirmationService : IUserConfirmationService
{
    public static AllowAllUserConfirmationService Instance { get; } = new();

    private AllowAllUserConfirmationService()
    {
    }

    public bool ConfirmDestructiveAction(string title, string message)
    {
        return true;
    }
}
