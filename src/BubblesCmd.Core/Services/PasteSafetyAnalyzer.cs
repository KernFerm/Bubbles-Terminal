using System.Text.RegularExpressions;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class PasteSafetyAnalyzer
{
    private static readonly Regex RiskyPastePattern = new(
        @"\b(del|erase|rd|rmdir|remove-item|rm|format|diskpart|bcdedit|shutdown|restart-computer|invoke-expression|iex)\b|EncodedCommand|curl\s+.*\|\s*(powershell|pwsh|cmd)|iwr\s+.*\|\s*iex",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PasteSafetyReview Analyze(
        string text,
        bool warnOnMultiline,
        bool warnOnRiskyCommand,
        bool warnOnHiddenControlCharacters)
    {
        var hasMultipleLines = text.Contains('\n') || text.Contains('\r');
        var hasRiskyCommand = RiskyPastePattern.IsMatch(text);
        var hasHiddenControlCharacters = ContainsHiddenControlCharacters(text);
        var reasons = new List<string>();

        if (warnOnRiskyCommand && hasRiskyCommand)
        {
            reasons.Add("The clipboard contains command text that may be destructive or security-sensitive.");
        }

        if (warnOnHiddenControlCharacters && hasHiddenControlCharacters)
        {
            reasons.Add("The clipboard contains hidden control characters that can change what the shell receives.");
        }

        if (warnOnMultiline && hasMultipleLines)
        {
            reasons.Add("The clipboard contains multiple lines.");
        }

        return new PasteSafetyReview(hasMultipleLines, hasRiskyCommand, hasHiddenControlCharacters, reasons);
    }

    private static bool ContainsHiddenControlCharacters(string text)
    {
        foreach (var character in text)
        {
            if (character is '\r' or '\n' or '\t')
            {
                continue;
            }

            if (char.IsControl(character))
            {
                return true;
            }

            if (character is '\u200B' or '\u200C' or '\u200D' or '\u2060' or '\uFEFF')
            {
                return true;
            }

            if (character is >= '\u202A' and <= '\u202E')
            {
                return true;
            }

            if (character is >= '\u2066' and <= '\u2069')
            {
                return true;
            }
        }

        return false;
    }
}
