namespace BubblesCmd.Core.Models;

public sealed class AppSettings
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public string? DefaultProfileId { get; set; }

    public bool RestorePreviousWorkspace { get; set; } = true;

    public int ScrollbackLineLimit { get; set; } = 4000;

    public bool ConfirmMultilinePaste { get; set; } = true;

    public bool ConfirmDestructivePaste { get; set; } = true;

    public bool ConfirmControlCharacterPaste { get; set; } = true;

    public bool DiagnosticLoggingEnabled { get; set; }

    public TerminalAppearanceSettings Appearance { get; set; } = new();

    public List<ShellProfile> CustomProfiles { get; set; } = [];

    public List<CommandSnippet> Snippets { get; set; } =
    [
        new CommandSnippet
        {
            Name = "List directory",
            Description = "Insert a simple directory listing command.",
            ShellType = "cmd",
            Command = "dir",
            Tags = ["files"]
        },
        new CommandSnippet
        {
            Name = "PowerShell version",
            Description = "Insert a PowerShell version check.",
            ShellType = "powershell",
            Command = "$PSVersionTable",
            Tags = ["powershell", "diagnostics"]
        }
    ];

    public List<SavedTab> LastWorkspace { get; set; } = [];
}
