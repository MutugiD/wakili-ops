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
- Encrypted database backup artifact.
- Restore drill hash validation.
- Restore drill database decryptability check.

Manual verification:

- Restore test succeeds in temporary folder.
- Backup folder does not contain a plain SQLite database copy.

## Slice 11: Installer and Cross-Machine Test

Goal:

- Package app for another Windows machine.

Manual verification:

- Install on clean Windows machine.
- Create or restore vault.
- Open matter and document.

## Slice 12: Admin Install Telemetry and Disable Flow

Goal:

- Track installed app IDs and support owner-controlled enable/disable states.

Automated tests:

- Installation ID generation and preservation.
- Sanitized check-in payload excludes matter, document, path, OCR, and case details.
- Admin registry can upsert check-ins.
- Admin registry can enable and disable an installation ID.
- Admin registry delete does not delete local vault data.

Manual verification:

- App setup shows installation identity fields.
- Home screen shows installation ID and license status.
- Owner admin console lists installation records from a registry file.
- Owner admin console can enable, disable, and delete registry records.

## Slice 13: Optional Cloud-Backup Provider Adapter

Goal:

- Add a provider interface for opt-in encrypted snapshot upload.

Implemented:

- `ICloudBackupProvider` upload/download/list/delete contract.
- `CloudBackupService` that encrypts the whole local backup snapshot before provider upload.
- `LocalFilesystemCloudBackupProvider` as a plug-in test adapter.
- Cloud restore download that decrypts and extracts to a local restore target.

Acceptance tests:

- Upload is rejected when cloud backup is not enabled for the installation.
- Upload is rejected when the license is not active or trial.
- Uploaded provider metadata excludes matter names, party names, court case numbers, document filenames, and OCR/document text.
- Uploaded package bytes do not expose local backup contents in plain text.
- Downloaded package can be decrypted with the recovery key and verified by restore drill.
- Download with the wrong recovery key fails.

## Slice 15: User-Facing Backup Center Cloud Controls

Goal:

- Let the Windows app user enable the local-provider cloud backup option, upload an encrypted cloud backup package, list cloud snapshots, and verify a selected cloud snapshot.

Automated tests:

- Cloud backup upload requires entitlement.
- Cloud backup metadata is redacted.
- Cloud backup package does not expose document or matter details in plain text.
- Downloaded cloud snapshot can pass restore drill.

Manual verification:

- User enables cloud backup against a local provider folder.
- User uploads an encrypted cloud package.
- User refreshes cloud snapshots.
- User selects a snapshot and verifies restore.

## Slice 16: Local Backup Restore Workspace Browser

Goal:

- Let the Windows app user see local backup snapshots and verify a selected backup into a restore workspace without overwriting the live vault.

Automated tests:

- Local backup catalog lists valid snapshots.
- Invalid backup folders are ignored.
- Restore drill verifies selected backup files by hash.
- Installed-app workflow refreshes local backups, selects a snapshot, and verifies a local restore workspace.

Manual verification:

- User runs a backup.
- User refreshes the local backup list.
- User selects a backup snapshot.
- User enters a restore workspace folder.
- User runs selected local restore workspace and sees verification status.

## Slice 17: Cross-Machine Backup Restore Verification

Goal:

- Let a user verify a backup folder copied from another Windows machine without requiring it to live under the current machine's configured backup target.

Automated tests:

- Backup can be copied to an external folder and verified by restore drill.
- Original backup target can be absent during verification.
- Installed-app workflow verifies an externally copied backup through the UI.

Manual verification:

- Copy a backup snapshot folder from another machine or external drive.
- Paste the copied backup folder path into the external backup field.
- Choose a restore workspace folder.
- Enter the recovery key.
- Run external backup verification and confirm restored encrypted artifacts are created.

## Slice 18: Restore Verification Reports

Goal:

- Create a support-friendly report after each restore verification without exposing matter contents or recovery keys.

Automated tests:

- Report file is written to the restore workspace.
- Report contains source kind, source identifier, restore directory, verified file count, and byte count.
- Report does not contain document text or recovery-key values.
- Installed-app workflow verifies reports for local, external, and cloud restore paths.

Manual verification:

- Run selected local restore workspace.
- Run external backup restore verification.
- Run selected cloud restore drill.
- Confirm each restore workspace contains `restore-verification-report.json`.
