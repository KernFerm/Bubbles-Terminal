using System.Windows;
using BubblesCmd.App.Dialogs;

namespace BubblesCmd.App.Services;

internal sealed class CommandPaletteService
{
    public void Show(Window owner, IEnumerable<PaletteCommand> commands)
    {
        var dialog = new CommandPaletteWindow(commands) { Owner = owner };
        if (dialog.ShowDialog() == true)
        {
            dialog.SelectedCommand?.Execute();
        }
    }
}
