namespace BubblesCmd.Core.Models;

public sealed class TerminalAppearanceSettings
{
    public string FontFamily { get; set; } = "Cascadia Mono";

    public double FontSize { get; set; } = 16;

    public double LineHeight { get; set; } = 0;

    public string ThemeName { get; set; } = "Bubbles Dark";

    public string BackgroundColor { get; set; } = "#0C0C0C";

    public string ForegroundColor { get; set; } = "#F2F2F2";

    public string AccentColor { get; set; } = "#5FC9B5";

    public bool HighContrast { get; set; }

    public bool ReducedMotion { get; set; }
}
