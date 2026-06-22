# Code Implementation Plan

This file is a short pointer for the code build sequence. The detailed V1 implementation plan lives at `documentation/10-code-implementation-plan.md`.

## Current Slice

Next slice: cross-machine restore wizard or production cloud provider adapter can be resumed when release priorities are set.

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
- Local Windows package build script.
- Self-contained and framework-dependent package modes.
- Generated current-user install/uninstall scripts.
- Packaged executable smoke test.
- CI package smoke step.
- Installation ID, device nickname, license key, and license status in app settings.
- WPF display for installation ID and license status.
- Local license gate for disabled/revoked installations.
- Sanitized installation check-in payload contract.
- Owner admin console project.
- File-backed admin installation registry.
- Admin list/check-in/enable/disable/delete commands.
- Admin registry tests proving enable/disable/delete and no vault-data deletion.
- Windows end-to-end workflow script.
- End-to-end matter workflow test covering setup, vault, matter, scan inbox, import, classification, OCR/search, filing pack export, receipt capture, backup, restore drill, and admin registry.
- Backup/restore edge tests for unsafe backup target, destructive restore target, and tampered backup hash rejection.
- Restore drill safety fix preventing restore targets that equal or contain the backup directory.
- Product-named `WindowsLegalDocumentVault.exe`.
- Installed package smoke script that installs, launches, uninstalls, and verifies vault data preservation.
- Installed app interactive workflow script that downloads online DOCX/PDF samples and drives the WPF UI from setup through backup/restore.
- WPF automation IDs for repeatable GUI testing.
- Scrollable Home workflow so lower-stage actions are reachable on normal Windows viewports.
- Optional cloud-backup provider interface.
- Recovery-key encrypted cloud backup package creation.
- Local filesystem cloud-backup provider for adapter testing.
- Cloud backup entitlement, metadata redaction, download, and restore-drill tests.
- User-facing Backup Center controls for local-provider cloud backup.
- Installed app interactive E2E verifies local backup, encrypted cloud upload, snapshot selection, and cloud restore drill.
- Local backup catalog and restore workspace browser.
- Installed app interactive E2E verifies local backup list refresh, selected local restore workspace, and restored encrypted database artifact creation.
- Cross-machine backup verification fields and command for copied backup folders.
- Installed app interactive E2E verifies copied external backup restore into a separate workspace.
- Restore verification report service for local, external, and cloud restore checks.
- Installed app interactive E2E verifies restore report creation across all restore paths.
- Backup health evaluator and home status panel for local/cloud backup posture.
- Installed app interactive E2E verifies backup health after local and cloud backups.
- Safe selected local backup deletion and selected cloud backup deletion.
- Installed app interactive E2E verifies backup cleanup leaves the live vault intact.
- Local backup retention preview/apply policy with keep-latest and delete-older-than controls.
- Installed app interactive E2E verifies retention cleanup reduces two local backups to one retained snapshot.
- Confirmation prompts before local backup deletion, retention cleanup, and cloud backup package deletion.
- Installed app interactive E2E clicks real backup cleanup confirmation dialogs.
- Installed app interactive E2E chooses No before Yes and verifies canceled backup cleanup preserves artifacts.
- Persistent latest restore report summary in the status panel for local, external, and cloud restore drills.

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
12. Slice 11: installer and cross-machine test.
13. Slice 12: admin install telemetry and disable flow.
14. Slice 13: optional cloud-backup provider adapter.
15. Slice 14: hosted admin/payment entitlement integration. Skipped for now.
16. Slice 15: user-facing Backup Center cloud controls.
17. Slice 16: local backup restore workspace browser.
18. Slice 17: cross-machine backup restore verification.
19. Slice 18: restore verification reports.
20. Slice 19: backup health summary.
21. Slice 20: backup retention and cleanup.
22. Slice 21: local backup retention policy automation.
23. Slice 22: backup cleanup confirmation prompts.
24. Slice 23: backup cleanup cancellation coverage.
25. Slice 24: restore report status summary.
