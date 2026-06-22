# Iteration Log

This log tracks V1 planning and implementation progress.

## Status Legend

- Planned: documented but not started.
- Active: currently being implemented.
- Complete: implemented, tested, and manually verified.
- Deferred: intentionally moved out of V1.
- Blocked: cannot proceed without a decision or dependency.

## Current Iterations

| Slice | Name | Status | Notes |
| --- | --- | --- | --- |
| 0 | Project and test baseline | Complete | Solution, WPF shell, core/domain, infrastructure, and console test harness scaffolded. Build/test verified with local .NET 10 SDK. |
| 1 | Setup wizard | Complete | Firm profile, vault path, scan folder, backup target, recovery key confirmation, validation, JSON persistence, and app startup smoke verified. |
| 2 | Encrypted vault | Complete | Vault manifest, recovery-key unlock, encrypted object storage/readback, wrong-key failure, and unreadable-object tests verified. |
| 3 | Matter management | Complete | SQLite matter repository, create/list/update tests, UI matter creation form, matter list, and app startup smoke verified. |
| 4 | Document import | Complete | Manual DOC, DOCX, and PDF import into selected matter with supported file checks, encrypted vault storage, hash metadata, SQLite document registration, and UI matter document list. |
| 5 | Watched scan folder | Complete | On-demand watched folder refresh queues supported DOC, DOCX, and PDF files into a pending scan inbox, ignores unsupported files, avoids duplicate queue entries, and imports selected scans into selected matters. |
| 6 | Classification and versioning | Complete | Document type/status editing, filed/served immutability, initial version metadata on import, SQLite document version storage, and selected-document version list UI. |
| 7 | OCR and search | Complete | Local DOCX/text-like PDF extraction, encrypted-vault indexing, SQLite FTS matter search, selected-document indexing UI, and matter search UI. |
| 8 | Filing-pack builder | Complete | Matter filing-pack export with decrypted document copies, JSON manifest, readiness checklist, and unencrypted export warning. |
| 9 | Receipt and court-output capture | Complete | Capture filing receipts, payment receipts, notices, court orders, rulings, and judgments into the selected matter through encrypted vault import. |
| 10 | Backup and restore | Complete | Encrypted vault-object snapshot, recovery-key encrypted database backup, checksum manifest, restore-drill hash validation, and backup UI command. |
| 11 | Installer and cross-machine test | Complete | Local-user Windows package, install/uninstall scripts, packaged executable smoke, CI package smoke, and installation docs. Cross-machine/manual restore remains part of release acceptance. |
| 12 | Admin install telemetry and disable flow | Complete | Installation identity, license status, sanitized check-in payload, app-side disabled/revoked gate, owner admin console, and file-backed registry enable/disable/delete commands. |
| V1 hardening | Windows end-to-end workflow and edge cases | Complete | Added focused Windows E2E script, full matter workflow test, restore target safety fix, unsafe backup target test, destructive restore target test, and tampered backup hash test. |
| V1 packaging | Windows EXE package/install/run | Complete | Product-named `WindowsLegalDocumentVault.exe`, package build, local install/uninstall scripts, installed executable smoke, and Windows installation guide. |
| V1 interactive E2E | Installed app workflow with online documents | Complete | Added WPF automation IDs, scrollable Home workflow, isolated app-data test overrides, online DOCX/PDF downloads, and installed-app UI automation from setup through backup/restore drill. |
| 13 | Optional cloud-backup provider adapter | Complete | Provider interface, recovery-key encrypted cloud package, redacted metadata, local filesystem provider, download/extract flow, and restore-drill validation. |
| 14 | Hosted admin/payment entitlement integration | Skipped | Explicitly skipped for now. Hosted payment, entitlement sync, and backup-health reporting remain future work. |
| 15 | User-facing Backup Center cloud controls | Complete | Local-provider cloud backup enablement, encrypted cloud upload, snapshot list refresh, selected snapshot download, and cloud restore drill in the Windows app. |
| 16 | Local backup restore workspace browser | Complete | Local backup catalog, snapshot list refresh, selected local backup restore workspace, and installed-app UI verification. |

## Deferred From V1

| Item | Reason |
| --- | --- |
| Direct Judiciary portal automation | Legal, operational, and authentication risk. |
| Production cloud backup upload | Future opt-in provider after local backup and admin licensing are reliable. |
| Direct TWAIN/WIA scanner control | Watched folder support is simpler and more compatible for V1. |
| Local Matter RAG Connector | Depends on trustworthy document vault and OCR/search foundation. |
| Wakili-Mkononi Matter AI Integration | Depends on Local Matter RAG Connector. |

## Decision Log

| Decision | Choice |
| --- | --- |
| App stack | .NET 10 LTS + WPF. |
| Metadata database | SQLite. |
| Search | SQLite FTS for V1. |
| Storage | Encrypted local vault objects. |
| Scanner integration | Watched folders first. |
| E-filing | Filing-pack preparation only. |
