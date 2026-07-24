# Bubbles CMD

Version: `0.0.2`

Author: `BubblesTheDev`

Bubbles CMD is a native Windows terminal application for launching real Windows shells inside a modern WPF interface. It is not a CMD emulator and it does not reimplement shell commands. Command Prompt, Windows PowerShell, PowerShell 7, Git Bash, Visual Studio developer shells, WSL, and custom profiles run as real installed shell processes.

The app keeps the Bubbles CMD window, tabs, panes, settings, snippets, command browser, and safety tools, while the live terminal surface is backed by a Windows Terminal-based renderer through `EasyWindowsTerminalControl`.

## Highlights

- Real shell hosting for installed Windows command-line environments.
- Windows Terminal-backed terminal surface for normal keyboard behavior, cursor handling, colors, and interactive console workflows.
- WPF desktop shell with tabs, pane splitting, resizable splitters, pane swapping, pane-to-tab movement, pane duplication, pane zoom, tab rename, pinning, reopen closed tab, restart, terminate, copy, paste, clear, search, snippets, commands, palette, settings, and about dialogs.
- Profile picker with Windows Terminal-style rows and shortcuts such as `Ctrl+Shift+1`, `Ctrl+Shift+2`, and onward.
- Profile picker rows show actual executable icons when Windows can provide them.
- Built-in profile detection for Windows PowerShell, Command Prompt, Azure Cloud Shell when Azure CLI is installed, Visual Studio Developer Command Prompt, Visual Studio Developer PowerShell, Git Bash, PowerShell 7, WSL, and custom profiles.
- Dynamic command discovery from installed CMD built-ins, `PATH`/`PATHEXT`, and PowerShell `Get-Command`.
- Local settings under `%LOCALAPPDATA%\BubblesCmd\settings.json`.
- Theme presets for Bubbles Dark, Command Prompt Classic, PowerShell Blue, and High Contrast.
- Optional local diagnostics under `%LOCALAPPDATA%\BubblesCmd\logs`.
- No telemetry, analytics, command uploads, clipboard uploads, or automatic crash uploads.

## Profile Shortcuts

Detected profiles are ordered to match common Windows Terminal-style usage:

- `Ctrl+Shift+1`: Windows PowerShell
- `Ctrl+Shift+2`: Command Prompt
- `Ctrl+Shift+3`: Azure Cloud Shell, when Azure CLI is installed
- `Ctrl+Shift+4`: Developer Command Prompt for VS 2022, when detected
- `Ctrl+Shift+5`: Developer PowerShell for VS 2022, when detected
- `Ctrl+Shift+6`: Git Bash, when installed

Availability depends on what is installed on the current computer. Bubbles CMD does not show a profile as available unless its backing executable is detected.

## Run

```powershell
dotnet run --project .\src\BubblesCmd.App\BubblesCmd.App.csproj
```

## Test

```powershell
dotnet run --project .\tests\BubblesCmd.Tests\BubblesCmd.Tests.csproj
```

## Package

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

The package script creates:

```text
artifacts\packages\bubbles-cmd-0.0.2-win-x64.zip
```

The app does not replace `cmd.exe`, `powershell.exe`, or `pwsh.exe`, and it does not modify protected Windows system files.

## Installer

Installer scaffolding is available in `installer\`:

- `installer\BubblesCmd.iss` for an Inno Setup wizard.
- `scripts\build-msi.ps1` for a WiX SDK MSI.
- `installer\install.ps1` for a local shortcut-based install.
- `installer\uninstall.ps1` for removing local shortcut-based installs.

## Terminal Smoke Checklist

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\terminal-smoke.ps1
```

## Repository Notes

- `do.md` is ignored by git and is intended to remain local.
- NuGet packages restore into a repo-local `.nuget-packages` folder to avoid global NuGet cache permission issues.
- Build outputs and packaged artifacts are ignored by git.

## Current Scope

`0.0.2` is a working MVP foundation. It includes real shell hosting, a Windows Terminal-backed terminal surface, profile detection, tabs, panes, settings, snippets, command discovery, safety prompts, packaging, and tests.

Future work includes signed installer builds, fully integrated elevated ConPTY sessions, deeper plugin execution support, richer nested pane layouts, and additional terminal customization.
