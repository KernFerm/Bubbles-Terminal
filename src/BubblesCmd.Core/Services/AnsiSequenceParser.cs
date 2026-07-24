using System.Text;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class AnsiSequenceParser
{
    private static readonly TerminalColor[] BasicPalette =
    [
        new(0, 0, 0),
        new(205, 49, 49),
        new(13, 188, 121),
        new(229, 229, 16),
        new(36, 114, 200),
        new(188, 63, 188),
        new(17, 168, 205),
        new(229, 229, 229),
        new(102, 102, 102),
        new(241, 76, 76),
        new(35, 209, 139),
        new(245, 245, 67),
        new(59, 142, 234),
        new(214, 112, 214),
        new(41, 184, 219),
        new(255, 255, 255)
    ];

    private TerminalTextStyle _currentStyle = TerminalTextStyle.Default;

    public IReadOnlyList<TerminalTextSegment> Parse(string text)
    {
        var segments = new List<TerminalTextSegment>();
        var buffer = new StringBuilder();

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character != '\u001b')
            {
                AppendPrintable(buffer, character);
                continue;
            }

            FlushBuffer(segments, buffer, _currentStyle);
            if (TryReadControlSequence(text, index, out var endIndex, out var command, out var parameters))
            {
                if (command == 'm')
                {
                    ApplySgr(parameters);
                }

                index = endIndex;
            }
        }

        FlushBuffer(segments, buffer, _currentStyle);
        return segments;
    }

    private static void AppendPrintable(StringBuilder buffer, char character)
    {
        if (character == '\r' || character == '\n' || character == '\t' || !char.IsControl(character))
        {
            buffer.Append(character);
        }
    }

    private void ApplySgr(IReadOnlyList<int> parameters)
    {
        if (parameters.Count == 0)
        {
            _currentStyle = TerminalTextStyle.Default;
            return;
        }

        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            switch (parameter)
            {
                case 0:
                    _currentStyle = TerminalTextStyle.Default;
                    break;
                case 1:
                    _currentStyle = _currentStyle with { Bold = true };
                    break;
                case 2:
                    _currentStyle = _currentStyle with { Dim = true };
                    break;
                case 3:
                    _currentStyle = _currentStyle with { Italic = true };
                    break;
                case 4:
                    _currentStyle = _currentStyle with { Underline = true };
                    break;
                case 7:
                    _currentStyle = _currentStyle with { Reverse = true };
                    break;
                case 22:
                    _currentStyle = _currentStyle with { Bold = false, Dim = false };
                    break;
                case 23:
                    _currentStyle = _currentStyle with { Italic = false };
                    break;
                case 24:
                    _currentStyle = _currentStyle with { Underline = false };
                    break;
                case 27:
                    _currentStyle = _currentStyle with { Reverse = false };
                    break;
                case 39:
                    _currentStyle = _currentStyle with { Foreground = null };
                    break;
                case 49:
                    _currentStyle = _currentStyle with { Background = null };
                    break;
                case >= 30 and <= 37:
                    _currentStyle = _currentStyle with { Foreground = BasicPalette[parameter - 30] };
                    break;
                case >= 40 and <= 47:
                    _currentStyle = _currentStyle with { Background = BasicPalette[parameter - 40] };
                    break;
                case >= 90 and <= 97:
                    _currentStyle = _currentStyle with { Foreground = BasicPalette[parameter - 90 + 8] };
                    break;
                case >= 100 and <= 107:
                    _currentStyle = _currentStyle with { Background = BasicPalette[parameter - 100 + 8] };
                    break;
                case 38:
                    if (TryReadExtendedColor(parameters, ref index, out var foreground))
                    {
                        _currentStyle = _currentStyle with { Foreground = foreground };
                    }

                    break;
                case 48:
                    if (TryReadExtendedColor(parameters, ref index, out var background))
                    {
                        _currentStyle = _currentStyle with { Background = background };
                    }

                    break;
            }
        }
    }

    private static bool TryReadExtendedColor(IReadOnlyList<int> parameters, ref int index, out TerminalColor color)
    {
        color = new TerminalColor(255, 255, 255);
        if (index + 1 >= parameters.Count)
        {
            return false;
        }

        var mode = parameters[index + 1];
        if (mode == 5 && index + 2 < parameters.Count)
        {
            color = From256Color(parameters[index + 2]);
            index += 2;
            return true;
        }

        if (mode == 2 && index + 4 < parameters.Count)
        {
            color = new TerminalColor(
                ClampByte(parameters[index + 2]),
                ClampByte(parameters[index + 3]),
                ClampByte(parameters[index + 4]));
            index += 4;
            return true;
        }

        return false;
    }

    private static TerminalColor From256Color(int value)
    {
        value = Math.Clamp(value, 0, 255);
        if (value < 16)
        {
            return BasicPalette[value];
        }

        if (value >= 232)
        {
            var shade = (byte)(8 + ((value - 232) * 10));
            return new TerminalColor(shade, shade, shade);
        }

        var color = value - 16;
        var red = color / 36;
        var green = color / 6 % 6;
        var blue = color % 6;
        return new TerminalColor(ToColorCubeValue(red), ToColorCubeValue(green), ToColorCubeValue(blue));
    }

    private static byte ToColorCubeValue(int value)
    {
        return value == 0 ? (byte)0 : (byte)(55 + (value * 40));
    }

    private static byte ClampByte(int value)
    {
        return (byte)Math.Clamp(value, 0, 255);
    }

    private static void FlushBuffer(
        ICollection<TerminalTextSegment> segments,
        StringBuilder buffer,
        TerminalTextStyle style)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        segments.Add(new TerminalTextSegment(buffer.ToString(), style));
        buffer.Clear();
    }

    private static bool TryReadControlSequence(
        string text,
        int escapeIndex,
        out int endIndex,
        out char command,
        out IReadOnlyList<int> parameters)
    {
        endIndex = escapeIndex;
        command = '\0';
        parameters = [];

        if (escapeIndex + 1 >= text.Length || text[escapeIndex + 1] != '[')
        {
            return false;
        }

        var index = escapeIndex + 2;
        while (index < text.Length)
        {
            var character = text[index];
            if (character is >= '@' and <= '~')
            {
                endIndex = index;
                command = character;
                parameters = ParseParameters(text[(escapeIndex + 2)..index]);
                return true;
            }

            index++;
        }

        return false;
    }

    private static IReadOnlyList<int> ParseParameters(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split(';', StringSplitOptions.None)
            .Select(parameter => int.TryParse(parameter, out var value) ? value : 0)
            .ToArray();
    }
}
