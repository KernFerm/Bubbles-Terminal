using System.Text;

namespace BubblesCmd.Core.Services;

public sealed class TerminalScreenBuffer(int maxLines = 4000)
{
    private readonly List<StringBuilder> _lines = [new()];
    private readonly int _maxLines = Math.Max(100, maxLines);
    private int _column;
    private int _row;
    private bool _lineRedrawPending;

    public void Append(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '\u001b' &&
                TryReadControlSequence(text, index, out var endIndex, out var command, out var parameters))
            {
                ApplyControlSequence(command, parameters);
                index = endIndex;
                continue;
            }

            switch (character)
            {
                case '\r':
                    _column = 0;
                    _lineRedrawPending = true;
                    break;
                case '\n':
                    _lineRedrawPending = false;
                    NewLine();
                    break;
                case '\b':
                    if (_column > 0)
                    {
                        _column--;
                        var line = _lines[_row];
                        if (_column < line.Length)
                        {
                            line.Remove(_column, 1);
                        }
                    }

                    break;
                case '\t':
                    var spaces = 4 - (_column % 4);
                    for (var spaceIndex = 0; spaceIndex < spaces; spaceIndex++)
                    {
                        WritePrintable(' ');
                    }

                    break;
                default:
                    if (!char.IsControl(character))
                    {
                        WritePrintable(character);
                    }

                    break;
            }
        }
    }

    public void Clear()
    {
        _lines.Clear();
        _lines.Add(new StringBuilder());
        _row = 0;
        _column = 0;
        _lineRedrawPending = false;
    }

    public string GetText()
    {
        return string.Join(Environment.NewLine, _lines.Select(line => line.ToString()));
    }

    public int GetCaretTextIndex()
    {
        var index = 0;
        for (var rowIndex = 0; rowIndex < _row && rowIndex < _lines.Count; rowIndex++)
        {
            index += _lines[rowIndex].Length + Environment.NewLine.Length;
        }

        return index + Math.Min(_column, _lines[_row].Length);
    }

    private void NewLine()
    {
        _row++;
        _column = 0;
        if (_row == _lines.Count)
        {
            _lines.Add(new StringBuilder());
        }

        TrimLines();
    }

    private void WritePrintable(char character)
    {
        var line = _lines[_row];
        if (_lineRedrawPending)
        {
            line.Clear();
            _lineRedrawPending = false;
        }

        while (line.Length < _column)
        {
            line.Append(' ');
        }

        if (_column < line.Length)
        {
            line[_column] = character;
        }
        else
        {
            line.Append(character);
        }

        _column++;
    }

    private void ApplyControlSequence(char command, IReadOnlyList<int> parameters)
    {
        var count = parameters.Count == 0 || parameters[0] <= 0 ? 1 : parameters[0];
        switch (command)
        {
            case 'C':
                _column += count;
                break;
            case 'D':
                _column = Math.Max(0, _column - count);
                break;
            case 'G':
                _column = Math.Max(0, count - 1);
                break;
            case 'K':
                _lineRedrawPending = false;
                EraseInLine(parameters.Count == 0 ? 0 : parameters[0]);
                break;
            case 'P':
                _lineRedrawPending = false;
                DeleteCharacters(count);
                break;
            case '@':
                _lineRedrawPending = false;
                InsertBlanks(count);
                break;
        }
    }

    private void EraseInLine(int mode)
    {
        var line = _lines[_row];
        switch (mode)
        {
            case 0:
                if (_column < line.Length)
                {
                    line.Remove(_column, line.Length - _column);
                }

                break;
            case 1:
                for (var index = 0; index <= Math.Min(_column, line.Length - 1); index++)
                {
                    line[index] = ' ';
                }

                break;
            case 2:
                line.Clear();
                _column = 0;
                break;
        }
    }

    private void DeleteCharacters(int count)
    {
        var line = _lines[_row];
        if (_column >= line.Length)
        {
            return;
        }

        line.Remove(_column, Math.Min(count, line.Length - _column));
    }

    private void InsertBlanks(int count)
    {
        var line = _lines[_row];
        while (line.Length < _column)
        {
            line.Append(' ');
        }

        line.Insert(_column, new string(' ', count));
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

        if (raw.StartsWith('?'))
        {
            raw = raw[1..];
        }

        return raw
            .Split(';', StringSplitOptions.None)
            .Select(parameter => int.TryParse(parameter, out var value) ? value : 0)
            .ToArray();
    }

    private void TrimLines()
    {
        while (_lines.Count > _maxLines)
        {
            _lines.RemoveAt(0);
            _row = Math.Max(0, _row - 1);
        }
    }
}
