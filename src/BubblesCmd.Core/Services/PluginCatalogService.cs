using System.Text.Json;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class PluginCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public string PluginDirectory => Path.Combine(Paths.AppDataDirectory, "plugins");

    public IReadOnlyList<PluginManifest> LoadManifests()
    {
        if (!Directory.Exists(PluginDirectory))
        {
            return [];
        }

        var manifests = new List<PluginManifest>();
        foreach (var manifestPath in Directory.EnumerateFiles(PluginDirectory, "plugin.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
                if (manifest is not null && Validate(manifest).Count == 0)
                {
                    manifests.Add(manifest);
                }
            }
            catch
            {
            }
        }

        return manifests
            .OrderBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> Validate(PluginManifest manifest)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            errors.Add("Plugin id is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("Plugin name is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            errors.Add("Plugin version is required.");
        }

        if (manifest.Permissions.Any(permission => string.IsNullOrWhiteSpace(permission)))
        {
            errors.Add("Plugin permissions must not be blank.");
        }

        return errors;
    }
}
