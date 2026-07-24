namespace BubblesCmd.Core.Models;

public sealed record TerminalControlSequenceResult(
    string Text,
    string? WindowTitle,
    bool? BracketedPasteEnabled,
    int BellCount);
