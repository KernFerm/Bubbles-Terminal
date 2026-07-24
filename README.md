# Bubbles CMD

Version: `0.0.3`

Author: `BubblesTheDev`

Bubbles CMD is a modern Windows terminal app that opens real installed shells inside one clean WPF desktop window. It is not a command emulator and it does not replace `cmd.exe`, PowerShell, Windows Terminal, Git Bash, WSL, or Visual Studio developer shells.

![Bubbles CMD preview](docs/screenshots/bubbles-cmd-preview.svg)

## Download For Windows

Most users should download the MSI installer from the GitHub Releases page:

```text
bubbles-cmd-0.0.3-win-x64.msi
```

Install it by double-clicking the MSI, then open Bubbles CMD from the Start Menu or Desktop shortcut.

Release page:

```text
https://github.com/KernFerm/Bubbles-Terminal/releases
```

End-user install notes are in [RELEASE.md](RELEASE.md).

## What It Does

- Opens real Windows shells inside the Bubbles CMD app window.
- Supports Windows PowerShell, Command Prompt, Azure Cloud Shell, Visual Studio developer shells, Git Bash, PowerShell 7, WSL, and custom profiles.
- Uses a Windows Terminal-backed terminal surface through `EasyWindowsTerminalControl`.
- Includes tabs, panes, split views, pane resizing, pane swapping, pane-to-tab movement, search, snippets, commands, settings, and a command palette.
- Uses local settings and optional local diagnostics only.
- Does not include telemetry, analytics, command uploads, clipboard uploads, or terminal-output uploads.

## First Launch

Use the profile menu to open a shell:

- `Ctrl+Shift+1`: Windows PowerShell
- `Ctrl+Shift+2`: Command Prompt
- `Ctrl+Shift+3`: Azure Cloud Shell
- `Ctrl+Shift+4`: Developer Command Prompt for VS 2022
- `Ctrl+Shift+5`: Developer PowerShell for VS 2022
- `Ctrl+Shift+6`: Git Bash

Some profiles require other tools to be installed. If Azure CLI, Visual Studio, Git, PowerShell 7, or WSL is missing, Bubbles CMD will show a helpful message instead of silently failing.

## For Builders

Requirements:

- Windows 10 or newer
- .NET 8 SDK
- Git

Run from source:

```powershell
dotnet restore .\BubblesCmd.sln
dotnet run --project .\src\BubblesCmd.App\BubblesCmd.App.csproj --configuration Release
```

Run tests:

```powershell
dotnet test .\BubblesCmd.sln --configuration Release
```

Build the release ZIP:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Build the MSI:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-msi.ps1
```

Generated release files are local artifacts and should not be committed:

```text
artifacts\packages\bubbles-cmd-0.0.3-win-x64.zip
artifacts\installer\bubbles-cmd-0.0.3-win-x64.msi
```

## Documentation

- [Detailed builder guide](docs/README.md)
- [Release notes](docs/RELEASE_NOTES.md)
- [Changelog](CHANGELOG.md)
- [Privacy policy](PRIVACY.md)
- [Security policy](SECURITY.md)
- [Manual test plan](docs/manual-test-plan.md)
- [Installer notes](installer/README.md)

## License

MIT. See [LICENSE](LICENSE).
