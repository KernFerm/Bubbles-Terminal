using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BubblesCmd.Core.Services;

namespace BubblesCmd.App.Dialogs;

internal sealed class AboutWindow : Window
{
    public AboutWindow(DiagnosticLogger logger)
    {
        Title = "About Bubbles CMD";
        Width = 560;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DialogTheme.Apply(this);

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.2";

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Background = DialogTheme.Background;
        panel.Children.Add(new TextBlock
        {
            Text = "Bubbles CMD",
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Foreground = DialogTheme.Text
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Version {version}",
            Margin = new Thickness(0, 4, 0, 18),
            Foreground = DialogTheme.MutedText
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Bubbles CMD hosts the real installed Windows shells through ConPTY. It does not replace cmd.exe, powershell.exe, or pwsh.exe.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = DialogTheme.Text
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Privacy: telemetry, analytics, command uploads, clipboard uploads, and crash uploads are not implemented. Settings, snippets, workspaces, and optional diagnostics stay local.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = DialogTheme.Text
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Settings: {Paths.SettingsFilePath}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = DialogTheme.Text
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Diagnostics: {logger.LogFilePath}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18),
            Foreground = DialogTheme.Text
        });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(DialogTheme.Button("Open Data Folder", () => OpenFolder(Paths.AppDataDirectory)));
        buttons.Children.Add(DialogTheme.Button("Open Log", () =>
        {
            if (File.Exists(logger.LogFilePath))
            {
                Process.Start(new ProcessStartInfo { FileName = logger.LogFilePath, UseShellExecute = true });
            }
        }));
        buttons.Children.Add(DialogTheme.Button("Close", () => Close()));
        panel.Children.Add(buttons);

        Content = panel;
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = WindowsPathQuoter.QuoteForShell(path),
            UseShellExecute = true
        });
    }

}
