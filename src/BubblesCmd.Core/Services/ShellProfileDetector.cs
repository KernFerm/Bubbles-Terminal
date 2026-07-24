using System.Diagnostics;
using BubblesCmd.Core.Models;

namespace BubblesCmd.Core.Services;

public sealed class ShellProfileDetector
{
    public IReadOnlyList<ShellProfile> DetectProfiles(IEnumerable<ShellProfile>? customProfiles = null)
    {
        var profiles = new List<ShellProfile>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        void AddProfile(ShellProfile profile)
        {
            if (!File.Exists(profile.ExecutablePath))
            {
                return;
            }

            if (seen.Add(profile.Id))
            {
                profiles.Add(profile);
            }
        }

        var windowsPowerShell = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");
        AddProfile(new ShellProfile(
            "powershell-5",
            "Windows PowerShell",
            windowsPowerShell,
            "-NoLogo",
            home,
            IconGlyph: "\uE7AC",
            ColorKey: "Blue"));

        AddProfile(new ShellProfile(
            "cmd",
            "Command Prompt",
            cmdPath,
            string.Empty,
            home,
            IconGlyph: "\uE756",
            ColorKey: "Amber"));

        if (ResolveWithWhere("az").FirstOrDefault() is { } azureCliPath)
        {
            AddProfile(new ShellProfile(
                "azure-cloud-shell",
                "Azure Cloud Shell",
                cmdPath,
                $"/K \"\"{azureCliPath}\" cloud-shell\"",
                home,
                IconGlyph: "\uE753",
                ColorKey: "Blue"));
        }
        else
        {
            AddProfile(new ShellProfile(
                "azure-cloud-shell",
                "Azure Cloud Shell",
                cmdPath,
                "/K echo Azure Cloud Shell requires Azure CLI ^(az^) to be installed and available on PATH.",
                home,
                IconGlyph: "\uE753",
                ColorKey: "Blue"));
        }

        var visualStudioProfiles = DetectVisualStudioDeveloperProfiles(home).ToArray();
        foreach (var vsDevShell in visualStudioProfiles)
        {
            AddProfile(vsDevShell);
        }

        if (!visualStudioProfiles.Any(profile => profile.Id.StartsWith("vs-devcmd-", StringComparison.OrdinalIgnoreCase)))
        {
            AddProfile(new ShellProfile(
                "vs-devcmd-missing",
                "Developer Command Prompt for VS 2022",
                cmdPath,
                "/K echo Visual Studio 2022 developer command prompt was not detected on this machine.",
                home,
                IconGlyph: "\uE943",
                ColorKey: "Purple"));
        }

        if (!visualStudioProfiles.Any(profile => profile.Id.StartsWith("vs-devps-", StringComparison.OrdinalIgnoreCase)))
        {
            AddProfile(new ShellProfile(
                "vs-devps-missing",
                "Developer PowerShell for VS 2022",
                windowsPowerShell,
                "-NoLogo -NoExit -Command \"Write-Host 'Visual Studio 2022 developer PowerShell was not detected on this machine.'\"",
                home,
                IconGlyph: "\uE943",
                ColorKey: "Purple"));
        }

        var gitBashCandidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "bash.exe"),
            Path.Combine(home, "AppData", "Local", "Programs", "Git", "bin", "bash.exe")
        };

        foreach (var gitBash in gitBashCandidates.Where(File.Exists))
        {
            AddProfile(new ShellProfile(
                $"git-bash-{gitBash.GetHashCode(StringComparison.OrdinalIgnoreCase):X8}",
                "Git Bash",
                gitBash,
                "--login -i",
                home,
                IconGlyph: "\uE943",
                ColorKey: "Green"));
        }

        foreach (var pwshPath in ResolveWithWhere("pwsh"))
        {
            AddProfile(new ShellProfile(
                $"pwsh-{pwshPath.GetHashCode(StringComparison.OrdinalIgnoreCase):X8}",
                "PowerShell 7",
                pwshPath,
                "-NoLogo",
                home,
                IconGlyph: "\uE7AC",
                ColorKey: "Teal"));
        }

        var wslPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wsl.exe");
        AddProfile(new ShellProfile(
            "wsl",
            "WSL",
            wslPath,
            string.Empty,
            home,
            IconGlyph: "\uF489",
            ColorKey: "Purple"));

        if (customProfiles is not null)
        {
            foreach (var customProfile in customProfiles)
            {
                AddProfile(customProfile);
            }
        }

        return profiles;
    }

    private static IEnumerable<ShellProfile> DetectVisualStudioDeveloperProfiles(string home)
    {
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield break;
        }

        var basePath = Path.Combine(programFilesX86, "Microsoft Visual Studio");
        if (!Directory.Exists(basePath))
        {
            yield break;
        }

        foreach (var devCmd in Directory.EnumerateFiles(basePath, "VsDevCmd.bat", SearchOption.AllDirectories).Take(2))
        {
            var versionName = GetVisualStudioVersionName(devCmd);
            yield return new ShellProfile(
                $"vs-devcmd-{devCmd.GetHashCode(StringComparison.OrdinalIgnoreCase):X8}",
                $"Developer Command Prompt for {versionName}",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                $"/K \"{devCmd}\"",
                home,
                IconGlyph: "\uE943",
                ColorKey: "Purple");
        }

        foreach (var devPs in Directory.EnumerateFiles(basePath, "Launch-VsDevShell.ps1", SearchOption.AllDirectories).Take(2))
        {
            var versionName = GetVisualStudioVersionName(devPs);
            yield return new ShellProfile(
                $"vs-devps-{devPs.GetHashCode(StringComparison.OrdinalIgnoreCase):X8}",
                $"Developer PowerShell for {versionName}",
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "WindowsPowerShell",
                    "v1.0",
                    "powershell.exe"),
                $"-NoLogo -NoExit -File \"{devPs}\"",
                home,
                IconGlyph: "\uE943",
                ColorKey: "Purple");
        }
    }

    private static string GetVisualStudioVersionName(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}2022{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            ? "VS 2022"
            : "Visual Studio";
    }

    private static IEnumerable<string> ResolveWithWhere(string executable)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "where.exe"),
                    Arguments = executable,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1500);

            return output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }
}
