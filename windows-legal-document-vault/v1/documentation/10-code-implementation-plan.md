# Code Implementation Plan

This document tracks how the Windows Legal Document Vault codebase should grow module by module.

## Current Baseline

Slices 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, and 12 are complete.

Current code scaffold:

- `WakiliDms.sln`
- `src/WakiliDms.App`
- `src/WakiliDms.Core`
- `src/WakiliDms.Infrastructure`
- `tests/WakiliDms.Tests`

Current implemented behavior:

- WPF app shell.
- First-run setup wizard.
- Main window home state after setup completion.
- Core result type.
- Matter entity creation.
- Document type/status enums.
- Filed/served immutability helper.
- App settings model.
- Setup validation.
- JSON settings store.
- Encrypted vault service.
- Vault manifest creation.
- Recovery-key based vault unlock.
- Encrypted object storage and readback.
- Wrong-key failure handling.
- SQLite matter repository.
- Matter create/list/update persistence.
- Matter creation UI.
- Matter list UI.
- SQLite document repository.
- Manual DOC, DOCX, and PDF import service.
- Imported document metadata registration.
- Matter document list UI.
- SQLite scan inbox repository.
- Watched scan folder refresh service.
- Pending scan inbox UI.
- Pending scan import into selected matter.
- Document classification/status update flow.
- Filed/served immutability guard.
- Initial document version metadata on import.
- Selected-document version list UI.
- Local DOCX and text-like PDF text extraction.
- SQLite FTS matter search.
- Selected-document indexing command.
- Matter search command and result list.
- Matter filing-pack export.
- Filing-pack manifest and readiness checklist generation.
- Receipt and court-output capture.
- Local backup snapshot.
- Recovery-key encrypted database backup artifact.
- Backup checksum manifest.
- Restore drill verification.
- Local Windows package scripts.
- Local install/uninstall scripts generated into the package.
- Packaged executable smoke test.
- Installation identity and license state in app settings.
- Sanitized installation check-in payload contract.
- Owner admin console scaffold.
- File-backed admin installation registry.
- Admin enable/disable/delete controls.
- Windows end-to-end workflow script.
- Full matter workflow test across setup, encrypted vault, matter, scan inbox, import, classification, OCR/search, filing pack export, court-output capture, backup, restore drill, and admin registry.
- Restore drill safety validation.
- Product-named `WindowsLegalDocumentVault.exe`.
- Installed package smoke test.
- Installed app interactive workflow test with online DOCX/PDF samples.
- WPF automation IDs and scrollable Home workflow for real GUI E2E validation.
- Optional cloud-backup provider interface.
- Recovery-key encrypted cloud backup package creation.
- Local filesystem cloud-backup provider for adapter testing.
- Cloud backup entitlement, metadata redaction, download, and restore-drill tests.
- User-facing Backup Center cloud controls backed by the local filesystem provider.
- Console test harness with first baseline tests.
- App startup smoke test.

## Local Tooling Status

The current machine originally had .NET runtime 6.0.22 but no .NET SDK. A user-local .NET 10 SDK was installed at `C:\Users\admin\.dotnet` for build/test verification.

Expected commands after .NET 10 SDK installation:

```powershell
dotnet build WakiliDms.sln
dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj
dotnet run --project src/WakiliDms.App/WakiliDms.App.csproj
```

## Completed Slice: Setup Wizard

Implemented:

- Setup wizard screen flow.
- Firm profile fields.
- Vault path selector.
- Watched scan folder path selector.
- Backup target path selector.
- Recovery key confirmation.
- Settings persistence through `ISettingsStore`.
- First-run detection.

Acceptance criteria:

- Fresh app opens setup wizard.
- User cannot finish setup with missing required fields.
- Cloud backup cannot be enabled in V1.
- Completed setup persists and opens home screen on restart.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- WPF app startup smoke.

## Completed Slice: Encrypted Vault

Implemented:

- Vault creation service.
- Recovery-key based encryption key derivation.
- Encrypted vault object write/read.
- SHA-256 object hashing.
- Vault manifest metadata.
- Wrong-key failure behavior.
- Tests proving stored object bytes are not plain text.

Acceptance criteria:

