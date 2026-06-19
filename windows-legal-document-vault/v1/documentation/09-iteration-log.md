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
| 9 | Receipt and court-output capture | Planned | Attach receipts, notices, orders, registry messages. |
| 10 | Backup and restore | Planned | Encrypted snapshots and restore drill. |
| 11 | Installer and cross-machine test | Planned | Windows install and restore verification. |

## Deferred From V1

| Item | Reason |
| --- | --- |
| Direct Judiciary portal automation | Legal, operational, and authentication risk. |
| Cloud backup | Future opt-in after local backup is reliable. |
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
