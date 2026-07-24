using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BubblesCmd.App.Dialogs;

internal sealed class CommandPaletteWindow : Window
{
    private readonly ListBox _commandList = new();

    public CommandPaletteWindow(IEnumerable<PaletteCommand> commands)
    {
        Title = "Command Palette";
        Width = 520;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DialogTheme.Apply(this);

        _commandList.Margin = new Thickness(12);
        _commandList.FontSize = 15;
        _commandList.ItemsSource = commands.ToList();
        DialogTheme.StyleListBox(_commandList, nameof(PaletteCommand.Name));
        _commandList.MouseDoubleClick += (_, _) => Accept();
        _commandList.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Accept();
                e.Handled = true;
            }
        };

        Content = _commandList;
        Loaded += (_, _) =>
        {
            _commandList.Focus();
            _commandList.SelectedIndex = 0;
        };
    }

    public PaletteCommand? SelectedCommand { get; private set; }

    private void Accept()
    {
        SelectedCommand = _commandList.SelectedItem as PaletteCommand;
        DialogResult = SelectedCommand is not null;
    }
}

internal sealed record PaletteCommand(string Name, Action Execute);
