namespace BubblesCmd.Core.Models;

public sealed class TerminalOutputEventArgs(string text) : EventArgs
{
    public string Text { get; } = text;
}
