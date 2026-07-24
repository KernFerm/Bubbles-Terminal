param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.0.3"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $root ".dotnet"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$publishDir = Join-Path $root "artifacts\publish\bubbles-cmd-$Version-$Runtime"
$packageDir = Join-Path $root "artifacts\packages"
$packagePath = Join-Path $packageDir "bubbles-cmd-$Version-$Runtime.zip"

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

dotnet publish (Join-Path $root "src\BubblesCmd.App\BubblesCmd.App.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained false `
    --output $publishDir

$releaseDocsDir = Join-Path $publishDir "docs"
New-Item -ItemType Directory -Force -Path $releaseDocsDir | Out-Null
foreach ($file in @("README.md", "CHANGELOG.md", "LICENSE", "PRIVACY.md", "SECURITY.md")) {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination $publishDir -Force
}

Copy-Item -LiteralPath (Join-Path $root "docs\RELEASE_NOTES.md") -Destination $releaseDocsDir -Force
Copy-Item -LiteralPath (Join-Path $root "docs\manual-test-plan.md") -Destination $releaseDocsDir -Force
Copy-Item -LiteralPath (Join-Path $root "installer\README.md") -Destination (Join-Path $releaseDocsDir "INSTALLER.md") -Force

if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $packagePath
Write-Host "Created $packagePath"
