using System.Text.Json;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings Load()
    {
        try
        {
            var path = Paths.SettingsFilePath;
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(path);
            return Normalize(JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings());
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Paths.AppDataDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(Paths.SettingsFilePath, json);
    }

    public AppSettings LoadFrom(string path)
    {
        var json = File.ReadAllText(path);
        return Normalize(JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings());
    }

    public void SaveTo(AppSettings settings, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void BackupCurrentSettings()
    {
        if (!File.Exists(Paths.SettingsFilePath))
        {
            return;
        }

        Directory.CreateDirectory(Paths.AppDataDirectory);
        var backupPath = Path.Combine(Paths.AppDataDirectory, $"settings-backup-{DateTimeOffset.Now:yyyyMMddHHmmss}.json");
        File.Copy(Paths.SettingsFilePath, backupPath);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        var loadedVersion = settings.Version;
        settings.Version = AppSettings.CurrentVersion;

        if (settings.Snippets.Count == 0)
        {
            settings.Snippets = new AppSettings().Snippets;
        }

        settings.Appearance ??= new TerminalAppearanceSettings();
        MigrateTerminalReadabilityDefaults(settings.Appearance, loadedVersion);

        return settings;
    }

    private static void MigrateTerminalReadabilityDefaults(TerminalAppearanceSettings appearance, int loadedVersion)
    {
        if (loadedVersion >= 2)
        {
            return;
        }

        if (Math.Abs(appearance.FontSize - 14) < 0.01)
        {
            appearance.FontSize = 16;
        }

        if (string.Equals(appearance.BackgroundColor, "#080D13", StringComparison.OrdinalIgnoreCase))
        {
            appearance.BackgroundColor = "#0C0C0C";
        }

        if (string.Equals(appearance.ForegroundColor, "#F5F5F5", StringComparison.OrdinalIgnoreCase))
        {
            appearance.ForegroundColor = "#F2F2F2";
        }
    }
}
