using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BubblesCmd.App.Dialogs;

internal static class DialogTheme
{
    public static readonly Brush Background = Brushes.White;
    public static readonly Brush Panel = CreateBrush(22, 33, 44);
    public static readonly Brush Text = Brushes.Black;
    public static readonly Brush MutedText = CreateBrush(42, 52, 65);
    public static readonly Brush InputBackground = Brushes.White;
    public static readonly Brush InputText = Brushes.Black;
    public static readonly Brush Accent = CreateBrush(0, 96, 160);
    public static readonly Brush Selected = CreateBrush(194, 231, 255);

    public static void Apply(Window window)
    {
        window.Background = Background;
        window.Foreground = Text;
    }

    public static TextBlock TextBlock(string text, double fontSize = 14, FontWeight? weight = null)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Text,
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal
        };
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.Background = InputBackground;
        textBox.Foreground = InputText;
        textBox.CaretBrush = InputText;
        textBox.BorderBrush = Accent;
        textBox.SelectionBrush = Selected;
    }

    public static void StyleListBox(ListBox listBox, string? displayPath = null)
    {
        listBox.Background = InputBackground;
        listBox.Foreground = InputText;
        listBox.BorderBrush = Accent;
        listBox.DisplayMemberPath = string.Empty;
        listBox.ItemTemplate = CreateReadableItemTemplate(displayPath);
    }

    public static void StyleListView(ListView listView)
    {
        listView.Background = InputBackground;
        listView.Foreground = InputText;
        listView.BorderBrush = Accent;
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.Foreground = Text;
    }

    public static Button Button(string text, Action action)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(4),
            Padding = new Thickness(10, 5, 10, 5),
            Background = Panel,
            Foreground = Brushes.White,
            BorderBrush = Accent
        };
        button.Click += (_, _) => action();
        return button;
    }

    public static DataTemplate CreateTextCellTemplate(string displayPath)
    {
        return CreateReadableItemTemplate(displayPath);
    }

    private static DataTemplate CreateReadableItemTemplate(string? displayPath)
    {
        var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
        factory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding(displayPath));
        factory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, InputText);
        factory.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 14.0);
        factory.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(4, 2, 4, 2));
        return new DataTemplate { VisualTree = factory };
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
