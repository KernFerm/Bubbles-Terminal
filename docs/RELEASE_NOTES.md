# Bubbles CMD 0.0.3

Initial buildable MVP release by `BubblesTheDev`.

## Included

- Native Windows WPF application shell.
- Windows Terminal-backed terminal surface through `EasyWindowsTerminalControl`.
- App icon, window icon, package icon, and SVG branding assets.
- Real installed shell processes instead of command emulation.
- Built-in profile detection for Windows PowerShell, Command Prompt, Azure Cloud Shell when Azure CLI is installed, Visual Studio Developer Command Prompt, Visual Studio Developer PowerShell, Git Bash, PowerShell 7, WSL, and custom profiles.
- Windows Terminal-style profile picker with shortcut labels and `Ctrl+Shift+1` through `Ctrl+Shift+9` launch support.
- Tabs, duplicate tab, restart tab, rename tab, pin tab, move tab left/right, reopen recently closed tab, close confirmation, and force terminate.
- Split panes, resizable splitters, duplicate pane, swap pane, move pane to new tab, next-pane focus, pane zoom, and close pane.
- Command browser using installed CMD help, local `PATH`/`PATHEXT`, and PowerShell `Get-Command`.
- Local snippets that insert commands for review.
- Command palette for application actions.
- Clipboard paste protections for multiline text, risky command patterns, and hidden control characters.
- Search, copy, paste, clear, output export, settings import/export/reset, custom profiles, theme presets, administrator-requested custom profile setting, appearance settings, workspace restore, and optional diagnostics.
- Local-only settings and diagnostics storage.
- Repo-local NuGet package restore folder to avoid global package cache permission issues.
- Framework-dependent ZIP packaging script.
- WiX SDK MSI generation script.
- Inno Setup installer scaffolding plus local PowerShell install/uninstall scripts.
- GitHub Actions CI workflow for restore, build, tests, package, and artifact upload.
- Terminal smoke checklist script for common interactive tools.
- `do.md` ignored by git for local planning/spec notes.
- No telemetry, analytics, command uploads, clipboard uploads, or automatic crash uploads.

## Fixed

- Replaced the previous WPF text-rendering terminal surface with a real terminal renderer.
- Improved profile switching so each selected shell launches inside the same Bubbles CMD application experience.
- Added profile ordering and shortcuts to match the requested Windows Terminal-style layout.
- Added actual executable icon extraction for profile picker rows.
- Added explicit UAC restart prompt for administrator-requested custom profiles.
- Fixed Visual Studio/NuGet restore behavior by restoring packages locally to the repository.
- Explicitly set application metadata author/company values to `BubblesTheDev`.

## Known Gaps

- No signed MSI/MSIX installer yet; Inno Setup and PowerShell installer scaffolding are included.
- Fully integrated elevated ConPTY tabs are future work; administrator-requested profiles currently prompt for an elevated app restart.
- Plugin support is manifest/catalog scaffolding only; plugin execution is not enabled.
- Advanced nested pane layouts are future work.
