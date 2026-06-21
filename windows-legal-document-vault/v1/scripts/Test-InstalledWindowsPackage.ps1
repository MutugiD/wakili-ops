param(
    [int]$Seconds = 8,
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$buildScript = Join-Path $PSScriptRoot "Build-WindowsPackage.ps1"
$resultJson = if ($FrameworkDependent) {
    powershell -NoProfile -ExecutionPolicy Bypass -File $buildScript -FrameworkDependent
} else {
    powershell -NoProfile -ExecutionPolicy Bypass -File $buildScript
}

$resultLine = @($resultJson) | Where-Object { $_ -match "^\s*\{" } | Select-Object -Last 1
if (-not $resultLine) {
    throw "Package build did not return a JSON result."
}

$result = $resultLine | ConvertFrom-Json
$packageExe = Join-Path $result.PackageDirectory "WindowsLegalDocumentVault.exe"
if (-not (Test-Path $packageExe)) {
    throw "Package executable not found at $packageExe"
}

$tempRoot = Join-Path $env:TEMP ("WindowsLegalDocumentVault.InstallSmoke." + [guid]::NewGuid().ToString("N"))
$installRoot = Join-Path $tempRoot "InstalledApp"
$userDataRoot = Join-Path $tempRoot "UserVaultData"
$markerPath = Join-Path $userDataRoot "settings-marker.txt"

try {
    New-Item -ItemType Directory -Path $userDataRoot | Out-Null
    Set-Content -Path $markerPath -Value "preserve local user vault data" -Encoding UTF8

    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $result.PackageDirectory "install-local.ps1") -InstallRoot $installRoot -SkipShortcuts
    if ($LASTEXITCODE -ne 0) {
        throw "Local install failed with exit code $LASTEXITCODE."
    }

    $installedExe = Join-Path $installRoot "WindowsLegalDocumentVault.exe"
    if (-not (Test-Path $installedExe)) {
        throw "Installed executable not found at $installedExe"
    }

    $process = Start-Process `
        -FilePath $installedExe `
        -WorkingDirectory $installRoot `
        -WindowStyle Hidden `
        -PassThru

    Start-Sleep -Seconds $Seconds

    if ($process.HasExited) {
        throw "Installed app exited early with code $($process.ExitCode)."
    }

    Stop-Process -Id $process.Id -Force

    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $installRoot "uninstall-local.ps1") -InstallRoot $installRoot -UserDataRoot $userDataRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Local uninstall failed with exit code $LASTEXITCODE."
    }

    if (Test-Path $installRoot) {
        throw "Install root should be removed by uninstall: $installRoot"
    }

    if (-not (Test-Path $markerPath)) {
        throw "User vault data marker should be preserved by uninstall."
    }

    "PASS Installed Windows package smoke. Executable=WindowsLegalDocumentVault.exe"
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
