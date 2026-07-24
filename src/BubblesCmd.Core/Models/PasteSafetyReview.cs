namespace BubblesCmd.Core.Models;

public sealed record PasteSafetyReview(
    bool HasMultipleLines,
    bool HasRiskyCommand,
    bool HasHiddenControlCharacters,
    IReadOnlyList<string> Reasons)
{
    public bool RequiresReview => Reasons.Count > 0;
}
