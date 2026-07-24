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
    IDictionary<string, string>? EnvironmentOverrides = null)
{
    public IDictionary<string, string> EnvironmentOverrides { get; init; } =
        EnvironmentOverrides is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(EnvironmentOverrides, StringComparer.OrdinalIgnoreCase);
}
