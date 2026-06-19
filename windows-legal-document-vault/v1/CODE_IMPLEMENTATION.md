# Code Implementation Plan

This file is a short pointer for the code build sequence. The detailed V1 implementation plan lives at `documentation/10-code-implementation-plan.md`.

## Current Slice

Slice 11: installer and cross-machine test.

Completed:

- .NET solution and project layout.
- WPF shell project.
- Core domain project.
- Infrastructure project.
- Console-based test harness.
- Initial setup validation and matter lifecycle code.
- Setup wizard.
- JSON settings persistence.
- App startup smoke.
- Encrypted vault create/unlock.
- Encrypted object store/read.
- Wrong-key vault failure tests.
- SQLite matter repository.
- Matter create/list/update tests.
- Matter creation and listing UI.
- SQLite document repository.
- Manual DOC, DOCX, and PDF document import.
- Encrypted vault registration for imported document bytes.
- Matter document list UI.
- Document import tests for PDF, DOCX, and unsupported file rejection.
- SQLite scan inbox repository.
- Watched scan folder refresh service.
- Pending scan inbox UI.
- Import selected scan into selected matter.
- Scan inbox tests for queueing, duplicate detection, unsupported file ignore, and pending scan import.
- Document type/status editing.
- Filed/served immutability at document classification level.
- Initial document version metadata on import.
- SQLite document version repository.
- Selected-document version list UI.
- Classification/versioning tests.
- Local DOCX and text-like PDF extraction.
- SQLite FTS matter search repository.
- Selected-document indexing UI.
- Matter-scoped search UI.
- OCR/search tests.
- Windows app startup smoke script.
- Matter filing-pack export.
- Decrypted export copies from encrypted vault objects.
- Filing-pack JSON manifest.
- Filing readiness checklist.
- Filing-pack export tests.
- Receipt and court-output capture service.
- WPF receipt/court-output capture controls.
- Capture tests for filing receipt and non-output rejection.
- Local backup snapshot service.
- Recovery-key encrypted database backup artifact.
- Encrypted vault object backup copy.
- Backup manifest with checksums.
- Restore drill that validates hashes and decryptability without writing a plain database copy.
- Backup and restore drill UI command.
- Backup and restore drill tests.

## SDK Requirement

This machine originally had the .NET runtime but no .NET SDK. A user-local .NET 10 SDK was installed at `C:\Users\admin\.dotnet`.

Expected commands after SDK installation:

```powershell
dotnet build WakiliDms.sln
dotnet run --project tests/WakiliDms.Tests/WakiliDms.Tests.csproj
dotnet run --project src/WakiliDms.App/WakiliDms.App.csproj
```

## Module Sequence

1. Slice 0: project and test baseline.
2. Slice 1: setup wizard and settings persistence.
3. Slice 2: encrypted vault.
4. Slice 3: matter management.
5. Slice 4: document import.
6. Slice 5: watched scan folder.
7. Slice 6: classification and versioning.
8. Slice 7: OCR and search.
9. Slice 8: filing-pack builder.
10. Slice 9: receipt and court-output capture.
11. Slice 10: backup and restore.
12. Slice 11: installer and cross-machine test. Current next slice.
