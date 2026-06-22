namespace WakiliDms.App.ViewModels;

public interface IUserConfirmationService
{
    bool ConfirmDestructiveAction(string title, string message);
}
