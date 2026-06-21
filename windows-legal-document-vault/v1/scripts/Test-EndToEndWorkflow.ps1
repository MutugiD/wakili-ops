param(
    [switch]$Visible,
    [switch]$IncludePackageSmoke,
    [switch]$IncludeInteractiveInstalledApp
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"

if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Push-Location $root
try {
    & $dotnet build WakiliDms.sln --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }

    & $dotnet run `
        --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj `
        --configuration Release `
        -- `
        --filter "end-to-end"
    if ($LASTEXITCODE -ne 0) {
        throw "End-to-end workflow test failed with exit code $LASTEXITCODE."
    }

    $smokeArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ".\scripts\Start-AppSmoke.ps1")
    if ($Visible) {
        $smokeArgs += "-Visible"
    }

    powershell @smokeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "App startup smoke failed with exit code $LASTEXITCODE."
    }

    if ($IncludePackageSmoke) {
        powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-WindowsPackage.ps1 -FrameworkDependent
        if ($LASTEXITCODE -ne 0) {
            throw "Package smoke failed with exit code $LASTEXITCODE."
        }

        powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledWindowsPackage.ps1 -FrameworkDependent
        if ($LASTEXITCODE -ne 0) {
            throw "Installed package smoke failed with exit code $LASTEXITCODE."
        }
    }

    if ($IncludeInteractiveInstalledApp) {
        powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage
        if ($LASTEXITCODE -ne 0) {
            throw "Installed app interactive workflow failed with exit code $LASTEXITCODE."
        }
    }

    "PASS Windows Legal Document Vault end-to-end workflow."
}
finally {
    Pop-Location
}
