# Bubbles CMD Installer

This folder contains installer scaffolding for Bubbles CMD `0.0.3`.

## Option 1: Inno Setup Wizard

Install Inno Setup, build the app package, then compile:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
iscc .\installer\BubblesCmd.iss
```

The generated setup wizard includes:

- Per-user install location
- Start Menu shortcut
- Optional Desktop shortcut
- Uninstall entry
- Upgrade-over-existing-install behavior

## Option 2: MSI

Build a real MSI with the WiX SDK project:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-msi.ps1
```

The MSI is created at:

```text
artifacts\installer\bubbles-cmd-0.0.3-win-x64.msi
```

The MSI installs per-user to `%LOCALAPPDATA%\Programs\Bubbles CMD`, creates Start Menu and Desktop shortcuts, supports uninstall through Windows Apps & Features, and supports major upgrades through the stable MSI `UpgradeCode`.

## Option 3: PowerShell Shortcut Install

For development and local testing without Inno Setup:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\installer\install.ps1
```

To remove shortcuts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\installer\uninstall.ps1
```

The PowerShell installer copies the published files to `%LOCALAPPDATA%\Programs\Bubbles CMD` and creates Start Menu and optional Desktop shortcuts. It does not modify Windows system shells.
