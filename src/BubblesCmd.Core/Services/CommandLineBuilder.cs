namespace BubblesCmd.Core.Services;

public static class CommandLineBuilder
{
    public static string Build(string executablePath, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return Quote(executablePath);
        }

        return $"{Quote(executablePath)} {arguments}".Trim();
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
