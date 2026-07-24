using BubblesCmd.Core.Models;

namespace BubblesCmd.App.Terminal;

internal sealed record ClosedTabState(
    ShellProfile Profile,
    string Title,
    bool WasPinned,
    DateTimeOffset ClosedAt);
