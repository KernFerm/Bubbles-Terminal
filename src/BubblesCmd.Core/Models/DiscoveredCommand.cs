namespace BubblesCmd.Core.Models;

public sealed record DiscoveredCommand(
    string Name,
    string Source,
    string CommandType,
    string ShellKind);
