param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\WakiliDms.App\WakiliDms.App.csproj"
$artifactsRoot = Join-Path $root "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish\$Runtime"
$packageRoot = Join-Path $artifactsRoot "package"
$packageName = "windows-legal-document-vault-v1-$Runtime"
$packageDirectory = Join-Path $packageRoot $packageName
$zipPath = Join-Path $packageRoot "$packageName.zip"
$executableName = "WindowsLegalDocumentVault.exe"
$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"

if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

if (-not (Test-Path $project)) {
    throw "App project not found at $project"
}

Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $packageDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishRoot, $packageDirectory | Out-Null

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
& $dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained $selfContained `
    --output $publishRoot `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -Path (Join-Path $publishRoot "*") -Destination $packageDirectory -Recurse

$installScript = @'
param(
    [string]$InstallRoot = "$env:LOCALAPPDATA\Programs\WindowsLegalDocumentVault",
    [switch]$CreateDesktopShortcut,
    [switch]$SkipShortcuts
)

$ErrorActionPreference = "Stop"

$sourceRoot = $PSScriptRoot
$exePath = Join-Path $sourceRoot "WindowsLegalDocumentVault.exe"
if (-not (Test-Path $exePath)) {
    throw "Packaged executable not found at $exePath"
}

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
Copy-Item -Path (Join-Path $sourceRoot "*") -Destination $InstallRoot -Recurse -Force

$installedExe = Join-Path $InstallRoot "WindowsLegalDocumentVault.exe"
if (-not $SkipShortcuts) {
    $startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    $shortcutPath = Join-Path $startMenu "Windows Legal Document Vault.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $installedExe
    $shortcut.WorkingDirectory = $InstallRoot
    $shortcut.Description = "Windows Legal Document Vault"
    $shortcut.Save()

    if ($CreateDesktopShortcut) {
        $desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Windows Legal Document Vault.lnk"
        $desktop = $shell.CreateShortcut($desktopShortcut)
        $desktop.TargetPath = $installedExe
        $desktop.WorkingDirectory = $InstallRoot
        $desktop.Description = "Windows Legal Document Vault"
        $desktop.Save()
    }
}

"PASS Installed Windows Legal Document Vault to $InstallRoot"
'@

Set-Content -Path (Join-Path $packageDirectory "install-local.ps1") -Value $installScript -Encoding UTF8

$uninstallScript = @'
param(
    [string]$InstallRoot = "$env:LOCALAPPDATA\Programs\WindowsLegalDocumentVault",
    [string]$UserDataRoot = "$env:LOCALAPPDATA\WakiliDms",
    [switch]$DeleteUserVaultData
)

$ErrorActionPreference = "Stop"

$startMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Windows Legal Document Vault.lnk"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Windows Legal Document Vault.lnk"
Remove-Item -LiteralPath $startMenuShortcut -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $desktopShortcut -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $InstallRoot -Recurse -Force -ErrorAction SilentlyContinue

if ($DeleteUserVaultData) {
    Remove-Item -LiteralPath $UserDataRoot -Recurse -Force -ErrorAction SilentlyContinue
}

"PASS Uninstalled Windows Legal Document Vault package. User vault data preserved unless DeleteUserVaultData was supplied."
'@

Set-Content -Path (Join-Path $packageDirectory "uninstall-local.ps1") -Value $uninstallScript -Encoding UTF8

$runCommand = @'
@echo off
set APP_DIR=%~dp0
start "" "%APP_DIR%WindowsLegalDocumentVault.exe"
'@
Set-Content -Path (Join-Path $packageDirectory "run-windows-legal-document-vault.cmd") -Value $runCommand -Encoding ASCII

$manifest = [ordered]@{
    product = "Windows Legal Document Vault"
    version = "v1"
    runtime = $Runtime
    frameworkDependent = [bool]$FrameworkDependent
    builtAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    executable = $executableName
    installScript = "install-local.ps1"
    uninstallScript = "uninstall-local.ps1"
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $packageDirectory "package-manifest.json") -Encoding UTF8

$readme = @"
# Windows Legal Document Vault V1 Package

Run ``install-local.ps1`` from PowerShell to install for the current Windows user.

````powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-local.ps1
````

The installer copies the app to ``%LOCALAPPDATA%\Programs\WindowsLegalDocumentVault`` and creates a Start Menu shortcut. It does not delete or move user vault data.

The packaged executable is ``WindowsLegalDocumentVault.exe``.
"@
Set-Content -Path (Join-Path $packageDirectory "README.md") -Value $readme -Encoding UTF8

Compress-Archive -Path (Join-Path $packageDirectory "*") -DestinationPath $zipPath -Force

[pscustomobject]@{
    PackageDirectory = $packageDirectory
    ZipPath = $zipPath
    Executable = $executableName
    FrameworkDependent = [bool]$FrameworkDependent
} | ConvertTo-Json -Depth 3 -Compress
