namespace BubblesCmd.App.Services;

internal sealed record TerminalSearchResult(bool Found, int MatchIndex, int MatchCount, int MatchNumber, string Preview);
