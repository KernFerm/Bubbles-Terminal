# Privacy Policy

Bubbles CMD is designed as a local Windows terminal application.

## Data Collection

Bubbles CMD does not collect, upload, sell, or share:

- Terminal commands
- Terminal output
- Clipboard contents
- Shell history
- File paths
- Environment variables
- Installed command lists
- Crash reports
- Usage analytics

## Local Settings

Settings are stored locally under:

```text
%LOCALAPPDATA%\BubblesCmd\settings.json
```

Settings may include user-created custom profiles, snippets, appearance options, and workspace restore metadata.

## Diagnostics

Optional diagnostics are local only and are intended for troubleshooting application behavior. Diagnostics do not intentionally include command text, terminal output, clipboard contents, or passwords.

Diagnostic logs are stored under:

```text
%LOCALAPPDATA%\BubblesCmd\logs
```

## Network Access

Core terminal functionality does not require an internet connection. Any network access comes from commands or shells that the user explicitly runs.

## Clipboard

Clipboard contents are only read when the user performs a paste action. Clipboard contents are not logged or uploaded.

## Updates

Version `0.0.2` does not include automatic update checks.
