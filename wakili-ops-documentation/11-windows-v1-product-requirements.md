# Windows V1 Product Requirements

## Purpose

This document converts the Windows Legal Document Vault concept into practical Windows MVP requirements. It is the bridge between product documentation and future code planning.

The first build should behave like a dependable office utility: install it, point it at a vault and scanner folder, scan/import documents, organize by matter, prepare a filing pack, and back up safely.

## Product Goal

Deliver a Windows local-first legal document system for Kenyan solo advocates and small firms.

The V1 product must:

- Run on ordinary Windows office machines.
- Keep documents local by default.
- Support scanner-folder workflows.
- Make scanned documents searchable.
- Organize documents by matter.
- Track draft, signed, filed, served, amended, and archived versions.
- Prepare e-filing packs for manual upload.
- Capture receipts, notices, orders, and court outputs.
- Back up and restore the vault.

## Non-Goals

V1 must not:

- Directly automate login or filing on `efiling.court.go.ke`.
- Become a cloud-first storage system.
- Replace legal practice management, accounting, or billing.
- Provide legal advice.
- Train AI models on firm documents.
- Require always-on internet.
- Require an IT consultant for single-user setup.

## Supported Environment

### Operating System

Minimum target:

- Windows 10 64-bit.
- Windows 11 64-bit.

### Hardware Assumption

The product should tolerate modest office machines:

- 8 GB RAM minimum, 16 GB recommended.
- 2-core CPU minimum, 4-core recommended.
- SSD recommended for OCR and search performance.
- External drive support for backups.

### Scanner Assumption

V1 should not depend on direct scanner-driver integration.

Recommended approach:

- Support watched folders.
- Let users keep using existing scanner software.
- Import PDFs and images written into the watched folder.

Direct TWAIN/WIA scanner integration can be evaluated after validation.

## First-Run Requirements

The setup wizard must collect:

- Firm name.
- Primary user name.
- Vault location.
- Watched scan folder.
- Backup target.
- Recovery-key setup.
- Optional app PIN.

The wizard must explain:

- Documents stay local by default.
- Cloud backup is off by default.
- Losing the recovery key can make encrypted backups unrecoverable.
- Filing packs are prepared locally and uploaded manually by the user.

## Core User Stories

### Matter Creation

As an advocate or clerk, I can create a matter with:

- Matter name.
- Internal reference.
- Parties.
- Client.
- Court and station.
- Case number if known.
- Practice area.
- Responsible advocate.

### Document Import

As a clerk, I can import:

- Scanned PDFs.
- Image PDFs.
- JPG/JPEG.
- PNG.
- TIFF.
- DOCX.
- Existing folders.

The system records original filename, import source, hash, timestamp, user, and matter assignment.

### OCR and Search

As a user, I can search documents by:

- Matter.
- Party.
- Case number.
- Document title.
- Document type.
- Full text.
- Date.
- Status.

OCR must preserve the original file and store extracted text separately.

### Classification

As a user, I can classify a document as:

- Pleading.
- Affidavit.
- Annexure.
- Submission.
- Authority.
- Court order.
- Ruling.
- Judgment.
- Filing receipt.
- Payment receipt.
- Notice.
- Letter.
- Evidence.
- Unknown.

The system may suggest classification but must allow user confirmation.

### Version Tracking

As an advocate, I can track versions through:

- Imported.
- Draft.
- Reviewed.
- Approved.
- Signed.
- Scanned signed copy.
- Filing pack candidate.
- Filed.
- Served.
- Amended.
- Rejected by registry.
- Corrected.
- Archived.

Filed and served versions must not be overwritten.

### Filing Pack

As a clerk, I can select approved documents and generate a filing pack.

The system creates:

- Filing pack folder.
- Document index.
- Readiness report.
- Upload checklist.
- Selected PDFs.
- Snapshot of selected versions.

The system checks:

- PDF format.
- File size threshold.
- Password/encryption.
- Blank pages.
- Duplicate files.
- Page count.
- OCR confidence.
- 300 DPI where detectable.
- Naming convention.
- Signed-version risk.