- A vault can be created at the configured path.
- A sample document can be encrypted, stored, read, and verified.
- Wrong key fails safely.
- Stored vault object is not readable as plain text.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- WPF app startup smoke.

## Completed Slice: Matter Management

Implemented:

- Matter repository contract.
- SQLite-backed matter persistence.
- Matter create/list/update operations.
- Matter creation form on Home.
- Matter list on Home.
- Tests for persistence, listing, and update behavior.

Acceptance criteria:

- User can create a matter from the app after setup.
- Matters persist across app restart.
- Matter list shows created matters.
- Invalid matter names are rejected.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- WPF app startup smoke.

## Completed Slice: Document Import

Implemented:

- Manual path-based import service.
- Supported file type validation.
- SHA-256 file hashing.
- Store imported bytes in the encrypted vault.
- Create initial document metadata.
- Show imported documents under the selected matter.
- Tests for PDF import, DOCX import, vault byte readback, encrypted storage, metadata registration, and unsupported file rejection.

Acceptance criteria:

- User can import a PDF into a matter.
- User can import a DOCX into a matter.
- Unsupported file type is rejected.
- Original file bytes are stored in the encrypted vault.
- Import tests cover success and unsupported file cases.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- WPF app startup smoke.

## Completed Slice: Watched Scan Folder

Implemented:

- On-demand scan folder refresh.
- Queue files dropped into the configured scan folder.
- Show pending scanned files before matter assignment.
- Reuse the document import service once a matter is chosen.
- Duplicate queue detection by source path and file hash.
- Unsupported file ignore.
- Tests for queueing, duplicate detection, unsupported file ignore, and pending scan import.

Acceptance criteria:

- User can refresh the configured watched scan folder.
- Supported DOC, DOCX, and PDF files appear in the scan inbox.
- Unsupported files are ignored.
- Re-refreshing does not queue the same scan twice.
- User can import a pending scan into the selected matter.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- WPF app startup smoke.

## Completed Slice: Classification and Versioning

Implemented:

- Editable document type and lifecycle status.
- Filed/served immutability at the document level.
- Classification persistence in SQLite.
- Initial document version metadata on import.
- Document version repository and selected-document version list.
- Tests for classification update, immutable filed document, and initial version metadata.

Acceptance criteria:

- User can select a matter document and update type/status.
- Filed and served documents cannot be reclassified.
- Imported documents get version 1 metadata.
- User can see version metadata for the selected document.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- WPF app startup smoke.

## Completed Slice: OCR and Search

Implemented:

- Local text extraction adapter for DOCX and text-like PDFs.
- SQLite FTS table for searchable document text.
- Matter-scoped search UI.
- Index selected document from encrypted vault object.
- Tests for DOCX extraction, PDF extraction, vault indexing, and matter search.

Acceptance criteria:

- User can index selected DOCX/PDF documents using the vault recovery key.
- User can search within the selected matter.
- Search results show matching document name and snippet.
- Indexed text stays local in SQLite.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- `scripts\Start-AppSmoke.ps1`

## Completed Slice: Filing-Pack Builder

Implemented:

- Export selected matter documents into a user-chosen normal folder.
- Create filing manifest and readiness checklist.
- Warn that export folders are not encrypted.
- Tests for decrypted file copies, manifest, and checklist.

Acceptance criteria:

- User can export the selected matter document list as a filing pack.
- Export folder includes decrypted document copies.
- Export folder includes `filing-pack-manifest.json`.
- Export folder includes `filing-readiness-checklist.txt`.
- App warns that export folders are not encrypted.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- `scripts\Start-AppSmoke.ps1`

## Completed Slice: Receipt and Court-Output Capture

Implemented:

- Attach portal receipts and court outputs to matters.
- Classify receipts/orders/notices separately from filing-pack source documents.
- Reject non-output document types for court-output capture.
- UI controls for receipt/order/ruling/judgment/notice capture.
- Tests for filing receipt capture and non-output rejection.

Acceptance criteria:

- User can capture filing receipts and court outputs into the selected matter.
- Captured files are stored as encrypted vault objects.
- Captured files appear in the matter document list with the selected output type.
- Pleadings and other non-output types are rejected by this capture path.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- `scripts\Start-AppSmoke.ps1`

