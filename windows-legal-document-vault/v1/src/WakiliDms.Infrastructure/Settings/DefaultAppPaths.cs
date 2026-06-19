namespace WakiliDms.Infrastructure.Settings;

public static class DefaultAppPaths
{
    public static string SettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WakiliDms", "settings.json");
    }

    public static string DatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WakiliDms", "wakili-dms.db");
    }
}
