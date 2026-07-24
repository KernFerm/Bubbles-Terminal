namespace BubblesCmd.Core.Services;

public static class Paths
{
    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblesCmd");

    public static string SettingsFilePath => Path.Combine(AppDataDirectory, "settings.json");
}
