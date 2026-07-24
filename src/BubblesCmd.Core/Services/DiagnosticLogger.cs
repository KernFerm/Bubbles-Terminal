namespace BubblesCmd.Core.Services;

public sealed class DiagnosticLogger
{
    private const long MaxLogBytes = 512 * 1024;
    private readonly object _syncRoot = new();

    public string LogDirectory => Path.Combine(Paths.AppDataDirectory, "logs");

    public string LogFilePath => Path.Combine(LogDirectory, "bubbles-cmd.log");

    public bool Enabled { get; set; }

    public void Info(string eventName, IReadOnlyDictionary<string, string>? properties = null)
    {
        Write("INFO", eventName, properties);
    }

    public void Error(string eventName, Exception exception)
    {
        Write("ERROR", eventName, new Dictionary<string, string>
        {
            ["exceptionType"] = exception.GetType().Name,
            ["message"] = exception.Message
        });
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }
        }
    }

    private void Write(string level, string eventName, IReadOnlyDictionary<string, string>? properties)
    {
        if (!Enabled)
        {
            return;
        }

        lock (_syncRoot)
        {
            Directory.CreateDirectory(LogDirectory);
            RotateIfNeeded();

            var fields = properties is null
                ? string.Empty
                : " " + string.Join(
                    " ",
                    properties.Select(pair => $"{Sanitize(pair.Key)}={Sanitize(pair.Value)}"));

            File.AppendAllText(
                LogFilePath,
                $"{DateTimeOffset.Now:O} {level} {Sanitize(eventName)}{fields}{Environment.NewLine}");
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(LogFilePath))
        {
            return;
        }

        var fileInfo = new FileInfo(LogFilePath);
        if (fileInfo.Length < MaxLogBytes)
        {
            return;
        }

        var archivePath = Path.Combine(LogDirectory, $"bubbles-cmd-{DateTimeOffset.Now:yyyyMMddHHmmss}.log");
        File.Move(LogFilePath, archivePath);
    }

    private static string Sanitize(string value)
    {
        return value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal);
    }
}