## Completed Slice: Backup and Restore

Implemented:

- Local encrypted backup snapshot manifest.
- Copy encrypted vault objects into backup target.
- Encrypt the SQLite metadata/search database into a `.backup` artifact with the recovery key.
- Write checksums for each backup artifact.
- Restore drill into a temporary folder.
- Verify backup hashes.
- Verify encrypted database decryptability without leaving a plain database copy.
- UI command for backup and restore drill.
- Tests for snapshot manifest, encrypted database artifact, and restore drill.

Acceptance criteria:

- User can create a local backup into the configured backup target.
- Backup includes encrypted vault objects and an encrypted database artifact.
- Backup does not include a plain SQLite database copy.
- Restore drill verifies all hashes and encrypted database decryptability.
- Restore drill does not overwrite the live vault or write a plain database copy.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- `scripts\Start-AppSmoke.ps1`

## Completed Slice: Installer and Cross-Machine Test

Implemented:

- Windows install packaging.
- Self-contained `win-x64` package build.
- Framework-dependent package build for CI speed.
- Current-user install script under `%LOCALAPPDATA%\Programs`.
- Start Menu shortcut creation.
- Optional desktop shortcut creation.
- Uninstall script that preserves user vault data by default.
- Packaged executable smoke test.
- CI package smoke step.
- Installer documentation update.

Acceptance criteria:

- Developer can build a package from the V1 app root.
- Package includes executable, install script, uninstall script, run command, README, and manifest.
- Packaged app starts without using the source tree.
- Local install does not require administrator privileges.
- Uninstall preserves user vault data unless explicitly told to delete it.

Verification:

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-WindowsPackage.ps1 -FrameworkDependent`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-WindowsPackage.ps1`

## Completed Slice: Admin Install Telemetry and Disable Flow

Implemented:

- Installation ID generation on setup or settings migration.
- License key and device nickname capture.
- License status stored in app settings.
- WPF display for installation ID, device nickname, and license status.
- Local disabled/revoked gate for license-gated features.
- Sanitized installation check-in payload contract.
- Owner admin console project at `src/WakiliDms.Admin`.
- File-backed admin installation registry.
- Admin commands: `list`, `checkin`, `enable`, `disable`, and `delete`.
- Tests proving check-in payload excludes document/matter details.
- Tests proving admin enable/disable/delete behavior.
- Test proving admin registry deletion does not delete local vault data.

Acceptance criteria:

- Each install has a stable installation ID.
- Admin tracking payload contains only allowed metadata.
- Admin registry can list, enable, disable, and delete installation IDs.
- Disabled/revoked local license states stop license-gated app actions.
- Disable/delete never deletes local vault data.

Verification:

- `dotnet build WakiliDms.sln`
- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj`
- `dotnet run --project src/WakiliDms.Admin/WakiliDms.Admin.csproj -- list --registry <path>`
- `scripts\Start-AppSmoke.ps1`

## Completed Hardening Pass: Windows End-to-End Workflow

Implemented:

- `scripts\Test-EndToEndWorkflow.ps1`.
- Test filter support in the console test harness.
- Full end-to-end matter workflow test.
- CI step for the Windows end-to-end workflow.
- Restore drill guard against restore targets equal to, or above, the backup directory.
- Edge tests for backup target inside vault, destructive restore targets, and tampered backup files.

Acceptance criteria:

- One command runs the focused E2E flow on Windows.
- The flow covers setup, vault, matter, scan inbox, document import, classification, search, filing pack export, receipt capture, backup, restore drill, and admin registry.
- Restore drill cannot delete the backup it is validating.
- Tampered backup files fail restore validation by hash.

Verification:

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1 -Visible -IncludePackageSmoke`

## Completed Packaging Pass: Windows EXE Package, Install, and Run

Implemented:

- App package executable name is `WindowsLegalDocumentVault.exe`.
- `Build-WindowsPackage.ps1` creates a self-contained Windows package and zip by default.
- Package includes install, uninstall, run command, README, and manifest.
- `Test-WindowsPackage.ps1` launches the packaged `.exe`.
- `Test-InstalledWindowsPackage.ps1` installs to a local folder, launches the installed `.exe`, uninstalls, and verifies user vault data is preserved.
- CI runs both package smoke and installed-package smoke.

