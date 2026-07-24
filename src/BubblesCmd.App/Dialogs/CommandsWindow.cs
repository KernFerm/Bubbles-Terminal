using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BubblesCmd.Core.Models;

namespace BubblesCmd.App.Dialogs;

internal sealed class CommandsWindow : Window
{
    private readonly TextBox _searchBox = new();
    private readonly ListView _commandsList = new();
    private readonly IReadOnlyList<DiscoveredCommand> _commands;

    public CommandsWindow(IEnumerable<DiscoveredCommand> commands)
    {
        _commands = commands.ToList();
        Title = "Commands";
        Width = 760;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DialogTheme.Apply(this);

        var root = new DockPanel { Margin = new Thickness(12) };
        root.Background = DialogTheme.Background;
        _searchBox.Margin = new Thickness(0, 0, 0, 8);
        _searchBox.ToolTip = "Search command names, type, or source";
        _searchBox.KeyUp += (_, _) => RefreshList();
        DialogTheme.StyleTextBox(_searchBox);
        DockPanel.SetDock(_searchBox, Dock.Top);
        root.Children.Add(_searchBox);

        _commandsList.FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono");
        _commandsList.FontSize = 13;
        DialogTheme.StyleListView(_commandsList);
        _commandsList.View = new GridView
        {
            Columns =
            {
                CreateColumn("Command", nameof(DiscoveredCommand.Name), 190),
                CreateColumn("Kind", nameof(DiscoveredCommand.CommandType), 130),
                CreateColumn("Source", nameof(DiscoveredCommand.Source), 390)
            }
        };
        _commandsList.MouseDoubleClick += (_, _) => Accept();
        _commandsList.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Accept();
                e.Handled = true;
            }
        };
        root.Children.Add(_commandsList);

        Content = root;
        Loaded += (_, _) =>
        {
            RefreshList();
            _searchBox.Focus();
        };
    }

    public DiscoveredCommand? SelectedCommand { get; private set; }

    private void RefreshList()
    {
        var search = _searchBox.Text.Trim();
        var filtered = _commands
            .Where(command =>
                string.IsNullOrWhiteSpace(search) ||
                command.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                command.CommandType.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                command.Source.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Take(250)
            .ToList();

        _commandsList.ItemsSource = filtered;
        _commandsList.SelectedIndex = filtered.Count > 0 ? 0 : -1;
    }

    private void Accept()
    {
        if (_commandsList.SelectedIndex < 0)
        {
            return;
        }

        SelectedCommand = _commandsList.SelectedItem as DiscoveredCommand;
        DialogResult = SelectedCommand is not null;
    }

    private static GridViewColumn CreateColumn(string header, string bindingPath, double width)
    {
        return new GridViewColumn
        {
            Header = header,
            Width = width,
            CellTemplate = DialogTheme.CreateTextCellTemplate(bindingPath)
        };
    }
}
