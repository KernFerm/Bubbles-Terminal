namespace BubblesCmd.Core.Models;

public sealed class TerminalSessionExitedEventArgs(int exitCode) : EventArgs
{
    public int ExitCode { get; } = exitCode;
}
