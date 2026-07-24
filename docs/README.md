# Bubbles CMD Builder Guide

Version: `0.0.3`

Author: `BubblesTheDev`

This document is for developers and builders. If you only want to install Bubbles CMD, use the MSI from the GitHub Releases page and read [../RELEASE.md](../RELEASE.md).

## Project Overview

Bubbles CMD is a native Windows WPF terminal application. It keeps the Bubbles CMD app shell, profile picker, tabs, panes, settings, snippets, command browser, command palette, and safety prompts while using a Windows Terminal-backed renderer through `EasyWindowsTerminalControl` for the live terminal surface.

It launches real installed shell processes. It does not reimplement shell commands and it does not replace Windows system shells.

## End-User Release Files

The public release asset for regular users is:

```text
bubbles-cmd-0.0.3-win-x64.msi
```

The MSI installs per-user, creates shortcuts, supports uninstall through Windows Apps & Features, and installs under the user-local Programs folder.

The ZIP package is mainly for developers or portable testing:

```text
bubbles-cmd-0.0.3-win-x64.zip
```

Do not commit MSI, ZIP, publish folders, build folders, NuGet caches, or local planning notes to git.

## Requirements

- Windows 10 or newer
- .NET 8 SDK
- Git
- Optional: WiX SDK restore through the included installer project
- Optional profiles: Azure CLI, Visual Studio 2022, Git for Windows, PowerShell 7, WSL

## Run From Source

```powershell
dotnet restore .\BubblesCmd.sln
dotnet run --project .\src\BubblesCmd.App\BubblesCmd.App.csproj --configuration Release
```

## Test

```powershell
dotnet test .\BubblesCmd.sln --configuration Release
```

The test project covers settings, profile detection, command discovery, terminal parsing helpers, paste safety, plugin manifest validation, and the standard profile list.

## Package ZIP

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

Output:

```text
artifacts\packages\bubbles-cmd-0.0.3-win-x64.zip
```

## Build MSI

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-msi.ps1
```

Output:

```text
artifacts\installer\bubbles-cmd-0.0.3-win-x64.msi
```

The WiX build generates an app-file manifest under `artifacts\installer\obj`. That generated file can contain machine-local source paths and is intentionally ignored.

## Profile Behavior

Default profile order:

- `Ctrl+Shift+1`: Windows PowerShell
- `Ctrl+Shift+2`: Command Prompt
- `Ctrl+Shift+3`: Azure Cloud Shell
- `Ctrl+Shift+4`: Developer Command Prompt for VS 2022
- `Ctrl+Shift+5`: Developer PowerShell for VS 2022
- `Ctrl+Shift+6`: Git Bash

Profiles backed by missing tools can still appear with a clear message explaining what needs to be installed. Installed tools launch as real shell processes in the terminal surface.

## Repository Hygiene

Ignored local/generated content includes:

- `do.md`
- `.agents/`
- `.dotnet/`
- `.nuget-packages/`
- `artifacts/`
- `bin/`
- `obj/`
- `.env`
- `*.pfx`
- `*.msi`
- `*.msix`
- `installer/wix/AppFiles.generated.wxs`

Before pushing, run:

```powershell
git status --short
rg -n "C:\\Users\\YOUR_USERNAME|OneDrive|\.codex|AppFiles\.generated\.wxs" . --glob "!artifacts/**" --glob "!.git/**" --glob "!**/bin/**" --glob "!**/obj/**"
```

## Terminal Smoke Checklist

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\terminal-smoke.ps1
```

Use this checklist to manually verify interactive programs such as `cmd`, PowerShell, Git, SSH prompts, Python, Node, editors, and full-screen terminal programs.

## Current Scope

`0.0.3` is a working MVP foundation. It includes real shell hosting, a Windows Terminal-backed terminal surface, profile detection, tabs, panes, settings, snippets, command discovery, safety prompts, ZIP packaging, MSI packaging, and tests.

Future work includes signed releases, deeper elevated ConPTY support, richer nested pane layouts, plugin execution, and additional terminal customization.
