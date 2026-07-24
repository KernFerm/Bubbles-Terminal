using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BubblesCmd.App.Dialogs;

internal sealed class TextPromptWindow : Window
{
    private readonly TextBox _textBox = new();

    public TextPromptWindow(string title, string label, string initialValue)
    {
        Title = title;
        Width = 420;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DialogTheme.Apply(this);

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Background = DialogTheme.Background;
        var labelBlock = DialogTheme.TextBlock(label);
        labelBlock.Margin = new Thickness(0, 0, 0, 8);
        panel.Children.Add(labelBlock);

        _textBox.Text = initialValue;
        _textBox.Margin = new Thickness(0, 0, 0, 12);
        DialogTheme.StyleTextBox(_textBox);
        _textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Accept();
                e.Handled = true;
            }
        };
        panel.Children.Add(_textBox);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(DialogTheme.Button("OK", Accept));
        buttons.Children.Add(DialogTheme.Button("Cancel", () => DialogResult = false));
        panel.Children.Add(buttons);

        Content = panel;
        Loaded += (_, _) =>
        {
            _textBox.Focus();
            _textBox.SelectAll();
        };
    }

    public string Value => _textBox.Text.Trim();

    private void Accept()
    {
        DialogResult = !string.IsNullOrWhiteSpace(Value);
    }

}
