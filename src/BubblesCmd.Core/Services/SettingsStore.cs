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

    private const int MaxBackupFiles = 5;

    public AppSettings Load()
    {
        return LoadWithStatus().Settings;
    }

    public SettingsLoadResult LoadWithStatus()
    {
        return LoadWithStatus(Paths.SettingsFilePath);
    }

    public SettingsLoadResult LoadWithStatus(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new SettingsLoadResult
                {
                    Settings = new AppSettings()
                };
            }

            var json = File.ReadAllText(path);
            return new SettingsLoadResult
            {
                Settings = Normalize(JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings())
            };
        }
        catch (Exception ex)
        {
            BackupCorruptSettings(path);
            return new SettingsLoadResult
            {
                Settings = new AppSettings(),
                UsedFallbackSettings = true,
                WarningMessage = $"Settings could not be loaded and were reset to defaults. {ex.Message}"
            };
        }
    }

    public void Save(AppSettings settings)
    {
        SaveTo(settings, Paths.SettingsFilePath);
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
        WriteFileAtomically(path, json);
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
        TrimBackupFiles("settings-backup-*.json");
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

    private static void WriteFileAtomically(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, contents);

        if (File.Exists(path))
        {
            var backupPath = $"{path}.bak";
            File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    private static void BackupCorruptSettings(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(Paths.AppDataDirectory);
            var backupPath = Path.Combine(Paths.AppDataDirectory, $"settings-corrupt-{DateTimeOffset.Now:yyyyMMddHHmmss}.json");
            File.Copy(path, backupPath, overwrite: true);
            TrimBackupFiles("settings-corrupt-*.json");
        }
        catch
        {
        }
    }

    private static void TrimBackupFiles(string pattern)
    {
        var directory = Paths.AppDataDirectory;
        if (!Directory.Exists(directory))
        {
            return;
        }

        var backups = new DirectoryInfo(directory)
            .EnumerateFiles(pattern)
            .OrderByDescending(file => file.CreationTimeUtc)
            .Skip(MaxBackupFiles)
            .ToArray();

        foreach (var backup in backups)
        {
            try
            {
                backup.Delete();
            }
            catch
            {
            }
        }
    }
}
