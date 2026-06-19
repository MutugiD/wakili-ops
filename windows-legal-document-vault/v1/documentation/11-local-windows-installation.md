# Local Windows Installation

## Purpose

This guide explains how to install, run, test, and smoke-check the Windows Legal Document Vault locally on a Windows development machine.

The app lives under:

```text
D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
```

All app code, tests, and V1 implementation documentation should stay inside this folder.

## Prerequisites

Required:

- Windows 10 or Windows 11, 64-bit.
- .NET 10 SDK.
- PowerShell.

Current development machine note:

- A user-local .NET 10 SDK is installed at `C:\Users\admin\.dotnet`.
- Use `C:\Users\admin\.dotnet\dotnet.exe` when the global `dotnet` command only exposes an older runtime.

## Build

From the V1 app root:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
& "$env:USERPROFILE\.dotnet\dotnet.exe" build WakiliDms.sln
```

Expected result:

- Build succeeds.
- Zero warnings.
- Zero errors.

## Run Tests

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
& "$env:USERPROFILE\.dotnet\dotnet.exe" run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj
```

Expected result:

- All test lines print `PASS`.
- Process exits with code `0`.

## Run the App

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
& "$env:USERPROFILE\.dotnet\dotnet.exe" run --project src\WakiliDms.App\WakiliDms.App.csproj
```

Expected result:

- WPF window opens.
- First-run setup appears if no settings exist.
- Home screen appears if setup was already completed.

## App Startup Smoke Test

Use this when a quick non-interactive check is enough:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
.\scripts\Start-AppSmoke.ps1
```

To see the app window during the smoke:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
.\scripts\Start-AppSmoke.ps1 -Visible
```

## Local App Data

Development settings and local app database are stored under:

```text
%LOCALAPPDATA%\WakiliDms\
```

Current files:

- `settings.json`
- `wakili-dms.db`

To reset local development state:

```powershell
Remove-Item "$env:LOCALAPPDATA\WakiliDms" -Recurse -Force
```

Only run this for local development. It deletes local app settings and metadata for the current Windows user.

## Manual Setup Flow

1. Launch the app.
2. Enter firm name.
3. Enter primary user.
4. Enter vault path.
5. Enter watched scan folder path.
6. Enter backup target path.
7. Confirm recovery key has been saved.
8. Click `Complete setup`.

Expected result:

- App shows Home.
- Settings persist after restart.
- Matter creation form is available.

## Manual Matter Flow

1. Complete setup.
2. Enter matter name.
3. Optionally enter client name.
4. Optionally enter court case number.
5. Click `Create matter`.

Expected result:

- Matter appears in the matter list.
- Matter remains visible after app restart.

## Troubleshooting

### No .NET SDK Found

Symptom:

```text
No .NET SDKs were found.
```

Fix:

- Install .NET 10 SDK.
- Or use the local SDK path: `C:\Users\admin\.dotnet\dotnet.exe`.

### SQLite Restore or Vulnerability Warning

The infrastructure project directly references `SQLitePCLRaw.bundle_e_sqlite3` 3.x to avoid the vulnerable 2.x transitive package path.

Verify with:

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" list package --include-transitive --vulnerable
```

Expected result:

- No vulnerable packages.
