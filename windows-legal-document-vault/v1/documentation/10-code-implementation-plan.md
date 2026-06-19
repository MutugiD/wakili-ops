# Code Implementation Plan

This document tracks how the Windows Legal Document Vault codebase should grow module by module.

## Current Baseline

Slices 0, 1, 2, 3, 4, 5, and 6 are complete.

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

## Next Slice: OCR and Search

Build next:

- Local text extraction adapter for DOCX and text-like PDFs.
- OCR status metadata.
- SQLite FTS table for searchable document text.
- Matter-scoped search UI.

## Following Slices

1. Filing-pack builder.
2. Receipt and court-output capture.
3. Backup and restore.
4. Installer and cross-machine verification.
