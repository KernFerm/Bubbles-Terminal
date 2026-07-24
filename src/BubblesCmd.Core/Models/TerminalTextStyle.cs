namespace BubblesCmd.Core.Models;

public sealed record TerminalTextStyle(
    TerminalColor? Foreground = null,
    TerminalColor? Background = null,
    bool Bold = false,
    bool Dim = false,
    bool Italic = false,
    bool Underline = false,
    bool Reverse = false)
{
    public static TerminalTextStyle Default { get; } = new();
}
