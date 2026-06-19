param(
    [int]$Seconds = 8,
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
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
$exePath = Join-Path $result.PackageDirectory "WakiliDms.App.exe"
if (-not (Test-Path $exePath)) {
    throw "Packaged executable not found at $exePath"
}

$process = Start-Process `
    -FilePath $exePath `
    -WorkingDirectory $result.PackageDirectory `
    -WindowStyle Hidden `
    -PassThru

Start-Sleep -Seconds $Seconds

if ($process.HasExited) {
    throw "Packaged app exited early with code $($process.ExitCode)."
}

Stop-Process -Id $process.Id -Force

"PASS Windows package smoke. Package=$($result.PackageDirectory)"
