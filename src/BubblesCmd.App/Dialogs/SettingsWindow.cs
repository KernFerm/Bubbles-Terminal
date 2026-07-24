using System.Windows;
using System.Windows.Controls;
using BubblesCmd.Core.Models;
using BubblesCmd.Core.Services;
using Microsoft.Win32;

namespace BubblesCmd.App.Dialogs;

internal sealed class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly SettingsValidator _validator = new();
    private readonly CheckBox _restoreWorkspaceCheckBox = new() { Content = "Restore previous workspace" };
    private readonly CheckBox _multilinePasteCheckBox = new() { Content = "Warn before multiline paste" };
    private readonly CheckBox _riskyPasteCheckBox = new() { Content = "Warn before risky paste" };
    private readonly CheckBox _controlCharacterPasteCheckBox = new() { Content = "Warn before hidden-control-character paste" };
    private readonly CheckBox _diagnosticLoggingCheckBox = new() { Content = "Enable local diagnostic logging" };
    private readonly CheckBox _highContrastCheckBox = new() { Content = "High contrast terminal colors" };
    private readonly CheckBox _reducedMotionCheckBox = new() { Content = "Reduce motion" };
    private readonly TextBox _scrollbackTextBox = new() { Width = 120 };
    private readonly TextBox _fontFamilyTextBox = new() { Width = 220 };
    private readonly TextBox _fontSizeTextBox = new() { Width = 120 };
    private readonly TextBox _lineHeightTextBox = new() { Width = 120 };
    private readonly TextBox _backgroundColorTextBox = new() { Width = 120 };
    private readonly TextBox _foregroundColorTextBox = new() { Width = 120 };
    private readonly TextBox _accentColorTextBox = new() { Width = 120 };
    private readonly ComboBox _themePresetComboBox = new() { Width = 220 };
    private readonly CheckBox _profileRunAsAdministratorCheckBox = new() { Content = "Request administrator launch" };
    private readonly TextBox _profileNameTextBox = new() { Width = 220 };
    private readonly TextBox _profilePathTextBox = new() { Width = 360 };
    private readonly TextBox _profileArgsTextBox = new() { Width = 360 };
    private readonly TextBox _profileStartDirectoryTextBox = new() { Width = 360 };
    private readonly TextBox _profileStartupCommandTextBox = new() { Width = 360 };
    private readonly TextBox _profileTitleTemplateTextBox = new() { Width = 360 };
    private readonly TextBox _profileIconGlyphTextBox = new() { Width = 120, Text = "\uE756" };
    private readonly TextBox _profileEnvironmentTextBox = new() { Width = 360, AcceptsReturn = true, Height = 84, TextWrapping = TextWrapping.Wrap };
    private readonly ListBox _customProfilesList = new() { Height = 130 };
    private readonly TextBox _snippetNameTextBox = new() { Width = 220 };
    private readonly TextBox _snippetCommandTextBox = new() { Width = 360 };
    private readonly ListBox _snippetsList = new() { Height = 130 };

    public SettingsWindow(AppSettings settings, SettingsStore settingsStore)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        Title = "Settings";
        Width = 760;
        Height = 720;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DialogTheme.Apply(this);

        var root = new ScrollViewer
        {
            Background = DialogTheme.Background,
            Content = BuildContent()
        };
        Content = root;
        ApplyControlTheme();
        LoadFromSettings();
    }

    private UIElement BuildContent()
    {
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Background = DialogTheme.Background;
        panel.Children.Add(Header("General"));
        panel.Children.Add(_restoreWorkspaceCheckBox);
        panel.Children.Add(_multilinePasteCheckBox);
        panel.Children.Add(_riskyPasteCheckBox);
        panel.Children.Add(_controlCharacterPasteCheckBox);
        panel.Children.Add(_diagnosticLoggingCheckBox);
        panel.Children.Add(Row("Scrollback lines", _scrollbackTextBox));
        panel.Children.Add(ButtonRow(
            Button("Import Settings", ImportSettings),
            Button("Export Settings", ExportSettings),
            Button("Reset Settings", ResetSettings)));

        panel.Children.Add(Header("Appearance"));
        panel.Children.Add(Row("Theme preset", _themePresetComboBox));
        panel.Children.Add(Row("Font family", _fontFamilyTextBox));
        panel.Children.Add(Row("Font size", _fontSizeTextBox));
        panel.Children.Add(Row("Line height", _lineHeightTextBox));
        panel.Children.Add(Row("Background", _backgroundColorTextBox));
        panel.Children.Add(Row("Foreground", _foregroundColorTextBox));
        panel.Children.Add(Row("Accent", _accentColorTextBox));
        panel.Children.Add(_highContrastCheckBox);
        panel.Children.Add(_reducedMotionCheckBox);

        panel.Children.Add(Header("Custom Profiles"));
        panel.Children.Add(_customProfilesList);
        panel.Children.Add(Row("Name", _profileNameTextBox));
        panel.Children.Add(Row("Executable", _profilePathTextBox, Button("Browse", BrowseProfileExecutable)));
        panel.Children.Add(Row("Arguments", _profileArgsTextBox));
        panel.Children.Add(Row("Start directory", _profileStartDirectoryTextBox));
        panel.Children.Add(Row("Startup command", _profileStartupCommandTextBox));
        panel.Children.Add(Row("Tab title", _profileTitleTemplateTextBox));
        panel.Children.Add(Row("Icon glyph", _profileIconGlyphTextBox));
        panel.Children.Add(Row("Env (KEY=VALUE)", _profileEnvironmentTextBox));
        panel.Children.Add(_profileRunAsAdministratorCheckBox);
        panel.Children.Add(ButtonRow(Button("Add Profile", AddProfile), Button("Remove Selected Profile", RemoveSelectedProfile)));

        panel.Children.Add(Header("Keyboard Shortcuts"));
        panel.Children.Add(DialogTheme.TextBlock("Ctrl+Shift+1..9 opens profile shortcuts. Ctrl+Shift+T opens a new tab. Ctrl+W closes a tab. Ctrl+Shift+D duplicates a pane. Ctrl+Shift+P opens the palette. Ctrl+Tab focuses the next pane.", 13));

        panel.Children.Add(Header("Snippets"));
        panel.Children.Add(_snippetsList);
        panel.Children.Add(Row("Name", _snippetNameTextBox));
        panel.Children.Add(Row("Command", _snippetCommandTextBox));
        panel.Children.Add(ButtonRow(Button("Add Snippet", AddSnippet), Button("Remove Selected Snippet", RemoveSelectedSnippet)));

        panel.Children.Add(ButtonRow(Button("Save", Save), Button("Cancel", () => DialogResult = false)));
        return panel;
    }

    private void ApplyControlTheme()
    {
        foreach (var checkBox in new[]
        {
            _restoreWorkspaceCheckBox,
            _multilinePasteCheckBox,
            _riskyPasteCheckBox,
            _controlCharacterPasteCheckBox,
            _diagnosticLoggingCheckBox,
            _highContrastCheckBox,
            _reducedMotionCheckBox,
            _profileRunAsAdministratorCheckBox
        })
        {
            DialogTheme.StyleCheckBox(checkBox);
        }
        SetReadableCheckBoxContent(
            _restoreWorkspaceCheckBox,
            _multilinePasteCheckBox,
            _riskyPasteCheckBox,
            _controlCharacterPasteCheckBox,
            _diagnosticLoggingCheckBox,
            _highContrastCheckBox,
            _reducedMotionCheckBox);

        foreach (var textBox in new[]
        {
            _scrollbackTextBox,
            _fontFamilyTextBox,
            _fontSizeTextBox,
            _lineHeightTextBox,
            _backgroundColorTextBox,
            _foregroundColorTextBox,
            _accentColorTextBox,
            _profileNameTextBox,
            _profilePathTextBox,
            _profileArgsTextBox,
            _profileStartDirectoryTextBox,
            _profileStartupCommandTextBox,
            _profileTitleTemplateTextBox,
            _profileIconGlyphTextBox,
            _profileEnvironmentTextBox,
            _snippetNameTextBox,
            _snippetCommandTextBox
        })
        {
            DialogTheme.StyleTextBox(textBox);
        }

        _themePresetComboBox.ItemsSource = new[] { "Bubbles Dark", "Command Prompt Classic", "PowerShell Blue", "High Contrast" };
        _themePresetComboBox.SelectionChanged += (_, _) => ApplyThemePreset(_themePresetComboBox.SelectedItem as string);

        DialogTheme.StyleListBox(_customProfilesList);
        DialogTheme.StyleListBox(_snippetsList);
    }

    private static void SetReadableCheckBoxContent(params CheckBox[] checkBoxes)
    {
        foreach (var checkBox in checkBoxes)
        {
            if (checkBox.Content is not string text)
            {
                continue;
            }

            checkBox.Content = new TextBlock
            {
                Text = text,
                Foreground = DialogTheme.Text,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    private void LoadFromSettings()
    {
        _restoreWorkspaceCheckBox.IsChecked = _settings.RestorePreviousWorkspace;
        _multilinePasteCheckBox.IsChecked = _settings.ConfirmMultilinePaste;
        _riskyPasteCheckBox.IsChecked = _settings.ConfirmDestructivePaste;
        _controlCharacterPasteCheckBox.IsChecked = _settings.ConfirmControlCharacterPaste;
        _diagnosticLoggingCheckBox.IsChecked = _settings.DiagnosticLoggingEnabled;
        _scrollbackTextBox.Text = _settings.ScrollbackLineLimit.ToString();
        _fontFamilyTextBox.Text = _settings.Appearance.FontFamily;
        _fontSizeTextBox.Text = _settings.Appearance.FontSize.ToString("0.##");
        _lineHeightTextBox.Text = _settings.Appearance.LineHeight.ToString("0.##");
        _backgroundColorTextBox.Text = _settings.Appearance.BackgroundColor;
        _foregroundColorTextBox.Text = _settings.Appearance.ForegroundColor;
        _accentColorTextBox.Text = _settings.Appearance.AccentColor;
        _highContrastCheckBox.IsChecked = _settings.Appearance.HighContrast;
        _reducedMotionCheckBox.IsChecked = _settings.Appearance.ReducedMotion;
        _themePresetComboBox.SelectedItem = _settings.Appearance.ThemeName;
        RefreshLists();
    }

    private void Save()
    {
        if (!int.TryParse(_scrollbackTextBox.Text, out var scrollback))
        {
            MessageBox.Show(this, "Scrollback line limit must be a number.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(_fontSizeTextBox.Text, out var fontSize))
        {
            MessageBox.Show(this, "Font size must be a number.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(_lineHeightTextBox.Text, out var lineHeight))
        {
            MessageBox.Show(this, "Line height must be a number.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.RestorePreviousWorkspace = _restoreWorkspaceCheckBox.IsChecked == true;
        _settings.ConfirmMultilinePaste = _multilinePasteCheckBox.IsChecked == true;
        _settings.ConfirmDestructivePaste = _riskyPasteCheckBox.IsChecked == true;
        _settings.ConfirmControlCharacterPaste = _controlCharacterPasteCheckBox.IsChecked == true;
        _settings.DiagnosticLoggingEnabled = _diagnosticLoggingCheckBox.IsChecked == true;
        _settings.ScrollbackLineLimit = scrollback;
        _settings.Appearance.FontFamily = _fontFamilyTextBox.Text.Trim();
        _settings.Appearance.ThemeName = (_themePresetComboBox.SelectedItem as string) ?? "Custom";
        _settings.Appearance.FontSize = fontSize;
        _settings.Appearance.LineHeight = lineHeight;
        _settings.Appearance.BackgroundColor = _backgroundColorTextBox.Text.Trim();
        _settings.Appearance.ForegroundColor = _foregroundColorTextBox.Text.Trim();
        _settings.Appearance.AccentColor = _accentColorTextBox.Text.Trim();
        _settings.Appearance.HighContrast = _highContrastCheckBox.IsChecked == true;
        _settings.Appearance.ReducedMotion = _reducedMotionCheckBox.IsChecked == true;

        var errors = _validator.Validate(_settings);
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void ImportSettings()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Bubbles CMD settings (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var imported = _settingsStore.LoadFrom(dialog.FileName);
            CopySettings(imported, _settings);
            LoadFromSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportSettings()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Bubbles CMD settings (*.json)|*.json|All files (*.*)|*.*",
            FileName = "bubbles-cmd-settings.json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _settingsStore.SaveTo(_settings, dialog.FileName);
        }
    }

    private void ResetSettings()
    {
        var result = MessageBox.Show(
            this,
            "Reset settings, custom profiles, snippets, and saved workspace to defaults?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        CopySettings(new AppSettings(), _settings);
        LoadFromSettings();
    }

    private void BrowseProfileExecutable()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _profilePathTextBox.Text = dialog.FileName;
        }
    }

    private void AddProfile()
    {
        var name = _profileNameTextBox.Text.Trim();
        var path = _profilePathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "Profile name and executable are required.", "Custom Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.CustomProfiles.Add(new ShellProfile(
            $"custom-{Guid.NewGuid():N}",
            name,
            path,
            _profileArgsTextBox.Text.Trim(),
            string.IsNullOrWhiteSpace(_profileStartDirectoryTextBox.Text)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : _profileStartDirectoryTextBox.Text.Trim(),
            RunAsAdministrator: _profileRunAsAdministratorCheckBox.IsChecked == true,
            IconGlyph: string.IsNullOrWhiteSpace(_profileIconGlyphTextBox.Text) ? "\uE756" : _profileIconGlyphTextBox.Text.Trim(),
            ColorKey: "Custom",
            StartupCommand: _profileStartupCommandTextBox.Text.Trim(),
            TabTitleTemplate: _profileTitleTemplateTextBox.Text.Trim(),
            EnvironmentOverrides: ParseEnvironmentOverrides(_profileEnvironmentTextBox.Text)));
        RefreshLists();
        ClearProfileInputs();
    }

    private void ApplyThemePreset(string? preset)
    {
        switch (preset)
        {
            case "Command Prompt Classic":
                _backgroundColorTextBox.Text = "#0C0C0C";
                _foregroundColorTextBox.Text = "#F2F2F2";
                _accentColorTextBox.Text = "#58B9FF";
                _highContrastCheckBox.IsChecked = false;
                break;
            case "PowerShell Blue":
                _backgroundColorTextBox.Text = "#012456";
                _foregroundColorTextBox.Text = "#F2F2F2";
                _accentColorTextBox.Text = "#7DB7FF";
                _highContrastCheckBox.IsChecked = false;
                break;
            case "High Contrast":
                _backgroundColorTextBox.Text = "#000000";
                _foregroundColorTextBox.Text = "#FFFFFF";
                _accentColorTextBox.Text = "#FFFF00";
                _highContrastCheckBox.IsChecked = true;
                break;
            case "Bubbles Dark":
                _backgroundColorTextBox.Text = "#0C0C0C";
                _foregroundColorTextBox.Text = "#F2F2F2";
                _accentColorTextBox.Text = "#5FC9B5";
                _highContrastCheckBox.IsChecked = false;
                break;
        }
    }

    private void RemoveSelectedProfile()
    {
        if (_customProfilesList.SelectedItem is ShellProfile profile)
        {
            _settings.CustomProfiles.Remove(profile);
            RefreshLists();
        }
    }

    private void AddSnippet()
    {
        var name = _snippetNameTextBox.Text.Trim();
        var command = _snippetCommandTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
        {
            MessageBox.Show(this, "Snippet name and command are required.", "Snippet", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.Snippets.Add(new CommandSnippet
        {
            Name = name,
            Command = command,
            Description = "User snippet",
            ShellType = "any",
            RequiresConfirmation = true
        });
        RefreshLists();
    }

    private void RemoveSelectedSnippet()
    {
        if (_snippetsList.SelectedItem is CommandSnippet snippet)
        {
            _settings.Snippets.Remove(snippet);
            RefreshLists();
        }
    }

    private void RefreshLists()
    {
        _customProfilesList.DisplayMemberPath = string.Empty;
        _customProfilesList.ItemsSource = null;
        _customProfilesList.ItemsSource = _settings.CustomProfiles;
        DialogTheme.StyleListBox(_customProfilesList, nameof(ShellProfile.DisplayName));
        _snippetsList.DisplayMemberPath = string.Empty;
        _snippetsList.ItemsSource = null;
        _snippetsList.ItemsSource = _settings.Snippets;
        DialogTheme.StyleListBox(_snippetsList, nameof(CommandSnippet.Name));
    }

    private static TextBlock Header(string text)
    {
        var header = DialogTheme.TextBlock(text, 18, FontWeights.SemiBold);
        header.Margin = new Thickness(0, 18, 0, 8);
        return header;
    }

    private static StackPanel Row(string label, UIElement control, UIElement? trailing = null)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        var labelBlock = DialogTheme.TextBlock(label);
        labelBlock.Width = 130;
        labelBlock.VerticalAlignment = VerticalAlignment.Center;
        panel.Children.Add(labelBlock);
        panel.Children.Add(control);
        if (trailing is not null)
        {
            panel.Children.Add(trailing);
        }

        return panel;
    }

    private static StackPanel ButtonRow(params Button[] buttons)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        foreach (var button in buttons)
        {
            panel.Children.Add(button);
        }

        return panel;
    }

    private static Button Button(string text, Action action)
    {
        return DialogTheme.Button(text, action);
    }

    private static void CopySettings(AppSettings source, AppSettings target)
    {
        target.Version = source.Version;
        target.DefaultProfileId = source.DefaultProfileId;
        target.RestorePreviousWorkspace = source.RestorePreviousWorkspace;
        target.ScrollbackLineLimit = source.ScrollbackLineLimit;
        target.ConfirmMultilinePaste = source.ConfirmMultilinePaste;
        target.ConfirmDestructivePaste = source.ConfirmDestructivePaste;
        target.ConfirmControlCharacterPaste = source.ConfirmControlCharacterPaste;
        target.DiagnosticLoggingEnabled = source.DiagnosticLoggingEnabled;
        target.Appearance = source.Appearance;
        target.CustomProfiles = source.CustomProfiles;
        target.Snippets = source.Snippets;
        target.LastWorkspace = source.LastWorkspace;
    }

    private static IDictionary<string, string> ParseEnvironmentOverrides(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = rawLine.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = rawLine[..separatorIndex].Trim();
            var value = rawLine[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private void ClearProfileInputs()
    {
        _profileNameTextBox.Clear();
        _profilePathTextBox.Clear();
        _profileArgsTextBox.Clear();
        _profileStartDirectoryTextBox.Clear();
        _profileStartupCommandTextBox.Clear();
        _profileTitleTemplateTextBox.Clear();
        _profileEnvironmentTextBox.Clear();
        _profileRunAsAdministratorCheckBox.IsChecked = false;
        _profileIconGlyphTextBox.Text = "\uE756";
    }
}
