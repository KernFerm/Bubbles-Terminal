namespace BubblesCmd.Core.Models;

public sealed class CommandSnippet
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ShellType { get; set; } = "any";

    public string Command { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public bool RequiresAdministrator { get; set; }

    public bool RequiresConfirmation { get; set; } = true;

    public List<string> Tags { get; set; } = [];
}