Verification:

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-WindowsPackage.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledWindowsPackage.ps1`

## Completed Hardening Pass: Installed App Interactive E2E

Implemented:

- `Test-InstalledAppInteractiveWorkflow.ps1` for installed-app GUI automation.
- Online DOCX and PDF download step for realistic document input.
- Isolated settings/database path overrides through `WAKILI_DMS_SETTINGS_PATH` and `WAKILI_DMS_DATABASE_PATH`.
- WPF automation IDs for setup, matter, import, scan inbox, search, filing pack, court output, backup, and list controls.
- Scrollable Home workflow so lower-stage controls are reachable in a normal Windows app window.
- Selected-matter command-state refresh for search, filing-pack export, and court-output capture.

Verification:

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -UseDefaultUserAppData -KeepAppOpen`

## Completed Slice: Optional Cloud-Backup Provider Adapter

Implemented:

- Provider-neutral `ICloudBackupProvider` contract.
- Cloud upload service that packages a local backup directory and encrypts the package with the user's recovery key before provider upload.
- Redacted `CloudBackupSnapshotMetadata` with installation ID, snapshot ID, timestamp, encrypted byte length, encrypted package hash, and upload status only.
- Local filesystem provider for repeatable adapter tests.
- Cloud download/decrypt/extract flow.
- Restore drill compatibility after cloud download.

Verification:

- `dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj --configuration Release`

## Completed Slice: User-Facing Backup Center Cloud Controls

Implemented:

- Cloud backup provider path persisted in app settings.
- Backup Center controls for local-provider cloud backup enablement.
- Encrypted cloud upload from a freshly created local backup snapshot.
- Cloud snapshot list refresh.
- Selected cloud snapshot download into a restore target.
- Restore drill verification from the cloud-downloaded snapshot.
- Installed-app interactive workflow now verifies local backup, cloud upload, encrypted package creation, snapshot selection, and cloud restore drill.

Verification:

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`

## Completed Slice: Local Backup Restore Workspace Browser

Implemented:

- `LocalBackupCatalogService` for listing valid local backup snapshots from the configured backup target.
- Local backup snapshot view model and right-panel list in the Windows app.
- Backup Center controls to refresh local backups and run a selected local restore workspace.
- Restore workspace output is written under the configured local restore folder by snapshot ID.
- Installed-app interactive workflow now verifies local snapshot refresh, snapshot selection, restore workspace creation, and restored encrypted database artifact presence.

Verification:

- `dotnet run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj --configuration Release -- --filter "Local backup catalog"`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`

## Completed Slice: Cross-Machine Backup Restore Verification

Implemented:

- External backup folder path and external restore workspace fields in the Windows app.
- Restore verification command for backups copied from another machine or drive.
- Restore drill reuse so copied backups get the same manifest, hash, and recovery-key checks as local backups.
- Core test proving a copied backup can verify after the original backup target is removed.
- Installed-app interactive workflow now copies a real backup to an external folder and verifies it through the UI.

Verification:

- `dotnet run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj --configuration Release -- --filter "copied from another machine"`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`

## Completed Slice: Restore Verification Reports

Implemented:

- `RestoreVerificationReport` and `RestoreVerificationReportService`.
- Local backup restore workspace reports.
- External backup restore verification reports.
- Cloud backup restore drill reports.
- Installed-app interactive workflow checks report files across all restore paths.

Verification:

- `dotnet run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj --configuration Release -- --filter "Restore verification report"`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`

## Completed Slice: Backup Health Summary

Implemented:

- `BackupHealthEvaluationService` that evaluates local and cloud backup snapshots.
- Home status panel backup health text.
- Last local backup timestamp.
- Last cloud backup timestamp.
- Installed-app interactive workflow assertions for local-only and local-plus-cloud health states.

Verification:

- `dotnet run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj --configuration Release -- --filter "Backup health"`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`

## Following Slices

1. Production cloud provider adapter after provider choice.
2. Backup retention and cleanup policy.
3. Hosted admin/payment entitlement integration, when monetization backend is prioritized.
