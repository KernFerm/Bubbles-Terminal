using BubblesCmd.Core.Models;

namespace BubblesCmd.App.Terminal;

internal sealed class TerminalPaneState(ShellProfile profile, TerminalView view)
{
    public ShellProfile Profile { get; } = profile;

    public TerminalView View { get; } = view;

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}
