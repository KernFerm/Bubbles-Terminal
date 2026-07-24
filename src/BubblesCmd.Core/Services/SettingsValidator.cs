using BubblesCmd.Core.Models;
using System.Text.RegularExpressions;

namespace BubblesCmd.Core.Services;

public sealed class SettingsValidator
{
    private static readonly Regex HexColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public IReadOnlyList<string> Validate(AppSettings settings)
    {
        var errors = new List<string>();

        if (settings.ScrollbackLineLimit < 100)
        {
            errors.Add("Scrollback line limit must be at least 100.");
        }

        if (settings.ScrollbackLineLimit > 1_000_000)
        {
            errors.Add("Scrollback line limit must be 1,000,000 or lower.");
        }

        if (string.IsNullOrWhiteSpace(settings.Appearance.FontFamily))
        {
            errors.Add("Terminal font family is required.");
        }

        if (settings.Appearance.FontSize is < 8 or > 48)
        {
            errors.Add("Terminal font size must be between 8 and 48.");
        }

        if (settings.Appearance.LineHeight is < 0 or > 96)
        {
            errors.Add("Terminal line height must be 0 or between 1 and 96.");
        }

        ValidateColor(settings.Appearance.BackgroundColor, "Background color", errors);
        ValidateColor(settings.Appearance.ForegroundColor, "Foreground color", errors);
        ValidateColor(settings.Appearance.AccentColor, "Accent color", errors);

        foreach (var profile in settings.CustomProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                errors.Add("Custom profiles must have a name.");
            }

            if (string.IsNullOrWhiteSpace(profile.ExecutablePath) || !File.Exists(profile.ExecutablePath))
            {
                errors.Add($"Custom profile '{profile.DisplayName}' points to a missing executable.");
            }

            if (profile.EnvironmentOverrides.Any(pair => string.IsNullOrWhiteSpace(pair.Key)))
            {
                errors.Add($"Custom profile '{profile.DisplayName}' contains an environment variable with a blank name.");
            }
        }

        foreach (var snippet in settings.Snippets)
        {
            if (string.IsNullOrWhiteSpace(snippet.Name))
            {
                errors.Add("Snippets must have a name.");
            }

            if (string.IsNullOrWhiteSpace(snippet.Command))
            {
                errors.Add($"Snippet '{snippet.Name}' has no command text.");
            }
        }

        return errors;
    }

    private static void ValidateColor(string color, string label, ICollection<string> errors)
    {
        if (!HexColorPattern.IsMatch(color))
        {
            errors.Add($"{label} must be a #RRGGBB value.");
        }
    }
}
