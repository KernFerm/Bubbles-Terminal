namespace BubblesCmd.Core.Models;

public sealed class SettingsLoadResult
{
    public AppSettings Settings { get; init; } = new();

    public bool UsedFallbackSettings { get; init; }

    public string? WarningMessage { get; init; }
}
