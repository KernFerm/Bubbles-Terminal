namespace BubblesCmd.Core.Models;

public sealed record ShellProfile(
    string Id,
    string DisplayName,
    string ExecutablePath,
    string Arguments,
    string StartingDirectory,
    bool RunAsAdministrator = false,
    string IconGlyph = "\uE756",
    string ColorKey = "Default",
    string? StartupCommand = null,
    string? TabTitleTemplate = null,
    IDictionary<string, string>? EnvironmentOverrides = null)
{
    public string? StartupCommand { get; init; } = string.IsNullOrWhiteSpace(StartupCommand)
        ? null
        : StartupCommand.Trim();

    public string? TabTitleTemplate { get; init; } = string.IsNullOrWhiteSpace(TabTitleTemplate)
        ? null
        : TabTitleTemplate.Trim();

    public IDictionary<string, string> EnvironmentOverrides { get; init; } =
        EnvironmentOverrides is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(EnvironmentOverrides, StringComparer.OrdinalIgnoreCase);
}
