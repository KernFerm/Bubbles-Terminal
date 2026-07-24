# Bubbles CMD 0.0.4 Release

Thank you for trying Bubbles CMD. This release is a Windows installer package for end users.

## Download

Download the MSI installer from the GitHub release assets:

```text
bubbles-cmd-0.0.4-win-x64.msi
```

Do not download source code unless you are a developer who wants to build the app manually.

## Install

1. Download `bubbles-cmd-0.0.4-win-x64.msi`.
2. Double-click the MSI file.
3. Follow the installer prompts.
4. Open Bubbles CMD from the Start Menu or Desktop shortcut.

Bubbles CMD installs for your Windows user account only. It does not replace `cmd.exe`, PowerShell, Windows Terminal, or any system shell.

## First Launch

When Bubbles CMD opens, use the profile menu to start a shell:

- Windows PowerShell
- Command Prompt
- Azure Cloud Shell, if Azure CLI is installed
- Developer Command Prompt for VS 2022, if Visual Studio is installed
- Developer PowerShell for VS 2022, if Visual Studio is installed
- Git Bash, if Git for Windows is installed
- WSL, if Windows Subsystem for Linux is installed

If a tool is not installed, Bubbles CMD will show a message instead of silently failing.

## Windows SmartScreen

Windows may show a warning because this is an early unsigned release.

If you trust this release:

1. Click `More info`.
2. Click `Run anyway`.

Future releases should use code signing to reduce this warning.

## Uninstall

Use Windows Settings:

1. Open `Settings`.
2. Go to `Apps`.
3. Find `Bubbles CMD`.
4. Select `Uninstall`.

You can also uninstall from Control Panel if your Windows version shows MSI apps there.

## Troubleshooting

- If a shell opens in the wrong folder, update to version `0.0.4` or newer.
- If Bubbles CMD feels slow after closing, update to version `0.0.4` or newer so terminal child processes are cleaned up more aggressively.
- If Git Bash is missing, install Git for Windows and restart Bubbles CMD.
- If Azure Cloud Shell is missing, install Azure CLI and restart Bubbles CMD.
- If Visual Studio developer profiles are missing, install Visual Studio 2022 with C++ or developer command tools.
- If WSL opens but no Linux distro starts, install a WSL distribution from Microsoft Store or run `wsl --install`.

## Privacy

Bubbles CMD does not include telemetry, analytics, command uploads, clipboard uploads, or terminal-output uploads.

Settings and optional diagnostics stay on your computer.

## Release Files

End users should download:

```text
bubbles-cmd-0.0.4-win-x64.msi
```

Developers may also download:

```text
bubbles-cmd-0.0.4-win-x64.zip
```
