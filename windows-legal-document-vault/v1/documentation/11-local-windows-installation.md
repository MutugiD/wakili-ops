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

## Build the Windows EXE

The app project publishes as:

```text
WindowsLegalDocumentVault.exe
```

From the V1 app root:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
& "$env:USERPROFILE\.dotnet\dotnet.exe" publish src\WakiliDms.App\WakiliDms.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\publish\win-x64
```

Expected result:

- `artifacts\publish\win-x64\WindowsLegalDocumentVault.exe`
- Supporting runtime files for the self-contained Windows build.

Run directly:

```powershell
.\artifacts\publish\win-x64\WindowsLegalDocumentVault.exe
```

## Build a Local Windows Package

Use this when preparing a plug-and-play folder or zip for local Windows testing:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-WindowsPackage.ps1
```

Expected outputs:

- `artifacts\package\windows-legal-document-vault-v1-win-x64\`
- `artifacts\package\windows-legal-document-vault-v1-win-x64.zip`
- `WindowsLegalDocumentVault.exe`
- `install-local.ps1`
- `uninstall-local.ps1`
- `run-windows-legal-document-vault.cmd`
- `package-manifest.json`

Default packaging is self-contained for `win-x64`, so the package is suitable for Windows machines that do not already have the target .NET runtime installed.

For CI or faster developer checks, build a framework-dependent package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-WindowsPackage.ps1 -FrameworkDependent
```

## Install the Local Package

From the generated package folder:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1\artifacts\package\windows-legal-document-vault-v1-win-x64
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-local.ps1
```

Expected result:

- App files are copied to `%LOCALAPPDATA%\Programs\WindowsLegalDocumentVault`.
- Installed executable exists at `%LOCALAPPDATA%\Programs\WindowsLegalDocumentVault\WindowsLegalDocumentVault.exe`.
- A Start Menu shortcut is created for the current Windows user.
- No administrator privileges are required.
- Existing vault data under `%LOCALAPPDATA%\WakiliDms` is preserved.

Run installed app:

```powershell
& "$env:LOCALAPPDATA\Programs\WindowsLegalDocumentVault\WindowsLegalDocumentVault.exe"
```

To create a desktop shortcut too:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-local.ps1 -CreateDesktopShortcut
```

## Uninstall the Local Package

From any folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\WindowsLegalDocumentVault\uninstall-local.ps1"
```

Expected result:

- Installed app files are removed.
- Start Menu and desktop shortcuts are removed.
- User vault data is preserved by default.

Only for disposable development data, add:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\WindowsLegalDocumentVault\uninstall-local.ps1" -DeleteUserVaultData
```

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

## Package Smoke Test

Use this before sharing a local package:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-WindowsPackage.ps1
```

Expected result:

- Package is built.
- Packaged `WindowsLegalDocumentVault.exe` starts successfully.
- Smoke script stops the launched process.

## Installed Package Smoke Test

Use this to prove the generated package installs, launches as an installed app, uninstalls, and preserves user vault data:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledWindowsPackage.ps1
```

Expected result:

- Package is built.
- `install-local.ps1` installs into a temporary local folder.
- Installed `WindowsLegalDocumentVault.exe` starts successfully.
- `uninstall-local.ps1` removes app files.
- User vault data is preserved unless `-DeleteUserVaultData` is explicitly supplied.

CI uses the faster framework-dependent variant:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-WindowsPackage.ps1 -FrameworkDependent
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledWindowsPackage.ps1 -FrameworkDependent
```

Do not use the framework-dependent package as the plug-and-play user package unless the target Windows machine already has the required .NET Desktop Runtime. On a clean machine it can show a Windows `.NET` install/update dialog instead of opening the vault. The default self-contained package avoids that problem.

## Installed App Interactive End-to-End Test

Use this after package smoke tests when you need to prove the installed app works through the actual WPF UI, not only through service-level tests:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage
```

Expected result:

- The self-contained package is built and installed to `%LOCALAPPDATA%\Programs\WindowsLegalDocumentVault`.
- Online sample documents are downloaded to `%LOCALAPPDATA%\WakiliDmsInteractiveE2E\online-documents`.
- The installed `WindowsLegalDocumentVault.exe` is launched.
- First-run setup is completed through the visible WPF controls.
- A matter is created through the UI.
- A downloaded DOCX is imported, indexed, and searched.
- A downloaded PDF is queued through the watched scan folder and imported through Scan Inbox.
- A filing pack is exported.
- A backup snapshot is created and restore drill is verified.
- The local backup list is refreshed.
- Backup health shows the local backup snapshot is available.
- A local backup snapshot is selected.
- A selected local backup restore workspace is created and verified.
- Local backup retention preview and cleanup can reduce old backup snapshots while keeping the newest snapshot.
- A copied external backup folder is verified into a separate restore workspace.
- Restore verification reports are created for local, external, and cloud restore checks.
- Cloud backup is enabled against a local provider folder.
- An encrypted cloud backup package is uploaded.
- Backup health shows local and cloud backup snapshots are available.
- The cloud snapshot list is refreshed and a snapshot is selected.
- The selected cloud snapshot is downloaded and restore drill is verified.
- The cloud package is checked for obvious plain-text matter/document leakage.
- Selected cloud and local backup snapshots can be deleted without deleting the live vault.

The script uses isolated app data by default through:

```text
WAKILI_DMS_SETTINGS_PATH
WAKILI_DMS_DATABASE_PATH
```

That keeps repeatable tests away from the normal user data folder. To intentionally drive the default installed app data path, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -UseDefaultUserAppData -KeepAppOpen
```

Use this only for a disposable local setup because it writes `%LOCALAPPDATA%\WakiliDms\settings.json` and `%LOCALAPPDATA%\WakiliDms\wakili-dms.db`.

Current online sample sources used by the script:

- DOCX: `https://raw.githubusercontent.com/rounakdatta/CorrectLy/master/sample.docx`
- PDF: `https://ontheline.trincoll.edu/images/bookdown/sample-local-pdf.pdf`

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
