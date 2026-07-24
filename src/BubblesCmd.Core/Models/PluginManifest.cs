namespace BubblesCmd.Core.Models;

public sealed class PluginManifest
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = [];

    public bool Enabled { get; set; }
}
