using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BubblesCmd.App.Services;
using BubblesCmd.Core.Models;
using BubblesCmd.Core.Services;
using EasyWindowsTerminalControl;
using Microsoft.Terminal.Wpf;

namespace BubblesCmd.App.Terminal;

internal sealed class TerminalView : Border, IDisposable
{
    private readonly EasyTerminalControl _terminalControl;
    private readonly ShellProfile _profile;
    private readonly TerminalControlSequenceParser _controlSequenceParser = new();
    private bool _disposed;
    private bool _isRunning = true;
    private string _lastSearchText = string.Empty;
    private int _lastSearchIndex = -1;
    private int _lastSearchMatchCount;
    private bool _isBracketedPasteEnabled;
    private TerminalAppearanceSettings _appearance = new();

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
        AttachTerminalInterceptors();

        DragEnter += OnDragEnter;
        Drop += OnDrop;
        GotKeyboardFocus += (_, _) => TerminalFocused?.Invoke(this, EventArgs.Empty);
        _terminalControl.GotKeyboardFocus += (_, _) => TerminalFocused?.Invoke(this, EventArgs.Empty);
        Loaded += async (_, _) =>
        {
            AttachTerminalInterceptors();
            FocusTerminal();
            await RunProfileStartupAsync();
        };
    }

    public event EventHandler? TerminalFocused;

    public event EventHandler<string>? TitleSuggested;

    public event EventHandler<int>? BellReceived;

    public bool IsRunning => _isRunning;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

    public int? ExitCode { get; private set; }

    public bool IsBracketedPasteEnabled => _isBracketedPasteEnabled;

    public void ApplyAppearance(TerminalAppearanceSettings appearance)
    {
        _appearance = appearance;
        var background = ParseBrush(appearance.HighContrast ? "#000000" : appearance.BackgroundColor, Brushes.Black);
        var accent = ParseBrush(appearance.AccentColor, Brushes.DeepSkyBlue);
        Background = background;
        BorderBrush = accent;
        _terminalControl.FontSizeWhenSettingTheme = (int)Math.Round(appearance.FontSize);
        _terminalControl.FontFamilyWhenSettingTheme = new FontFamily(appearance.FontFamily);
        Padding = appearance.LineHeight <= 1
            ? new Thickness(2)
            : new Thickness(2, Math.Min(18, appearance.LineHeight / 4), 2, Math.Min(18, appearance.LineHeight / 4));
        ApplyTheme(appearance);
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
        var selected = TryGetSelectedText();
        return string.IsNullOrWhiteSpace(selected) ? GetPlainText() : selected;
    }

    public TerminalSearchResult FindNext(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _lastSearchText = string.Empty;
            _lastSearchIndex = -1;
            _lastSearchMatchCount = 0;
            return new TerminalSearchResult(false, -1, 0, 0, string.Empty);
        }

        var text = GetPlainText();
        var comparison = StringComparison.CurrentCultureIgnoreCase;
        if (!string.Equals(_lastSearchText, searchText, comparison))
        {
            _lastSearchText = searchText;
            _lastSearchIndex = -1;
        }

        var startIndex = Math.Min(text.Length, Math.Max(0, _lastSearchIndex + 1));
        var foundIndex = text.IndexOf(_lastSearchText, startIndex, comparison);
        if (foundIndex < 0 && startIndex > 0)
        {
            foundIndex = text.IndexOf(_lastSearchText, comparison);
        }

        _lastSearchMatchCount = CountMatches(text, _lastSearchText, comparison);
        _lastSearchIndex = foundIndex;

        var matchNumber = foundIndex >= 0 ? CountMatches(text[..(foundIndex + _lastSearchText.Length)], _lastSearchText, comparison) : 0;
        return foundIndex >= 0
            ? new TerminalSearchResult(true, foundIndex, _lastSearchMatchCount, matchNumber, BuildPreview(text, foundIndex, _lastSearchText.Length))
            : new TerminalSearchResult(false, -1, 0, 0, string.Empty);
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

        try
        {
            var term = _terminalControl.ConPTYTerm;
            if (term is not null)
            {
                if (!term.Process.HasExited)
                {
                    term.Process.Kill(true);
                }

                term.StopExternalTermOnly();
            }
        }
        catch
        {
        }
        finally
        {
            _terminalControl.DisconnectConPTYTerm();
            _isRunning = false;
            ExitCode = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Terminate();
        if (_terminalControl is IDisposable disposableControl)
        {
            disposableControl.Dispose();
        }
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

    private void ApplyTheme(TerminalAppearanceSettings appearance)
    {
        var background = appearance.HighContrast ? "#000000" : appearance.BackgroundColor;
        var foreground = appearance.HighContrast ? "#FFFFFF" : appearance.ForegroundColor;
        var accent = appearance.HighContrast ? "#FFFF00" : appearance.AccentColor;
        var theme = new TerminalTheme
        {
            DefaultBackground = EasyTerminalControl.ColorToVal(ParseColor(background, Colors.Black)),
            DefaultForeground = EasyTerminalControl.ColorToVal(ParseColor(foreground, Colors.White)),
            DefaultSelectionBackground = EasyTerminalControl.ColorToVal(ParseColor(accent, Colors.DeepSkyBlue)),
            CursorStyle = appearance.ReducedMotion ? CursorStyle.SteadyBar : CursorStyle.BlinkingBar,
            ColorTable =
            [
                EasyTerminalControl.ColorToVal(ParseColor("#0C0C0C", Colors.Black)),
                EasyTerminalControl.ColorToVal(ParseColor("#C50F1F", Colors.DarkRed)),
                EasyTerminalControl.ColorToVal(ParseColor("#13A10E", Colors.DarkGreen)),
                EasyTerminalControl.ColorToVal(ParseColor("#C19C00", Colors.Goldenrod)),
                EasyTerminalControl.ColorToVal(ParseColor("#0037DA", Colors.DarkBlue)),
                EasyTerminalControl.ColorToVal(ParseColor("#881798", Colors.DarkMagenta)),
                EasyTerminalControl.ColorToVal(ParseColor("#3A96DD", Colors.DarkCyan)),
                EasyTerminalControl.ColorToVal(ParseColor("#CCCCCC", Colors.LightGray)),
                EasyTerminalControl.ColorToVal(ParseColor("#767676", Colors.Gray)),
                EasyTerminalControl.ColorToVal(ParseColor("#E74856", Colors.Red)),
                EasyTerminalControl.ColorToVal(ParseColor("#16C60C", Colors.LimeGreen)),
                EasyTerminalControl.ColorToVal(ParseColor("#F9F1A5", Colors.Khaki)),
                EasyTerminalControl.ColorToVal(ParseColor("#3B78FF", Colors.RoyalBlue)),
                EasyTerminalControl.ColorToVal(ParseColor("#B4009E", Colors.MediumVioletRed)),
                EasyTerminalControl.ColorToVal(ParseColor("#61D6D6", Colors.Turquoise)),
                EasyTerminalControl.ColorToVal(ParseColor("#F2F2F2", Colors.White))
            ]
        };

        _terminalControl.Theme = theme;
    }

    private async Task RunProfileStartupAsync()
    {
        if (_disposed)
        {
            return;
        }

        var commands = BuildStartupCommands();
        if (commands.Count == 0)
        {
            return;
        }

        await Task.Delay(250);
        foreach (var command in commands)
        {
            await SendInputAsync(command);
        }
    }

    private List<string> BuildStartupCommands()
    {
        var commands = new List<string>();
        if (_profile.EnvironmentOverrides.Count > 0)
        {
            commands.AddRange(_profile.EnvironmentOverrides.Select(BuildEnvironmentCommand));
        }

        if (!string.IsNullOrWhiteSpace(_profile.StartupCommand))
        {
            commands.Add(AppendNewLine(_profile.StartupCommand!));
        }

        return commands;
    }

    private string BuildEnvironmentCommand(KeyValuePair<string, string> pair)
    {
        var executableName = Path.GetFileName(_profile.ExecutablePath);
        if (executableName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase))
        {
            return $"$env:{pair.Key} = '{pair.Value.Replace("'", "''", StringComparison.Ordinal)}'{Environment.NewLine}";
        }

        if (executableName.Equals("bash.exe", StringComparison.OrdinalIgnoreCase) ||
            executableName.Equals("wsl.exe", StringComparison.OrdinalIgnoreCase))
        {
            return $"export {pair.Key}='{pair.Value.Replace("'", "'\\''", StringComparison.Ordinal)}'{Environment.NewLine}";
        }

        return $"set \"{pair.Key}={pair.Value}\"{Environment.NewLine}";
    }

    private static string AppendNewLine(string command)
    {
        return command.EndsWith("\n", StringComparison.Ordinal) || command.EndsWith("\r", StringComparison.Ordinal)
            ? command
            : command + Environment.NewLine;
    }

    private void AttachTerminalInterceptors()
    {
        TryAttachDelegate("InterceptOutputToUITerminal", nameof(InterceptOutputToUiTerminal));
    }

    private void InterceptOutputToUiTerminal(ref Span<char> text)
    {
        if (text.Length == 0)
        {
            return;
        }

        var parsed = _controlSequenceParser.Parse(text.ToString());
        if (parsed.BracketedPasteEnabled.HasValue)
        {
            _isBracketedPasteEnabled = parsed.BracketedPasteEnabled.Value;
        }

        if (!string.IsNullOrWhiteSpace(parsed.WindowTitle))
        {
            TitleSuggested?.Invoke(this, parsed.WindowTitle);
        }

        if (parsed.BellCount > 0)
        {
            BellReceived?.Invoke(this, parsed.BellCount);
        }

        if (parsed.Text.Length == text.Length)
        {
            return;
        }

        text = parsed.Text.ToCharArray().AsSpan();
    }

    private void TryAttachDelegate(string memberName, string handlerName)
    {
        var conPtyTerm = _terminalControl.ConPTYTerm;
        if (conPtyTerm is null)
        {
            return;
        }

        var termType = conPtyTerm.GetType();
        var handlerMethod = GetType().GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (handlerMethod is null)
        {
            return;
        }

        if (termType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public) is { CanWrite: true } property &&
            typeof(Delegate).IsAssignableFrom(property.PropertyType))
        {
            var del = Delegate.CreateDelegate(property.PropertyType, this, handlerMethod, throwOnBindFailure: false);
            if (del is not null)
            {
                property.SetValue(conPtyTerm, del);
            }

            return;
        }

        var method = termType.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (method is null)
        {
            return;
        }

        var parameters = method.GetParameters();
        if (parameters.Length != 1 || !typeof(Delegate).IsAssignableFrom(parameters[0].ParameterType))
        {
            return;
        }

        var methodDelegate = Delegate.CreateDelegate(parameters[0].ParameterType, this, handlerMethod, throwOnBindFailure: false);
        if (methodDelegate is not null)
        {
            method.Invoke(conPtyTerm, [methodDelegate]);
        }
    }

    private string TryGetSelectedText()
    {
        try
        {
            var method = _terminalControl.GetType().GetMethod("GetSelectedText", BindingFlags.Instance | BindingFlags.Public);
            return method?.Invoke(_terminalControl, null) as string ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int CountMatches(string text, string searchText, StringComparison comparison)
    {
        var count = 0;
        var index = 0;
        while (index < text.Length)
        {
            index = text.IndexOf(searchText, index, comparison);
            if (index < 0)
            {
                break;
            }

            count++;
            index += searchText.Length;
        }

        return count;
    }

    private static string BuildPreview(string text, int foundIndex, int matchLength)
    {
        var previewStart = Math.Max(0, foundIndex - 30);
        var previewLength = Math.Min(text.Length - previewStart, matchLength + 60);
        return text.Substring(previewStart, previewLength)
            .Replace(Environment.NewLine, " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
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

    private static Color ParseColor(string value, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(value)!;
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
