param(
    [int]$Seconds = 8,
    [switch]$Visible,
    [switch]$LeaveRunning
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\WakiliDms.App\WakiliDms.App.csproj"
$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"

if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

if (-not (Test-Path $project)) {
    throw "App project not found at $project"
}

$windowStyle = if ($Visible) { "Normal" } else { "Hidden" }
$process = Start-Process `
    -FilePath $dotnet `
    -ArgumentList @("run", "--configuration", "Release", "--project", $project, "--no-build") `
    -WindowStyle $windowStyle `
    -PassThru

Start-Sleep -Seconds $Seconds

if ($process.HasExited) {
    throw "App exited early with code $($process.ExitCode)."
}

if (-not $LeaveRunning) {
    Stop-Process -Id $process.Id -Force
}

"PASS Windows Legal Document Vault app startup smoke. ProcessId=$($process.Id)"
