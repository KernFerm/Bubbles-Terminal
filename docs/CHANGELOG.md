# Changelog

All notable changes to Bubbles CMD are documented here.

## 0.0.4

### Added

- Added richer custom profile options including startup commands, tab title templates, icon glyph overrides, and per-profile environment overrides.
- Added workspace persistence for saved startup-command metadata.
- Added xUnit-based automated tests for settings recovery, command-search ranking, and core terminal/service behavior.
- Added test-result upload and explicit release-artifact validation to the GitHub Actions workflow.

### Changed

- Updated terminal appearance application so foreground/background, selection, accent, cursor behavior, and line-height padding respond to saved settings.
- Updated command discovery with in-memory caching, fuzzy search ranking, and richer command descriptions in the command browser.
- Updated the command browser to show more command context and use the improved search behavior.
- Simplified the top toolbar so primary actions stay visible while secondary actions move behind the command palette and shortcuts.
- Refactored parts of the main-window workflow into focused helper services for workspace capture and command palette launching.
- Hardened settings persistence with atomic saves, repaired-settings detection, and best-effort corruption backups.
- Updated release, installer, script, workflow, and documentation version references to `0.0.4`.

### Fixed

- Wired live terminal output parsing into the app so bracketed paste mode, BEL notifications, and OSC title updates can flow into the UI.
- Improved terminal search, selection export, and output save behavior so they better match what the UI advertises.
- Replaced the custom executable-style test harness with a standard `dotnet test` workflow.

## 0.0.3

### Changed

- Updated application, assembly, installer, README, privacy, and security metadata to version `0.0.3`.

### Fixed

- Improved terminal shutdown so closing tabs, panes, or the app kills the owned shell process tree before disconnecting the terminal surface.

## 0.0.2

### Changed

- Updated application, assembly, installer, README, privacy, and security metadata to version `0.0.2`.
- Simplified built-in profile names so the picker displays terminal names clearly without repeated `Bubbles CMD -` prefixes.
- Listed Azure Cloud Shell and Visual Studio 2022 developer profiles even when their required tools are not installed, with clear in-terminal guidance.

### Fixed

- Fixed Windows Terminal-backed sessions so shells start in the configured profile directory instead of the installed app folder.
- Fixed `.gitignore` so the WPF app project under `src/BubblesCmd.App` is not accidentally ignored.
- Removed generated WiX app-file manifests from source control because they contain machine-local build paths.
- Added regression coverage for the standard profile list.

## 0.0.1

### Added

- Created the Bubbles CMD native Windows WPF application.
- Added application metadata for `BubblesTheDev`.
- Added real shell profile support for Windows PowerShell, Command Prompt, Azure Cloud Shell, Visual Studio developer shells, Git Bash, PowerShell 7, WSL, and custom shells.
- Added a Windows Terminal-style profile picker with icon, name, shortcut text, and profile launch behavior.
- Added `Ctrl+Shift+1` through `Ctrl+Shift+9` profile shortcuts.
- Added tabs, duplicated tabs, restart, rename, pinning, tab movement, recently closed tabs, and force terminate.
- Added split panes, resizable splitters, duplicate pane, pane swapping, move-pane-to-new-tab, next-pane focus, pane zoom, and pane closing.
- Added command browser, command palette, snippets, search, output export, copy, paste, clear, and drag-and-drop path quoting.
- Added settings UI, JSON settings persistence, custom profiles, administrator-requested custom profiles, snippets, appearance controls, theme presets, import, export, reset, and workspace restore.
- Added paste protection for multiline content, risky command patterns, and hidden control characters.
- Added optional local diagnostics without command text, terminal output, or clipboard content.
- Added app icon, window icon, package icon, SVG branding, and actual executable icon extraction for profile picker rows.
- Added test coverage for settings, profile detection, command discovery, terminal parsing, paste safety, plugin manifests, and profile ordering.
- Added framework-dependent ZIP packaging under `artifacts\packages`.
- Added WiX SDK MSI generation under `artifacts\installer`.
- Added installer scaffolding under `installer\`.
- Added GitHub Actions CI for restore, build, tests, and package artifact upload.
- Added terminal smoke checklist script for common interactive console tools.
- Added repo-local NuGet package restore through `.nuget-packages`.
- Added root-level README, LICENSE, CHANGELOG, PRIVACY, and SECURITY files.
- Added `do.md` to `.gitignore` so local planning notes are not uploaded.

### Changed

- Replaced the original WPF text terminal surface with a Windows Terminal-backed terminal renderer via `EasyWindowsTerminalControl`.
- Updated shell launching so each selected profile opens in the Bubbles CMD application using the shared terminal experience.
- Reordered common profiles so Windows PowerShell, Command Prompt, Azure Cloud Shell, Visual Studio developer shells, and Git Bash match the requested shortcut layout.
- Updated documentation and release notes for the current 0.0.1 scope.
- Updated release ZIP packaging to include public documentation files.

### Fixed

- Fixed NuGet restore failures caused by blocked access to the global NuGet package cache.
- Fixed application metadata so author/company values are explicitly set to `BubblesTheDev`.
- Fixed profile selection so choosing a profile from the dropdown launches that shell directly.
- Fixed shortcut profile launch behavior so a shortcut opens one matching profile tab.
- Added explicit UAC restart prompt for administrator-requested profiles instead of silently launching without elevation.

### Security And Privacy

- No telemetry.
- No analytics.
- No command uploads.
- No clipboard uploads.
- No automatic crash uploads.
- No replacement of Windows system shells.
