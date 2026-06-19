# Module Breakdown

## Module Order

Build modules in this order:

1. App shell.
2. Setup wizard and settings.
3. Encrypted vault.
4. Matter management.
5. Document import.
6. Classification and versioning.
7. OCR.
8. Search.
9. Filing-pack builder.
10. Receipt and court-output capture.
11. Backup and restore.
12. Installer and cross-machine smoke test.

## App Shell

Purpose:

- Provide the WPF application frame, navigation, dependency injection, logging, and test baseline.

Done when:

- App launches.
- Home route renders.
- Tests run in CI/local command.
- No real document operations are required yet.

## Setup Wizard and Settings

Purpose:

- Capture firm profile, vault path, watched scan folder, backup target, and recovery-key setup.

Inputs:

- Firm name.
- Primary user.
- Local vault path.
- Scanner folder.
- Backup folder.

Outputs:

- Saved app settings.
- Setup-complete state.
- Audit event for setup completion.

Done when:

- App detects first run.
- User can complete setup.
- Invalid paths produce clear messages.

## Encrypted Vault

Purpose:

- Create, unlock, read, and write encrypted vault objects.

Inputs:

- Vault path.
- Recovery key.
- File bytes.

Outputs:

- Encrypted object.
- Object metadata.
- Hash and integrity record.

Done when:

- A file can be encrypted, stored, read, and verified.
- Wrong key fails safely.
- Recovery key warning is shown.

## Matter Management

Purpose:

- Create, list, view, and update legal matters.

Done when:

- User can create a matter and see it in the matter list.
- Matter metadata persists after app restart.
- Audit log records creation and updates.

## Document Import

Purpose:

- Bring existing files into the vault from manual selection or watched scanner folder.

Done when:

- Supported files import into a selected matter.
- File hash and source metadata are recorded.
- Duplicate files are detected.
- Unsupported/corrupt files produce import reports.

## Classification and Versioning

Purpose:

- Assign document type and lifecycle status.

Done when:

- User can classify a document.
- User can mark versions as draft, approved, signed, filed, served, amended, or archived.
- Filed and served versions cannot be overwritten.

## OCR

Purpose:

- Extract text for search and later retrieval.

Done when:

- OCR status is tracked per document version.
- OCR failure does not block access to the original document.
- Extracted text is searchable after indexing.

## Search

Purpose:

- Search matters and document text locally.

Done when:

- User can search by matter metadata and document text.
- Search results show matter, document, version status, and source.

## Filing-Pack Builder

Purpose:

- Prepare court-ready export folders for manual e-filing.

Done when:

- User selects document versions.
- App creates an immutable pack snapshot.
- App exports PDFs, checklist, index, and readiness report.
- Pack remains Prepared until receipt confirmation.

## Receipt and Court-Output Capture

Purpose:

- Store portal receipts, notices, orders, rulings, registry messages, and rejection/correction requests.

Done when:

- User can attach a court output to a matter or filing pack.
- Filing pack can be marked Filed only through explicit confirmation.

## Backup and Restore

Purpose:

- Protect the vault from machine failure and accidental loss.

Done when:

- User can create encrypted backup snapshot.
- Snapshot checksum is verified.
- User can test restore without overwriting the live vault.

## Installer

Purpose:

- Package the app for installation on Windows machines.

Done when:

- App installs on a clean Windows 10/11 machine.
- User can restore a sample vault on another machine.

