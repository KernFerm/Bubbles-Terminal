using System.Text;
using System.Text.RegularExpressions;

namespace BubblesCmd.Core.Services;

public static partial class AnsiTextSanitizer
{
    [GeneratedRegex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled)]
    private static partial Regex EscapeSequenceRegex();

    public static string StripControlSequences(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sanitized = EscapeSequenceRegex().Replace(text, string.Empty);
        var builder = new StringBuilder(sanitized.Length);

        foreach (var character in sanitized)
        {
            if (character == '\r' || character == '\n' || character == '\t' || !char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
