using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BubblesCmd.Core.Models;

namespace BubblesCmd.App.Dialogs;

internal sealed class SnippetWindow : Window
{
    private readonly ListBox _snippetList = new();

    public SnippetWindow(IEnumerable<CommandSnippet> snippets)
    {
        Title = "Snippets";
        Width = 620;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DialogTheme.Apply(this);

        var root = new DockPanel { Margin = new Thickness(12) };
        root.Background = DialogTheme.Background;
        var hint = DialogTheme.TextBlock("Select a snippet to insert into the active shell input.");
        hint.Margin = new Thickness(0, 0, 0, 8);
        DockPanel.SetDock(hint, Dock.Top);
        root.Children.Add(hint);

        _snippetList.ItemsSource = snippets.ToList();
        DialogTheme.StyleListBox(_snippetList, nameof(CommandSnippet.Name));
        _snippetList.MouseDoubleClick += (_, _) => Accept();
        _snippetList.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Accept();
                e.Handled = true;
            }
        };
        root.Children.Add(_snippetList);

        Content = root;
        Loaded += (_, _) =>
        {
            _snippetList.Focus();
            _snippetList.SelectedIndex = 0;
        };
    }

    public CommandSnippet? SelectedSnippet { get; private set; }

    private void Accept()
    {
        SelectedSnippet = _snippetList.SelectedItem as CommandSnippet;
        DialogResult = SelectedSnippet is not null;
    }
}
