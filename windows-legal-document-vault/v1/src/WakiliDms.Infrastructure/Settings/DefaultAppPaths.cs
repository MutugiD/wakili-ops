namespace WakiliDms.Infrastructure.Settings;

public static class DefaultAppPaths
{
    private const string SettingsPathOverride = "WAKILI_DMS_SETTINGS_PATH";
    private const string DatabasePathOverride = "WAKILI_DMS_DATABASE_PATH";

    public static string SettingsPath()
    {
        var overridePath = Environment.GetEnvironmentVariable(SettingsPathOverride);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WakiliDms", "settings.json");
    }

    public static string DatabasePath()
    {
        var overridePath = Environment.GetEnvironmentVariable(DatabasePathOverride);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WakiliDms", "wakili-dms.db");
    }
}
