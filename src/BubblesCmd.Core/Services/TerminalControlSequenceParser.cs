using System.Text;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class TerminalControlSequenceParser
{
    private const int MaxWindowTitleLength = 120;

    public TerminalControlSequenceResult Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TerminalControlSequenceResult(string.Empty, null, null, 0);
        }

        var output = new StringBuilder(text.Length);
        string? windowTitle = null;
        bool? bracketedPasteEnabled = null;
        var bellCount = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\u0007')
            {
                bellCount++;
                continue;
            }

            if (text[index] != '\u001b' || index + 1 >= text.Length)
            {
                output.Append(text[index]);
                continue;
            }

            if (text[index + 1] == ']')
            {
                if (TryReadOsc(text, index, out var endIndex, out var command, out var payload))
                {
                    if (command is "0" or "2")
                    {
                        windowTitle = SanitizeWindowTitle(payload);
                    }

                    index = endIndex;
                    continue;
                }
            }

            if (text[index + 1] == '[')
            {
                if (TryReadCsi(text, index, out var endIndex, out var sequence))
                {
                    if (sequence == "?2004h")
                    {
                        bracketedPasteEnabled = true;
                        index = endIndex;
                        continue;
                    }

                    if (sequence == "?2004l")
                    {
                        bracketedPasteEnabled = false;
                        index = endIndex;
                        continue;
                    }
                }
            }

            output.Append(text[index]);
        }

        return new TerminalControlSequenceResult(output.ToString(), windowTitle, bracketedPasteEnabled, bellCount);
    }

    private static bool TryReadOsc(
        string text,
        int escapeIndex,
        out int endIndex,
        out string command,
        out string payload)
    {
        endIndex = escapeIndex;
        command = string.Empty;
        payload = string.Empty;

        var contentStart = escapeIndex + 2;
        for (var index = contentStart; index < text.Length; index++)
        {
            var isBelTerminated = text[index] == '\u0007';
            var isStringTerminated = text[index] == '\u001b' && index + 1 < text.Length && text[index + 1] == '\\';
            if (!isBelTerminated && !isStringTerminated)
            {
                continue;
            }

            var contentEnd = index;
            var raw = text[contentStart..contentEnd];
            var separator = raw.IndexOf(';');
            if (separator < 0)
            {
                return false;
            }

            command = raw[..separator];
            payload = raw[(separator + 1)..];
            endIndex = isStringTerminated ? index + 1 : index;
            return true;
        }

        return false;
    }

    private static bool TryReadCsi(
        string text,
        int escapeIndex,
        out int endIndex,
        out string sequence)
    {
        endIndex = escapeIndex;
        sequence = string.Empty;

        var contentStart = escapeIndex + 2;
        for (var index = contentStart; index < text.Length; index++)
        {
            var character = text[index];
            if (character is >= '@' and <= '~')
            {
                endIndex = index;
                sequence = text[contentStart..(index + 1)];
                return true;
            }
        }

        return false;
    }

    private static string SanitizeWindowTitle(string title)
    {
        var sanitized = AnsiTextSanitizer.StripControlSequences(title)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Trim();

        if (sanitized.Length > MaxWindowTitleLength)
        {
            sanitized = sanitized[..MaxWindowTitleLength];
        }

        return sanitized;
    }
}
