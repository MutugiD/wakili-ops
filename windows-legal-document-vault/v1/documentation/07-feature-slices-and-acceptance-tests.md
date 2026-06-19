# Feature Slices and Acceptance Tests

## Slice Rule

Each slice must include:

- User-visible behavior.
- Automated tests.
- Manual verification.
- Documentation update if behavior changes.

Do not start the next slice until the current slice can be demonstrated.

## Slice 0: Project and Test Baseline

Goal:

- Create app/test skeleton.

Acceptance:

- App launches.
- Test command runs.
- README explains how to run app and tests.

## Slice 1: Setup Wizard

Goal:

- Capture firm settings and create setup-complete state.

Automated tests:

- Required fields validation.
- Path validation.
- Settings persistence.

Manual verification:

- Fresh app opens setup wizard.
- Completed setup opens home screen.

## Slice 2: Encrypted Vault

Goal:

- Create and unlock encrypted vault.

Automated tests:

- Encrypt/decrypt round trip.
- Wrong key failure.
- Hash integrity verification.

Manual verification:

- Stored file is not readable as plain text.

## Slice 3: Matter Management

Goal:

- Create, list, open, and update matters.

Automated tests:

- Matter persistence.
- Required matter name.
- Audit event on create/update.

Manual verification:

- Matter remains after app restart.

## Slice 4: Document Import

Goal:

- Import supported files into a matter.

Automated tests:

- Hash calculation.
- Encrypted vault storage and readback.
- Matter document metadata registration.
- Unsupported file handling.

Manual verification:

- User imports a PDF and sees it under the matter.
- User imports a DOCX and sees it under the matter.

## Slice 5: Watched Scan Folder

Goal:

- Detect files added to scan folder.

Automated tests:

- Scan folder refresh queues supported files.
- Unsupported files are ignored.
- Duplicate scanned file is flagged.
- Pending scan can be imported into a selected matter.

Manual verification:

- Dropping a PDF in scan folder and refreshing shows it in Scan Inbox.
- Importing a pending scan moves it into the selected matter document list.

## Slice 6: Classification and Versioning

Goal:

- Assign type and lifecycle status.

Automated tests:

- Status transition rules.
- Filed/served immutability.
- Classification persistence.
- Initial document version metadata.

Manual verification:

- User edits document type/status and sees the updated matter document list.
- User selects a document and sees initial version history.
- User cannot reclassify a filed or served document.

## Slice 7: OCR and Search

Goal:

- Extract text and search locally.

Automated tests:

- DOCX text extraction.
- Text-like PDF extraction.
- Encrypted-vault document indexing.
- SQLite FTS matter search result.

Manual verification:

- User searches for text from imported document.
- User can launch the Windows app and keep it running through the startup smoke.

## Slice 8: Filing Pack Builder

Goal:

- Export selected documents for manual e-filing.

Automated tests:

- Pack snapshot creation.
- Readiness warnings.
- Export manifest.
- Decrypted exported copies match selected vault documents.

Manual verification:

- Export folder contains documents, index, checklist, and readiness report.
- User sees warning that export folders are not encrypted.

## Slice 9: Receipt and Court Output Capture

Goal:

- Attach receipt and court outputs to matter/filing pack.

Automated tests:

- Receipt attachment.
- Prepared to Filed transition requires confirmation.
- Non-output document types are rejected by court-output capture.

Manual verification:

- User attaches receipt and filing pack status becomes Filed.
- User captures a receipt/order/ruling/judgment/notice into the selected matter.

## Slice 10: Backup and Restore

Goal:

- Create encrypted backup and restore it.

Automated tests:

- Snapshot manifest.
- Checksum validation.
- Restore one matter.

Manual verification:

- Restore test succeeds in temporary folder.

## Slice 11: Installer and Cross-Machine Test

Goal:

- Package app for another Windows machine.

Manual verification:

- Install on clean Windows machine.
- Create or restore vault.
- Open matter and document.
