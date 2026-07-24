param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.0.2"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts\publish\bubbles-cmd-$Version-$Runtime"
$wixDir = Join-Path $root "installer\wix"
$generatedDir = Join-Path $root "artifacts\installer\obj"
$generatedPath = Join-Path $generatedDir "AppFiles.generated.wxs"
$installerProject = Join-Path $wixDir "BubblesCmd.Installer.wixproj"

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "scripts\package.ps1") `
    -Configuration $Configuration `
    -Runtime $Runtime `
    -Version $Version

function Convert-ToWixId {
    param([string]$Value)

    $id = [System.Text.RegularExpressions.Regex]::Replace($Value, "[^A-Za-z0-9_\.]", "_")
    if ($id.Length -gt 70) {
        $hashBytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($id))
        $hash = [Convert]::ToHexString($hashBytes).Substring(0, 12)
        $id = $id.Substring(0, 56) + "_" + $hash
    }

    if ($id -notmatch "^[A-Za-z_]") {
        $id = "B_" + $id
    }

    return $id
}

function Get-RelativePath {
    param(
        [string]$BasePath,
        [string]$Path
    )

    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath)
    if (-not $baseFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFullPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $pathFullPath = [System.IO.Path]::GetFullPath($Path)
    $baseUri = [System.Uri]::new($baseFullPath)
    $pathUri = [System.Uri]::new($pathFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace("/", "\")
}

$files = Get-ChildItem -Path $publishDir -File -Recurse | Sort-Object FullName
$directories = $files |
    ForEach-Object { Split-Path -Parent (Get-RelativePath $publishDir $_.FullName) } |
    Where-Object { $_ -and $_ -ne "." } |
    Sort-Object -Unique

$directoryIds = @{}
$directoryXml = New-Object System.Text.StringBuilder
$componentXml = New-Object System.Text.StringBuilder

foreach ($directory in $directories) {
    $segments = $directory.Split("/", [StringSplitOptions]::RemoveEmptyEntries)
    $parentId = "INSTALLFOLDER"
    $pathSoFar = ""
    foreach ($segment in $segments) {
        $pathSoFar = if ($pathSoFar) { "$pathSoFar/$segment" } else { $segment }
        if ($directoryIds.ContainsKey($pathSoFar)) {
            $parentId = $directoryIds[$pathSoFar]
            continue
        }

        $dirId = "DIR_" + (Convert-ToWixId $pathSoFar)
        $directoryIds[$pathSoFar] = $dirId
        [void]$directoryXml.AppendLine("    <DirectoryRef Id=`"$parentId`">")
        [void]$directoryXml.AppendLine("      <Directory Id=`"$dirId`" Name=`"$([System.Security.SecurityElement]::Escape($segment))`" />")
        [void]$directoryXml.AppendLine("    </DirectoryRef>")
        $parentId = $dirId
    }
}

[void]$componentXml.AppendLine("    <ComponentGroup Id=`"AppComponents`">")
foreach ($file in $files) {
    $relative = Get-RelativePath $publishDir $file.FullName
    $directory = Split-Path -Parent $relative
    $directoryId = if ($directory -and $directory -ne ".") { $directoryIds[$directory.Replace("\", "/")] } else { "INSTALLFOLDER" }
    $fileId = "FILE_" + (Convert-ToWixId $relative)
    $componentId = "CMP_" + (Convert-ToWixId $relative)
    $source = [System.Security.SecurityElement]::Escape($file.FullName)

    [void]$componentXml.AppendLine("      <Component Id=`"$componentId`" Directory=`"$directoryId`" Guid=`"*`">")
    [void]$componentXml.AppendLine("        <File Id=`"$fileId`" Source=`"$source`" KeyPath=`"yes`" />")
    [void]$componentXml.AppendLine("      </Component>")
}
[void]$componentXml.AppendLine("    </ComponentGroup>")

$wxs = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
$directoryXml  </Fragment>
  <Fragment>
$componentXml  </Fragment>
</Wix>
"@

New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null
Set-Content -Path $generatedPath -Value $wxs -Encoding UTF8

dotnet build $installerProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "MSI build failed with exit code $LASTEXITCODE"
}

$msiPath = Join-Path $root "artifacts\installer\bubbles-cmd-$Version-$Runtime.msi"
if (-not (Test-Path $msiPath)) {
    throw "MSI was not created at $msiPath"
}

Write-Host "Created $msiPath"
