using System.Diagnostics;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class CommandDiscoveryService
{
    public IReadOnlyList<DiscoveredCommand> DiscoverForProfile(ShellProfile profile, int limit = 500)
    {
        var commands = new List<DiscoveredCommand>();
        if (IsCmdProfile(profile))
        {
            commands.AddRange(DiscoverCmdInternalCommands(profile.ExecutablePath));
        }

        commands.AddRange(DiscoverPathCommands(
            Environment.GetEnvironmentVariable("PATH") ?? string.Empty,
            Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD",
            limit));

        if (IsPowerShellProfile(profile))
        {
            commands.AddRange(DiscoverPowerShellCommands(profile, limit));
        }

        return commands
            .GroupBy(command => $"{command.ShellKind}|{command.CommandType}|{command.Name}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(command => CommandPriority(command))
            .ThenBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    public IReadOnlyList<DiscoveredCommand> DiscoverCmdInternalCommands(string cmdPath)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = string.IsNullOrWhiteSpace(cmdPath) ? "cmd.exe" : cmdPath,
                    Arguments = "/D /C help",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return ParseCmdHelp(output);
        }
        catch
        {
            return [];
        }
    }

    public IReadOnlyList<DiscoveredCommand> DiscoverPathCommands(string pathValue, string pathExtValue, int limit = 500)
    {
        var extensions = pathExtValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(extension => extension.StartsWith('.') ? extension : "." + extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (extensions.Count == 0)
        {
            extensions.Add(".exe");
        }

        var results = new List<DiscoveredCommand>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (!extensions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }

                var commandName = Path.GetFileNameWithoutExtension(file);
                if (!seen.Add(commandName))
                {
                    continue;
                }

                results.Add(new DiscoveredCommand(commandName, file, "Application", "PATH"));
                if (results.Count >= limit)
                {
                    return results;
                }
            }
        }

        return results;
    }

    private static IReadOnlyList<DiscoveredCommand> DiscoverPowerShellCommands(ShellProfile profile, int limit)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = profile.ExecutablePath,
                    Arguments = $"-NoLogo -NoProfile -NonInteractive -Command \"Get-Command | Select-Object -First {limit} Name,CommandType,Source | ConvertTo-Csv -NoTypeInformation\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return ParsePowerShellCsv(output);
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<DiscoveredCommand> ParsePowerShellCsv(string csv)
    {
        var commands = new List<DiscoveredCommand>();
        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Skip(1))
        {
            var columns = ParseCsvLine(line);
            if (columns.Count < 3 || string.IsNullOrWhiteSpace(columns[0]))
            {
                continue;
            }

            commands.Add(new DiscoveredCommand(
                columns[0],
                columns[2],
                string.IsNullOrWhiteSpace(columns[1]) ? "PowerShell" : columns[1],
                "PowerShell"));
        }

        return commands;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"' && index + 1 < line.Length && line[index + 1] == '"')
            {
                value.Append('"');
                index++;
                continue;
            }

            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
                continue;
            }

            value.Append(character);
        }

        values.Add(value.ToString());
        return values;
    }

    private static bool IsPowerShellProfile(ShellProfile profile)
    {
        var fileName = Path.GetFileName(profile.ExecutablePath);
        return fileName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCmdProfile(ShellProfile profile)
    {
        return Path.GetFileName(profile.ExecutablePath)
            .Equals("cmd.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<DiscoveredCommand> ParseCmdHelp(string output)
    {
        var commands = new List<DiscoveredCommand>();
        foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            var separator = line.IndexOf(' ');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            if (name.Length < 2 || !name.All(character => char.IsLetterOrDigit(character) || character == '-'))
            {
                continue;
            }

            if (!name.Any(char.IsLetter))
            {
                continue;
            }

            commands.Add(new DiscoveredCommand(
                name.ToLowerInvariant(),
                "cmd.exe help",
                "CMD built-in",
                "cmd"));
        }

        return commands
            .GroupBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static int CommandPriority(DiscoveredCommand command)
    {
        if (command.CommandType.Equals("CMD built-in", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (command.ShellKind.Equals("PowerShell", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }
}
