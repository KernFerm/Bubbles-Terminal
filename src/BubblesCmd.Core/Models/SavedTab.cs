namespace BubblesCmd.Core.Models;

public sealed class SavedTab
{
    public string ProfileId { get; set; } = string.Empty;

    public string? Title { get; set; }

    public bool IsPinned { get; set; }

    public string? StartupCommand { get; set; }
}
