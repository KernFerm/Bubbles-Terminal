param(
    [switch]$DesktopShortcut
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $repoRoot "artifacts\publish\bubbles-cmd-0.0.2-win-x64"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\Bubbles CMD"
$exePath = Join-Path $installDir "BubblesCmd.App.exe"
$iconPath = Join-Path $installDir "Assets\bubbles.ico"

if (-not (Test-Path $publishDir)) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts\package.ps1")
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $installDir -Recurse -Force

$shell = New-Object -ComObject WScript.Shell
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Bubbles CMD"
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null
$shortcut = $shell.CreateShortcut((Join-Path $startMenuDir "Bubbles CMD.lnk"))
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = $iconPath
$shortcut.Save()

if ($DesktopShortcut) {
    $desktopShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "Bubbles CMD.lnk"))
    $desktopShortcut.TargetPath = $exePath
    $desktopShortcut.WorkingDirectory = $installDir
    $desktopShortcut.IconLocation = $iconPath
    $desktopShortcut.Save()
}

Write-Host "Installed Bubbles CMD to $installDir"
