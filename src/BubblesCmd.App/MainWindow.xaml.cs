using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BubblesCmd.App.Dialogs;
using BubblesCmd.App.Terminal;
using BubblesCmd.Core.Models;
using BubblesCmd.Core.Services;
using Microsoft.Win32;

namespace BubblesCmd.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly DiagnosticLogger _diagnosticLogger = new();
    private readonly ShellProfileDetector _profileDetector = new();
    private readonly CommandDiscoveryService _commandDiscoveryService = new();
    private readonly PasteSafetyAnalyzer _pasteSafetyAnalyzer = new();
    private readonly List<TerminalTabState> _tabs = [];
    private readonly List<ClosedTabState> _recentlyClosedTabs = [];
    private AppSettings _settings = new();
    private IReadOnlyList<ShellProfile> _profiles = [];
    private IReadOnlyList<ProfileMenuItem> _profileMenuItems = [];
    private bool _profileSelectionReady;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (IsProcessElevated())
        {
            Title = "Administrator: Bubbles CMD 0.0.3";
            StatusTextBlock.Text = "Running as administrator.";
        }

        _settings = _settingsStore.Load();
        _diagnosticLogger.Enabled = _settings.DiagnosticLoggingEnabled;
        _diagnosticLogger.Info("app.loaded");
        _profiles = _profileDetector.DetectProfiles(_settings.CustomProfiles);
        _profileMenuItems = CreateProfileMenuItems(_profiles);
        ProfileComboBox.ItemsSource = _profileMenuItems;

        if (_profiles.Count == 0)
        {
            StatusTextBlock.Text = "No shell profiles were detected on this machine.";
            return;
        }

        SelectProfileInMenu(_profiles.FirstOrDefault(profile => profile.Id == _settings.DefaultProfileId)
            ?? _profiles.First());

        RestoreWorkspaceOrOpenDefault();
        _profileSelectionReady = true;
    }

    private void NewTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GetSelectedProfile() is { } profile)
        {
            OpenNewTab(profile);
        }
    }

    private void ProfileComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_profileSelectionReady || ProfileComboBox.SelectedItem is not ProfileMenuItem item)
        {
            return;
        }

        OpenNewTab(item.Profile);
    }

    private void CloseTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        CloseActiveTab();
    }

    private void DuplicateTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GetActiveTab() is { } activeTab)
        {
            OpenNewTab(activeTab.Profile);
        }
    }

    private void RestartTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        var profile = activeTab.Profile;
        if (CloseTab(activeTab, askBeforeClosingRunningSession: true))
        {
            OpenNewTab(profile);
        }
    }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GetActiveTab()?.View.CopySelection() == true)
        {
            StatusTextBlock.Text = "Copied selection.";
        }
        else
        {
            StatusTextBlock.Text = "No selected text to copy.";
        }
    }

    private void RenameTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        RenameActiveTab();
    }

    private void PinTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        TogglePinActiveTab();
    }

    private void ReopenTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        ReopenRecentlyClosedTab();
    }

    private void MoveTabLeftButton_OnClick(object sender, RoutedEventArgs e)
    {
        MoveActiveTab(-1);
    }

    private void MoveTabRightButton_OnClick(object sender, RoutedEventArgs e)
    {
        MoveActiveTab(1);
    }

    private void TerminateTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        ForceTerminateActiveTab();
    }

    private void SplitVerticalButton_OnClick(object sender, RoutedEventArgs e)
    {
        SplitActivePane(Orientation.Vertical);
    }

    private void SplitHorizontalButton_OnClick(object sender, RoutedEventArgs e)
    {
        SplitActivePane(Orientation.Horizontal);
    }

    private void NextPaneButton_OnClick(object sender, RoutedEventArgs e)
    {
        FocusNextPane();
    }

    private void DuplicatePaneButton_OnClick(object sender, RoutedEventArgs e)
    {
        DuplicateActivePane();
    }

    private void ZoomPaneButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleActivePaneZoom();
    }

    private void ClosePaneButton_OnClick(object sender, RoutedEventArgs e)
    {
        CloseActivePane();
    }

    private void SwapPaneButton_OnClick(object sender, RoutedEventArgs e)
    {
        SwapActivePane();
    }

    private void PaneToTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        MoveActivePaneToNewTab();
    }

    private async void PasteButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PasteIntoActiveSessionAsync();
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GetActiveTab() is { } activeTab)
        {
            activeTab.View.Clear();
            StatusTextBlock.Text = "Cleared visible scrollback.";
        }
    }

    private void FindNextButton_OnClick(object sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void SnippetsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShowSnippets();
    }

    private async void CommandsButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowCommandsAsync();
    }

    private void PaletteButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShowCommandPalette();
    }

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void AboutButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShowAbout();
    }

    private void SearchTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FindNext();
            e.Handled = true;
        }
    }

    private void SessionTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SessionTabControl.SelectedItem is not TabItem selectedTab)
        {
            return;
        }

        var state = _tabs.FirstOrDefault(tab => tab.TabItem == selectedTab);
        if (state is null)
        {
            return;
        }

        state.View.FocusTerminal();
        UpdateStatusFor(state);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (GetSelectedProfile() is { } profile)
        {
            _settings.DefaultProfileId = profile.Id;
        }

        _settings.LastWorkspace = _tabs
            .Where(tab => tab.HasRunningSessions)
            .Select(tab => new SavedTab
            {
                ProfileId = tab.Profile.Id,
                Title = tab.Title,
                IsPinned = tab.IsPinned
            })
            .ToList();
        _settingsStore.Save(_settings);
        _diagnosticLogger.Info("app.closing", new Dictionary<string, string>
        {
            ["runningTabs"] = _settings.LastWorkspace.Count.ToString()
        });

        foreach (var tab in _tabs.ToArray())
        {
            tab.DisposeSessions();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.T)
        {
            if (GetSelectedProfile() is { } profile)
            {
                OpenNewTab(profile);
            }

            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && TryGetProfileShortcutNumber(e.Key, out var profileNumber))
        {
            OpenProfileByShortcut(profileNumber);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            CloseActiveTab();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.V)
        {
            _ = PasteIntoActiveSessionAsync();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.P)
        {
            ShowCommandPalette();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.S)
        {
            ShowSnippets();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.D)
        {
            DuplicateActivePane();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.R)
        {
            ReopenRecentlyClosedTab();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Left)
        {
            MoveActiveTab(-1);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Right)
        {
            MoveActiveTab(1);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Z)
        {
            ToggleActivePaneZoom();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Tab)
        {
            FocusNextPane();
            e.Handled = true;
        }
    }

    private void OpenNewTab(ShellProfile profile)
    {
        var pane = CreatePane(profile);
        if (pane is null)
        {
            return;
        }

        var state = new TerminalTabState(pane)
        {
            Title = profile.DisplayName
        };
        HookPaneFocus(state, pane);

        var tab = new TabItem
        {
            Header = profile.DisplayName,
            Content = state.PaneGrid
        };

        state.TabItem = tab;
        _tabs.Add(state);
        SessionTabControl.Items.Add(tab);
        SessionTabControl.SelectedItem = tab;
        pane.View.FocusTerminal();
        StatusTextBlock.Text = $"Started {profile.DisplayName}.";
    }

    private TerminalPaneState? CreatePane(ShellProfile profile)
    {
        if (profile.RunAsAdministrator && !IsProcessElevated())
        {
            var result = MessageBox.Show(
                this,
                $"'{profile.DisplayName}' is configured to run as administrator. Restart Bubbles CMD as administrator now?",
                "Administrator Profile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                RestartApplicationAsAdministrator();
            }

            return null;
        }

        TerminalView terminalView;
        try
        {
            terminalView = new TerminalView(profile);
            terminalView.ApplyAppearance(_settings.Appearance);
        }
        catch (Exception ex)
        {
            _diagnosticLogger.Error("session.start.failed", ex);
            MessageBox.Show(this, ex.Message, "Unable To Start Shell", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }

        _diagnosticLogger.Info("session.started", new Dictionary<string, string>
        {
            ["profileId"] = profile.Id,
            ["administrator"] = IsProcessElevated().ToString()
        });

        return new TerminalPaneState(profile, terminalView);
    }

    private void CloseActiveTab()
    {
        if (SessionTabControl.SelectedItem is not TabItem selectedTab)
        {
            return;
        }

        var state = _tabs.FirstOrDefault(tab => tab.TabItem == selectedTab);
        if (state is null)
        {
            return;
        }

        CloseTab(state, askBeforeClosingRunningSession: true);
    }

    private bool CloseTab(TerminalTabState state, bool askBeforeClosingRunningSession)
    {
        if (state.IsPinned)
        {
            var pinResult = MessageBox.Show(
                this,
                $"'{state.Title}' is pinned. Close it anyway?",
                "Close Pinned Tab",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (pinResult != MessageBoxResult.Yes)
            {
                return false;
            }
        }

        if (askBeforeClosingRunningSession && state.HasRunningSessions)
        {
            var result = MessageBox.Show(
                this,
                $"Close the running session(s) in '{state.Title}'?",
                "Close Session",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return false;
            }
        }

        RememberClosedTab(state);
        state.DisposeSessions();
        SessionTabControl.Items.Remove(state.TabItem);
        _tabs.Remove(state);
        StatusTextBlock.Text = $"Closed {state.Profile.DisplayName}.";

        if (_tabs.Count == 0)
        {
            StatusTextBlock.Text = "No active tabs.";
        }

        return true;
    }

    private TerminalTabState? GetActiveTab()
    {
        return SessionTabControl.SelectedItem is TabItem selectedTab
            ? _tabs.FirstOrDefault(tab => tab.TabItem == selectedTab)
            : null;
    }

    private ShellProfile? GetSelectedProfile()
    {
        return (ProfileComboBox.SelectedItem as ProfileMenuItem)?.Profile;
    }

    private void SelectProfileInMenu(ShellProfile? profile)
    {
        if (profile is null)
        {
            ProfileComboBox.SelectedItem = null;
            return;
        }

        ProfileComboBox.SelectedItem = _profileMenuItems.FirstOrDefault(item => item.Profile.Id == profile.Id)
            ?? _profileMenuItems.FirstOrDefault();
    }

    private void OpenProfileByShortcut(int shortcutNumber)
    {
        var item = _profileMenuItems.FirstOrDefault(profile => profile.ShortcutNumber == shortcutNumber);
        if (item is null)
        {
            return;
        }

        _profileSelectionReady = false;
        SelectProfileInMenu(item.Profile);
        _profileSelectionReady = true;
        OpenNewTab(item.Profile);
    }

    private static IReadOnlyList<ProfileMenuItem> CreateProfileMenuItems(IReadOnlyList<ShellProfile> profiles)
    {
        return profiles
            .Select((profile, index) => new ProfileMenuItem(profile, index + 1))
            .ToArray();
    }

    private static bool TryGetProfileShortcutNumber(Key key, out int profileNumber)
    {
        profileNumber = key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            Key.D7 or Key.NumPad7 => 7,
            Key.D8 or Key.NumPad8 => 8,
            Key.D9 or Key.NumPad9 => 9,
            _ => 0
        };

        return profileNumber > 0;
    }

    private async Task PasteIntoActiveSessionAsync()
    {
        if (GetActiveTab() is not { } activeTab || !Clipboard.ContainsText())
        {
            return;
        }

        var text = Clipboard.GetText();
        if (ShouldBlockPasteForReview(text))
        {
            return;
        }

        var input = activeTab.View.IsBracketedPasteEnabled
            ? $"\u001b[200~{text}\u001b[201~"
            : text;

        await activeTab.View.SendInputAsync(input);
        activeTab.View.FocusTerminal();
        StatusTextBlock.Text = "Pasted into active session.";
    }

    private bool ShouldBlockPasteForReview(string text)
    {
        var review = _pasteSafetyAnalyzer.Analyze(
            text,
            _settings.ConfirmMultilinePaste,
            _settings.ConfirmDestructivePaste,
            _settings.ConfirmControlCharacterPaste);
        if (review.RequiresReview)
        {
            var result = MessageBox.Show(
                this,
                $"{string.Join("\r\n", review.Reasons)}\r\n\r\nPaste it into the active shell?",
                "Review Paste",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result != MessageBoxResult.Yes;
        }

        return false;
    }

    private void FindNext()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        StatusTextBlock.Text = activeTab.View.FindNext(SearchTextBox.Text)
            ? $"Found '{SearchTextBox.Text}'."
            : $"No match for '{SearchTextBox.Text}'.";
    }

    private void ShowCommandPalette()
    {
        var commands = new List<PaletteCommand>
        {
            new("New tab", () =>
            {
                if (GetSelectedProfile() is { } profile)
                {
                    OpenNewTab(profile);
                }
            }),
            new("Duplicate tab", () => DuplicateTabButton_OnClick(this, new RoutedEventArgs())),
            new("Rename tab", RenameActiveTab),
            new("Pin or unpin tab", TogglePinActiveTab),
            new("Reopen closed tab", ReopenRecentlyClosedTab),
            new("Restart tab", () => RestartTabButton_OnClick(this, new RoutedEventArgs())),
            new("Move tab left", () => MoveActiveTab(-1)),
            new("Move tab right", () => MoveActiveTab(1)),
            new("Close tab", CloseActiveTab),
            new("Split pane vertically", () => SplitActivePane(Orientation.Vertical)),
            new("Split pane horizontally", () => SplitActivePane(Orientation.Horizontal)),
            new("Duplicate active pane", DuplicateActivePane),
            new("Focus next pane", FocusNextPane),
            new("Zoom or restore active pane", ToggleActivePaneZoom),
            new("Close active pane", CloseActivePane),
            new("Swap active pane left/up", SwapActivePane),
            new("Move active pane to new tab", MoveActivePaneToNewTab),
            new("Force terminate shell", ForceTerminateActiveTab),
            new("Search output", () =>
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
            }),
            new("Copy selected output", () => CopyButton_OnClick(this, new RoutedEventArgs())),
            new("Paste clipboard", () => _ = PasteIntoActiveSessionAsync()),
            new("Clear visible scrollback", () => ClearButton_OnClick(this, new RoutedEventArgs())),
            new("Open snippets", ShowSnippets),
            new("Open command browser", () => _ = ShowCommandsAsync()),
            new("Open settings", ShowSettings),
            new("Save workspace", SaveWorkspace),
            new("Save output", SaveActiveOutput),
            new("Copy starting directory", CopyActiveStartingDirectory),
            new("Open starting folder", OpenActiveStartingFolder),
            new("Open diagnostics log", OpenDiagnosticsLog),
            new("Clear diagnostics log", ClearDiagnosticsLog),
            new("About Bubbles CMD", ShowAbout)
        };

        var dialog = new CommandPaletteWindow(commands) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            dialog.SelectedCommand?.Execute();
        }
    }

    private void ShowSnippets()
    {
        if (_settings.Snippets.Count == 0)
        {
            StatusTextBlock.Text = "No snippets configured.";
            return;
        }

        var dialog = new SnippetWindow(_settings.Snippets) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedSnippet is { } snippet)
        {
            InsertSnippet(snippet);
        }
    }

    private async Task ShowCommandsAsync()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        StatusTextBlock.Text = "Discovering commands...";
        IReadOnlyList<DiscoveredCommand> commands;
        try
        {
            commands = await Task.Run(() => _commandDiscoveryService.DiscoverForProfile(activeTab.Profile));
        }
        catch (Exception ex)
        {
            _diagnosticLogger.Error("commands.discovery.failed", ex);
            StatusTextBlock.Text = $"Command discovery failed: {ex.Message}";
            return;
        }

        _diagnosticLogger.Info("commands.discovered", new Dictionary<string, string>
        {
            ["profileId"] = activeTab.Profile.Id,
            ["count"] = commands.Count.ToString()
        });

        var dialog = new CommandsWindow(commands) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedCommand is { } command)
        {
            await activeTab.View.SendInputAsync(command.Name);
            activeTab.View.FocusTerminal();
            StatusTextBlock.Text = $"Inserted command '{command.Name}'.";
        }
        else
        {
            StatusTextBlock.Text = $"Discovered {commands.Count} command(s).";
        }
    }

    private void InsertSnippet(CommandSnippet snippet)
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        if (snippet.RequiresConfirmation || snippet.RequiresAdministrator)
        {
            var warning = snippet.RequiresAdministrator
                ? "This snippet is marked as requiring administrator review."
                : "Insert this snippet into the active shell input?";
            var result = MessageBox.Show(
                this,
                $"{warning}\r\n\r\n{snippet.Command}",
                "Insert Snippet",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _ = activeTab.View.SendInputAsync(snippet.Command);
        activeTab.View.FocusTerminal();
        StatusTextBlock.Text = $"Inserted snippet '{snippet.Name}'.";
    }

    private void ShowSettings()
    {
        var dialog = new SettingsWindow(_settings, _settingsStore) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _settingsStore.Save(_settings);
        _diagnosticLogger.Enabled = _settings.DiagnosticLoggingEnabled;
        _diagnosticLogger.Info("settings.saved");
        ApplyAppearanceToTabs();
        RefreshProfiles();
        StatusTextBlock.Text = "Settings saved.";
    }

    private void ShowAbout()
    {
        var dialog = new AboutWindow(_diagnosticLogger) { Owner = this };
        dialog.ShowDialog();
    }

    private void SaveWorkspace()
    {
        _settings.LastWorkspace = _tabs
            .Where(tab => tab.HasRunningSessions)
            .Select(tab => new SavedTab
            {
                ProfileId = tab.Profile.Id,
                Title = tab.Title,
                IsPinned = tab.IsPinned
            })
            .ToList();
        _settingsStore.Save(_settings);
        _diagnosticLogger.Info("workspace.saved", new Dictionary<string, string>
        {
            ["runningTabs"] = _settings.LastWorkspace.Count.ToString()
        });
        StatusTextBlock.Text = $"Saved workspace with {_settings.LastWorkspace.Count} running tab(s).";
    }

    private void SaveActiveOutput()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
            FileName = $"{SanitizeFileName(activeTab.Title)}.txt"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, activeTab.View.GetSelectedTextOrPlainText());
        _diagnosticLogger.Info("output.saved", new Dictionary<string, string>
        {
            ["profileId"] = activeTab.Profile.Id
        });
        StatusTextBlock.Text = $"Saved output to {dialog.FileName}.";
    }

    private void CopyActiveStartingDirectory()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        Clipboard.SetText(activeTab.Profile.StartingDirectory);
        StatusTextBlock.Text = "Copied starting directory.";
    }

    private void OpenActiveStartingFolder()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = WindowsPathQuoter.QuoteForShell(activeTab.Profile.StartingDirectory),
                UseShellExecute = true
            });
            StatusTextBlock.Text = "Opened starting folder.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Could not open folder: {ex.Message}";
        }
    }

    private void OpenDiagnosticsLog()
    {
        if (!File.Exists(_diagnosticLogger.LogFilePath))
        {
            StatusTextBlock.Text = "Diagnostics log has not been created.";
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _diagnosticLogger.LogFilePath,
            UseShellExecute = true
        });
    }

    private void ClearDiagnosticsLog()
    {
        _diagnosticLogger.Clear();
        StatusTextBlock.Text = "Diagnostics log cleared.";
    }

    private void RefreshProfiles()
    {
        var selectedProfileId = GetSelectedProfile()?.Id ?? _settings.DefaultProfileId;
        _profiles = _profileDetector.DetectProfiles(_settings.CustomProfiles);
        _profileMenuItems = CreateProfileMenuItems(_profiles);
        _profileSelectionReady = false;
        ProfileComboBox.ItemsSource = _profileMenuItems;
        SelectProfileInMenu(_profiles.FirstOrDefault(profile => profile.Id == selectedProfileId)
            ?? _profiles.FirstOrDefault());
        _profileSelectionReady = true;
        _diagnosticLogger.Info("profiles.refreshed", new Dictionary<string, string>
        {
            ["count"] = _profiles.Count.ToString()
        });
    }

    private void RestoreWorkspaceOrOpenDefault()
    {
        var restoredCount = 0;

        if (_settings.RestorePreviousWorkspace)
        {
            foreach (var savedTab in _settings.LastWorkspace)
            {
                var profile = _profiles.FirstOrDefault(item => item.Id == savedTab.ProfileId);
                if (profile is null)
                {
                    continue;
                }

                OpenNewTab(profile);
                if (GetActiveTab() is { } restoredTab)
                {
                    restoredTab.Title = string.IsNullOrWhiteSpace(savedTab.Title)
                        ? profile.DisplayName
                        : savedTab.Title;
                    restoredTab.IsPinned = savedTab.IsPinned;
                    UpdateTabHeader(restoredTab);
                }

                restoredCount++;
            }
        }

        if (restoredCount == 0)
        {
            if (GetSelectedProfile() is { } profile)
            {
                OpenNewTab(profile);
            }
        }
    }

    private void UpdateStatusFor(TerminalTabState state)
    {
        var status = state.View.IsRunning
            ? "Running"
            : state.View.ExitCode is { } exitCode
                ? $"Exited {exitCode}"
                : "Stopped";

        StatusTextBlock.Text = $"{state.Profile.DisplayName} | {status} | Started {state.View.StartedAt:t}";
    }

    private void ApplyAppearanceToTabs()
    {
        foreach (var tab in _tabs)
        {
            foreach (var pane in tab.Panes)
            {
                pane.View.ApplyAppearance(_settings.Appearance);
            }
        }
    }

    private void SplitActivePane(Orientation orientation)
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        var pane = CreatePane(activeTab.Profile);
        if (pane is null)
        {
            return;
        }

        activeTab.AddPane(pane, orientation);
        HookPaneFocus(activeTab, pane);
        pane.View.FocusTerminal();
        UpdateTabHeader(activeTab);
        StatusTextBlock.Text = orientation == Orientation.Vertical
            ? "Split active tab into side-by-side panes."
            : "Split active tab into stacked panes.";
    }

    private void DuplicateActivePane()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        var pane = CreatePane(activeTab.Profile);
        if (pane is null)
        {
            return;
        }

        activeTab.AddPane(pane, Orientation.Vertical);
        HookPaneFocus(activeTab, pane);
        pane.View.FocusTerminal();
        UpdateTabHeader(activeTab);
        StatusTextBlock.Text = $"Duplicated pane using {pane.Profile.DisplayName}.";
    }

    private void CloseActivePane()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        if (activeTab.Panes.Count <= 1)
        {
            CloseActiveTab();
            return;
        }

        var pane = activeTab.ActivePane;
        if (pane.View.IsRunning)
        {
            var result = MessageBox.Show(
                this,
                $"Close the active pane running {pane.Profile.DisplayName}?",
                "Close Pane",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        pane.View.Dispose();
        activeTab.RemovePane(pane);
        activeTab.View.FocusTerminal();
        UpdateTabHeader(activeTab);
        StatusTextBlock.Text = $"Closed pane. {activeTab.Panes.Count} pane(s) remain.";
    }

    private void SwapActivePane()
    {
        if (GetActiveTab() is not { } activeTab || activeTab.Panes.Count <= 1)
        {
            StatusTextBlock.Text = "Need at least two panes to swap.";
            return;
        }

        activeTab.SwapActivePaneWithPrevious();
        activeTab.View.FocusTerminal();
        StatusTextBlock.Text = "Swapped active pane.";
    }

    private void MoveActivePaneToNewTab()
    {
        if (GetActiveTab() is not { } activeTab || activeTab.Panes.Count <= 1)
        {
            StatusTextBlock.Text = "Need at least two panes before moving a pane to a new tab.";
            return;
        }

        var pane = activeTab.ActivePane;
        if (!activeTab.DetachPane(pane))
        {
            return;
        }

        var state = new TerminalTabState(pane)
        {
            Title = pane.Profile.DisplayName
        };
        HookPaneFocus(state, pane);

        var tab = new TabItem
        {
            Header = state.Title,
            Content = state.PaneGrid
        };

        state.TabItem = tab;
        _tabs.Add(state);
        SessionTabControl.Items.Add(tab);
        SessionTabControl.SelectedItem = tab;
        pane.View.FocusTerminal();
        UpdateTabHeader(activeTab);
        StatusTextBlock.Text = $"Moved pane to new tab '{state.Title}'.";
    }

    private void FocusNextPane()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        activeTab.FocusNextPane();
        UpdateStatusFor(activeTab);
    }

    private void ToggleActivePaneZoom()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        activeTab.TogglePaneZoom();
        activeTab.View.FocusTerminal();
        StatusTextBlock.Text = activeTab.IsPaneZoomed
            ? $"Zoomed pane in '{activeTab.Title}'."
            : $"Restored panes in '{activeTab.Title}'.";
    }

    private void HookPaneFocus(TerminalTabState tab, TerminalPaneState pane)
    {
        pane.View.TerminalFocused += (_, _) =>
        {
            tab.SetActivePane(pane);
            if (SessionTabControl.SelectedItem == tab.TabItem)
            {
                UpdateStatusFor(tab);
            }
        };
    }

    private void RenameActiveTab()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        var dialog = new TextPromptWindow("Rename Tab", "Tab name", activeTab.Title) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        activeTab.Title = dialog.Value;
        UpdateTabHeader(activeTab);
        StatusTextBlock.Text = $"Renamed tab to '{activeTab.Title}'.";
    }

    private void TogglePinActiveTab()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        activeTab.IsPinned = !activeTab.IsPinned;
        UpdateTabHeader(activeTab);
        StatusTextBlock.Text = activeTab.IsPinned
            ? $"Pinned '{activeTab.Title}'."
            : $"Unpinned '{activeTab.Title}'.";
    }

    private void MoveActiveTab(int direction)
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        var currentIndex = _tabs.IndexOf(activeTab);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = currentIndex + direction;
        if (targetIndex < 0 || targetIndex >= _tabs.Count)
        {
            StatusTextBlock.Text = direction < 0
                ? $"'{activeTab.Title}' is already the first tab."
                : $"'{activeTab.Title}' is already the last tab.";
            return;
        }

        _tabs.RemoveAt(currentIndex);
        _tabs.Insert(targetIndex, activeTab);
        SessionTabControl.Items.Remove(activeTab.TabItem);
        SessionTabControl.Items.Insert(targetIndex, activeTab.TabItem);
        SessionTabControl.SelectedItem = activeTab.TabItem;
        activeTab.View.FocusTerminal();

        StatusTextBlock.Text = direction < 0
            ? $"Moved '{activeTab.Title}' left."
            : $"Moved '{activeTab.Title}' right.";
    }

    private void ReopenRecentlyClosedTab()
    {
        if (_recentlyClosedTabs.Count == 0)
        {
            StatusTextBlock.Text = "No recently closed tabs.";
            return;
        }

        var closedTab = _recentlyClosedTabs[^1];
        _recentlyClosedTabs.RemoveAt(_recentlyClosedTabs.Count - 1);
        OpenNewTab(closedTab.Profile);
        if (GetActiveTab() is { } reopenedTab)
        {
            reopenedTab.Title = closedTab.Title;
            reopenedTab.IsPinned = closedTab.WasPinned;
            UpdateTabHeader(reopenedTab);
            StatusTextBlock.Text = $"Reopened '{reopenedTab.Title}'.";
        }
    }

    private void ForceTerminateActiveTab()
    {
        if (GetActiveTab() is not { } activeTab)
        {
            return;
        }

        if (!activeTab.View.IsRunning)
        {
            StatusTextBlock.Text = "The active session has already exited.";
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Force terminate '{activeTab.Title}'? Unsaved work in the active shell may be lost.",
            "Terminate Session",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        activeTab.View.Terminate();
        _diagnosticLogger.Info("session.terminated", new Dictionary<string, string>
        {
            ["profileId"] = activeTab.Profile.Id
        });
        StatusTextBlock.Text = $"Terminate signal sent to '{activeTab.Title}'.";
    }

    private void RememberClosedTab(TerminalTabState state)
    {
        _recentlyClosedTabs.Add(new ClosedTabState(state.Profile, state.Title, state.IsPinned, DateTimeOffset.Now));
        if (_recentlyClosedTabs.Count > 12)
        {
            _recentlyClosedTabs.RemoveAt(0);
        }
    }

    private static void UpdateTabHeader(TerminalTabState state)
    {
        var prefix = state.IsPinned ? "[Pinned] " : string.Empty;
        if (state.Profile.RunAsAdministrator || IsProcessElevated())
        {
            prefix += "[Admin] ";
        }

        var suffix = state.HasRunningSessions ? string.Empty : " (Exited)";
        state.TabItem.Header = $"{prefix}{state.Title}{suffix}";
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '-');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "bubbles-output" : fileName;
    }

    private static bool IsProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void RestartApplicationAsAdministrator()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        });
    }
}

internal sealed record ProfileMenuItem(ShellProfile Profile, int ShortcutNumber)
{
    public string DisplayName => Profile.DisplayName;

    public string IconGlyph => Profile.IconGlyph;

    public string ShortcutText => ShortcutNumber <= 9 ? $"Ctrl+Shift+{ShortcutNumber}" : string.Empty;
}