### Receipt Capture

After manual filing, the user can attach:

- Filing receipt.
- Payment receipt.
- Notice of electronic filing.
- Court order.
- Registry message.
- Rejection/correction request.

The filing pack changes from Prepared to Filed only after user confirmation.

### Backup and Restore

As a firm owner, I can:

- Choose a backup target.
- Run backup now.
- See last backup status.
- See backup warnings.
- Test restore.
- Restore one document, one matter, or the vault.

Backups must be encrypted.

## Local Data Requirements

V1 should maintain:

- Matters.
- Parties.
- Documents.
- Document versions.
- OCR status and text references.
- Filing packs.
- Filing events.
- Receipts and court outputs.
- Backup snapshots.
- Audit events.
- Settings.

## Audit Requirements

Record audit events for:

- Matter creation.
- Document import.
- OCR completion/failure.
- Classification change.
- Version status change.
- Filing pack generation.
- Export.
- Receipt attachment.
- Backup.
- Restore.
- User permission change.
- Delete/archive.

The audit log should be exportable by an owner/admin.

## Privacy Requirements

Default state:

- Cloud backup off.
- No document upload.
- No telemetry containing document text.
- Local OCR preferred.
- Local search preferred.

Any external transfer must require explicit consent.

## Suggested Technical Decisions

These are recommended starting decisions, not final implementation commitments.

### Application Shell

Shortlist:

- .NET desktop if the priority is native Windows integration.
- Tauri if the priority is smaller installer size with web UI flexibility.
- Electron if the priority is fastest UI iteration and existing web skills.

Recommended decision for validation prototype:

- Use whichever stack allows the fastest clickable prototype.

Recommended decision for production MVP:

- Decide after scanner-folder, OCR, encryption, and backup proof of concept.

### Database

Recommended:

- SQLite for local metadata.
- SQLite FTS or equivalent for V1 search.

Reason:

- Simple local deployment.
- Easy backup.
- Strong enough for solo/small-firm MVP.

### Vault Storage

Recommended:

- Encrypted vault objects plus exportable matter folder structure.
- Preserve original file hashes.
- Store OCR text as sidecars or indexed text references.

### OCR

Recommended:

- Local OCR adapter.
- Background OCR queue.
- OCR confidence and failure status.

Cloud OCR should be optional and out of V1 unless a user explicitly opts in.

### Backup

Recommended:

- Encrypted snapshot backup.
- External drive and local folder targets first.
- Optional cloud provider later.

Do not treat file sync as backup unless restore and integrity verification are implemented.

## Error Handling Requirements

The system must handle:

- Corrupt files.
- Password-protected PDFs.
- Duplicate imports.
- Failed OCR.
- Missing external drive.
- Interrupted backup.
- Low disk space.
- Network path unavailable.
- Filing pack export failure.

Each failure should create a user-readable message and a diagnostic event.

## MVP Acceptance Test

The MVP passes when a solo advocate or clerk can:

1. Install the app.
2. Create a vault.
3. Configure a watched scanner folder.
4. Create a matter.
5. Import a scanned PDF.
6. OCR and search it.
7. Classify it.
8. Mark a signed or approved version.
9. Generate a filing pack.
10. Manually upload the pack to the Judiciary portal.
11. Attach a receipt.
12. Run backup.
13. Restore the matter on another Windows machine.

## Deferred Decisions

These decisions should be made after validation:

- Exact desktop framework.
- Direct scanner-driver integration.
- Whether to use raw folders or encrypted object store internally.
- Whether small-firm shared vault is in MVP or beta.
- Which cloud backup providers to support.
- Whether Local Matter RAG Connector ships as an add-on or separate installer.

## Build Readiness Checklist

Before coding begins, confirm:

- Interview findings support the MVP scope.
- Filing-pack rules are configurable.
- Data protection posture has been reviewed.
- Test documents are synthetic or redacted.
- The app has a clear no-cloud-by-default promise.
- Backup and restore are treated as core features, not later polish.

