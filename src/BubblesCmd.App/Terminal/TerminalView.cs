using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BubblesCmd.Core.Models;
using BubblesCmd.Core.Services;
using EasyWindowsTerminalControl;

namespace BubblesCmd.App.Terminal;

internal sealed class TerminalView : Border, IDisposable
{
    private readonly EasyTerminalControl _terminalControl;
    private readonly ShellProfile _profile;
    private bool _disposed;
    private bool _isRunning = true;
    private string _lastSearchText = string.Empty;

    public TerminalView(ShellProfile profile)
    {
        _profile = profile;
        CornerRadius = new CornerRadius(18);
        Background = new SolidColorBrush(Color.FromRgb(12, 12, 12));
        Padding = new Thickness(2);
        Focusable = true;
        AllowDrop = true;

        _terminalControl = new EasyTerminalControl
        {
            StartupCommandLine = CommandLineBuilder.Build(profile.ExecutablePath, profile.Arguments),
            WorkingDirectory = ResolveWorkingDirectory(profile.StartingDirectory),
            FontSizeWhenSettingTheme = 16,
            FontFamilyWhenSettingTheme = new FontFamily("Cascadia Mono"),
            LogConPTYOutput = true,
            Win32InputMode = true
        };

        Child = _terminalControl;

        DragEnter += OnDragEnter;
        Drop += OnDrop;
        GotKeyboardFocus += (_, _) => TerminalFocused?.Invoke(this, EventArgs.Empty);
        _terminalControl.GotKeyboardFocus += (_, _) => TerminalFocused?.Invoke(this, EventArgs.Empty);
        Loaded += (_, _) => FocusTerminal();
    }

    public event EventHandler? TerminalFocused;

    public bool IsRunning => _isRunning;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

    public int? ExitCode { get; private set; }

    public bool IsBracketedPasteEnabled => false;

    public void ApplyAppearance(TerminalAppearanceSettings appearance)
    {
        var background = ParseBrush(appearance.HighContrast ? "#000000" : appearance.BackgroundColor, Brushes.Black);
        Background = background;
        _terminalControl.FontSizeWhenSettingTheme = (int)Math.Round(appearance.FontSize);
        _terminalControl.FontFamilyWhenSettingTheme = new FontFamily(appearance.FontFamily);
    }

    public void Clear()
    {
        _terminalControl.ConPTYTerm?.ClearUITerminal();
        _lastSearchText = string.Empty;
    }

    public bool CopySelection()
    {
        FocusTerminal();
        if (ApplicationCommands.Copy.CanExecute(null, _terminalControl))
        {
            ApplicationCommands.Copy.Execute(null, _terminalControl);
            return true;
        }

        return false;
    }

    public void FocusTerminal()
    {
        Focus();
        _terminalControl.Focus();
    }

    public string GetPlainText()
    {
        try
        {
            return _terminalControl.ConPTYTerm?.GetConsoleText() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public string GetSelectedTextOrPlainText()
    {
        return GetPlainText();
    }

    public bool FindNext(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return false;
        }

        _lastSearchText = searchText;
        return GetPlainText().Contains(_lastSearchText, StringComparison.CurrentCultureIgnoreCase);
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested || string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        _terminalControl.ConPTYTerm?.WriteToTerm(text);
        FocusTerminal();
        return Task.CompletedTask;
    }

    public void Restart()
    {
        _terminalControl.StartupCommandLine = CommandLineBuilder.Build(_profile.ExecutablePath, _profile.Arguments);
        _terminalControl.WorkingDirectory = ResolveWorkingDirectory(_profile.StartingDirectory);
        _terminalControl.RestartTerm();
        _isRunning = true;
        ExitCode = null;
        FocusTerminal();
    }

    public void Terminate()
    {
        if (!_isRunning)
        {
            return;
        }

        _terminalControl.DisconnectConPTYTerm();
        _isRunning = false;
        ExitCode = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Terminate();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
        {
            return;
        }

        var quotedPaths = string.Join(" ", paths.Select(WindowsPathQuoter.QuoteForShell));
        await SendInputAsync(quotedPaths);
        e.Handled = true;
    }

    private static Brush ParseBrush(string value, Brush fallback)
    {
        try
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(value)!;
            brush.Freeze();
            return brush;
        }
        catch
        {
            return fallback;
        }
    }

    private static string ResolveWorkingDirectory(string startingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(startingDirectory) && Directory.Exists(startingDirectory))
        {
            return startingDirectory;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home) ? Environment.CurrentDirectory : home;
    }
}
