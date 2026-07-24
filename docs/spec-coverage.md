# Bubbles CMD Spec Coverage

This file tracks implemented areas from `do.md` and keeps remaining work visible.

## Implemented

- Real Windows shell process hosting through a ConPTY interop layer.
- Built-in profile detection for `cmd.exe`, Windows PowerShell, PowerShell 7, Git Bash, WSL, and Visual Studio developer shells when installed.
- Custom shell profiles stored in local user settings.
- Tabs with new, duplicate, restart, close, rename, pin, move left/right, recently closed reopen, exit-code display, and workspace restore.
- Basic split panes with vertical split, horizontal split, duplicate pane, active-pane focus, next-pane focus, pane zoom/restore, close-pane, per-pane ConPTY sessions, and active-pane routing for copy/paste/search/snippets/commands/terminate.
- Force-terminate action for stuck shell sessions with explicit confirmation.
- Clipboard copy/paste with multiline, risky-command, and hidden-control-character review warnings.
- Bracketed paste mode tracking from terminal control sequences with bracket-wrapped paste input when enabled by the active shell or application.
- BEL handling with bell control characters stripped from output and surfaced through the app status area.
- Scrollback search, clear, clear-screen escape handling, and output export.
- Console-style screen buffer for prompt-line echo, carriage return, newline, backspace, cursor-left/right, erase-line, insert-blank, and delete-character behavior.
- High-contrast console-readable terminal defaults with migration for older saved settings.
- Lightweight ANSI/SGR rendering for reset, bold, dim, italic, underline, reverse video, 16-color, 256-color, true-color foreground/background, and OSC window-title tab updates.
- File and folder drag-and-drop path insertion with Windows quoting.
- Local snippets that insert command text for user review.
- Command browser backed by local dynamic discovery from `PATH`/`PATHEXT`, CMD internal commands from `cmd.exe /D /C help`, and PowerShell `Get-Command`.
- Command palette for application actions.
- Settings window for workspace restore, paste warnings, scrollback, custom profiles, and snippets.
- Settings import, export, backup, reset, and validation.
- Terminal appearance settings for font family, font size, line height, foreground/background/accent colors, high contrast, and reduced motion preference.
- Optional local diagnostic logging with rotation and no command text, clipboard text, or terminal output.
- About/privacy window with local data locations.
- Per-monitor DPI aware app manifest and as-invoker security manifest.
- Framework-dependent ZIP packaging script.
- Plugin manifest/catalog scaffold with validation, without plugin execution.
- Local-only JSON settings under `%LOCALAPPDATA%\BubblesCmd`.
- Offline automated tests for sanitizing, profile detection, path quoting, settings validation, ANSI parsing, and PATH command discovery.

## Still To Build

- Full cursor-addressed VT renderer with alternate screen buffer, cursor state, mouse tracking, hyperlinks, reflow, and TUI support.
- Advanced pane management: nested layouts, manual pane resizing, pane swapping, pane-to-tab conversion, and drag-and-drop tab reordering.
- Accurate current working directory tracking from shell integration sequences or process inspection.
- Elevated ConPTY sessions with UAC prompts and clear administrator separation.
- Persistent optional history index with sensitive-input exclusions.
- Plugin isolation and permission declarations.
- Accessibility pass with explicit UI Automation patterns beyond default WPF behavior, screen-reader announcements, and deeper keyboard navigation polish.
- Installer, repair/uninstall flow, shortcuts, optional context-menu integration, and optional Windows Terminal profile registration.
