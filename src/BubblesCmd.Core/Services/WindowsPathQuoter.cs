namespace BubblesCmd.Core.Services;

public static class WindowsPathQuoter
{
    public static string QuoteForShell(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim();
        var escaped = trimmed.Replace("\"", "\\\"", StringComparison.Ordinal);
        return escaped.Any(char.IsWhiteSpace) ? $"\"{escaped}\"" : escaped;
    }
}
