using BubblesCmd.App.Terminal;
using BubblesCmd.Core.Models;

namespace BubblesCmd.App.Services;

internal sealed class WorkspaceService
{
    public List<SavedTab> CaptureWorkspace(IEnumerable<TerminalTabState> tabs)
    {
        return tabs
            .Where(tab => tab.HasRunningSessions)
            .Select(tab => new SavedTab
            {
                ProfileId = tab.Profile.Id,
                Title = tab.Title,
                IsPinned = tab.IsPinned,
                StartupCommand = tab.StartupCommand
            })
            .ToList();
    }
}
