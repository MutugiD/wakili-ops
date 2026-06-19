# Windows Legal Document Vault: Windows Plug-and-Play Document Management and Backup

## Product Definition

Windows Legal Document Vault is a Windows local-first document management system for Kenyan solo advocates and small firms.

It is installed as a normal Windows application with a tray service. It manages local matter files, scans, OCR, classification, version history, filing-pack preparation, audit logs, and encrypted backup.

The product's promise:

"Own your legal documents locally. Scan, organize, search, version, prepare, file, and back up every matter file without turning cloud storage into the primary source of truth."

## Target Users

### Primary Users

- Solo advocates.
- Small firms with 1 to 10 advocates.
- Advocate clerks and legal assistants.
- Firms with Windows PCs and ordinary scanner/printer workflows.

### Non-Primary Users for V1

- Large law firms with existing enterprise document systems.
- Judiciary staff.
- Public litigant self-service portals.
- Full practice management suites with billing and accounting as first-class modules.

## User Installation Experience

The first version should feel like a practical Windows utility, not an enterprise rollout.

### First Run

The user installs the app, opens it, and sees a setup wizard:

1. Choose vault location:
   - This PC.
   - External drive.
   - Office shared folder/NAS path.
2. Create firm profile:
   - Firm name.
   - Advocate name(s).
   - Email.
   - Phone.
   - Postal/physical address.
   - Optional P105/LSK identifiers.
3. Set security:
   - Windows account unlock.
   - Optional app PIN.
   - Recovery phrase or recovery key export.
4. Choose backup:
   - Local backup folder.
   - External drive.
   - Optional encrypted cloud backup later.
5. Create first matter or import an existing folder.

### Tray App

The tray app should provide:

- Scan inbox status.
- Backup health.
- Recent imports.
- Quick search.
- Open vault.
- Pause OCR.
- Manual backup now.
- Restore test reminder.

## Core Modules

## 1. Matter Vault

The Matter Vault is the local source of truth.

Each matter contains:

- Matter name.
- Internal matter number.
- Court case number, if available.
- Court and station.
- Division or registry.
- Parties.
- Client.
- Practice area.
- Responsible advocate.
- Clerk or assistant.
- Key dates.
- Linked filing packs.
- Linked receipts, orders, rulings, and notices.

### Matter Folder Model

The user should not need to understand the internal structure, but the vault should preserve an exportable logical structure:

```text
Matter Name/
  01-Instructions/
  02-Drafts/
  03-Pleadings/
  04-Affidavits/
  05-Annexures/
  06-Authorities/
  07-Court-Outputs/
  08-Receipts-and-Payments/
  09-Service/
  10-Filing-Packs/
  99-Archive/
```

The app may store files in an encrypted internal vault rather than raw folders, but exports should use this structure.

## 2. Scan Inbox

The Scan Inbox is the landing area for new paper-to-digital material.

Input sources:

- Scanner import.
- Watched scanner folder.
- Drag-and-drop files.
- Email attachment import later.
- Mobile photo import later.
- Existing folder import.

Supported file types:

- PDF.
- JPG/JPEG.
- PNG.
- TIFF.
- DOCX.
- TXT.
- CSV or XLSX as evidence/supporting material, marked as non-filing-native unless converted.

### Scan Inbox Workflow

1. File arrives in inbox.
2. App detects file type.
3. App checks for corruption, password protection, and malware scan status.
4. App runs OCR if needed.
5. App proposes matter match.
6. App proposes document type.
7. User confirms or edits.
8. App moves the document into the matter vault.

## 3. OCR and Classification

OCR converts scans into searchable documents.

Classification suggests:

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
- Contract.
- Land document.
- ID document.
- Unknown.

Classification must remain assistive. The user confirms final classification.

### OCR Requirements

- Local-first OCR where possible.
- Offline OCR for common document types.
- Searchable PDF output.
- OCR text stored separately for indexing.
- OCR confidence score.
- Page-level OCR status.
- Manual "mark unreadable" status.

## 4. Version Sessions

Version sessions track document life cycle.

Statuses:

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

### Version Rules

- Never overwrite a filed or served version.
- Creating an amended version preserves the filed version.
- Exporting to a filing pack creates a snapshot.
- Changing metadata creates an audit event.
- Deleting a document soft-deletes first and requires permission.

## 5. Filing-Pack Builder

The Filing-Pack Builder prepares a folder or zip of documents for manual upload to the official e-filing portal.

It does not log into the portal and does not file automatically in V1.

### Filing-Pack Contents

- Filing pack summary.
- Document index.
- Selected PDFs.
- Size report.
- OCR/readability report.
- Signature status.
- Court/matter metadata.
- Manual upload checklist.
- Post-filing receipt checklist.

### Readiness Checks

The app should check:

- PDF format.
- Separate file per document where required.
- File size threshold.
- Whether file is encrypted/password-protected.
- Malware scan status.
- Page count.
- 300 DPI where detectable for scanned annexures.
- Readability/OCR confidence.
- Missing signature markers where expected.
- Blank page detection.
- Duplicate pages or duplicate files.
- Naming convention.
- Whether the selected document is the current approved version.

### File Size Policy

Official and public guidance can vary by court or guide. Some practice direction excerpts refer to 25 MB per upload; other public guides mention 50 MB. V1 should default to the stricter 25 MB limit and allow firm or court profile overrides.

The documentation and UI should say:

"Confirm current size limits on the live Judiciary portal or applicable practice directions. This tool flags likely filing risks before upload."

## 6. Backup Manager

Backup Manager protects local ownership.

Backup types:

- Local snapshot on same PC.
- External drive backup.
- Office NAS backup.
- Optional encrypted cloud backup.

### Backup Rules

- Cloud backup is opt-in.
- Cloud backup stores encrypted snapshots only.
- Encryption occurs before upload.
- The user controls the recovery key.
- Backup health is visible in the tray.
- The app prompts for restore drills.

### Backup Statuses

- Protected.
- Backup overdue.
- External drive missing.
- Cloud backup paused.
- Last backup failed.
- Restore test overdue.
- Recovery key missing.

## 7. Audit Log

The audit log records:

- Matter created.
- Document imported.
- OCR run.
- Classification changed.
- Version created.
- Filing pack generated.
- Document exported.
- Receipt attached.
- Backup completed.
- Restore performed.
- User access.
- Delete or archive action.

Audit logs should be tamper-evident for normal users, but exportable for firm owners.

## 8. Receipts and Court Outputs

The system must treat court outputs as first-class matter records.

Court output types:

- Filing receipt.
- Payment receipt.
- Notice of electronic filing.
- Notice of service.
- Court order.
- Ruling.
- Judgment.
- Registry notice.
- Rejection or correction request.
- Cause-list entry.
- Hearing link.
- Payment status snapshot.
- Order validation result.
- Receipt validation result.

These should be easy to attach immediately after portal filing.

## Product Screens

### Home

- Recent matters.
- Scan inbox.
- Backup status.
- Filing packs awaiting receipt.
- Recently filed matters.
- Search bar.

### Matter Detail

- Timeline.
- Documents.
- Filing packs.
- Court outputs.
- Parties.
- Tasks.
- Notes.
- Audit log.

### Scan Inbox

- Incoming files.
- OCR status.
- Suggested matter.
- Suggested document type.
- Warnings.
- Confirm import action.

### Filing Pack

- Selected documents.
- Readiness status.
- Warnings.
- Index preview.
- Export pack button.
- Manual e-filing checklist.
- Post-filing receipt capture.

### Backup Center

- Last backup.
- Backup targets.
- Encryption status.
- Restore test.
- Recovery key status.

## MVP Scope

The MVP includes:

- Windows installer.
- Local vault.
- Matter creation.
- File import.
- Watched folder import.
- OCR for PDFs/images.
- Search by matter and document text.
- Manual classification.
- Version statuses.
- Filing-pack builder.
- Local/external encrypted backup.
- Audit log.
- Receipt/court-output attachment.

## Explicitly Out of Scope for MVP

- Direct e-filing portal automation.
- Billing and trust accounting.
- Full case management.
- Enterprise SSO.
- Multi-office replication.
- Cloud-first document storage.
- Automated legal advice.
- Automatic filing without human review.

## Product Acceptance Criteria

The MVP is acceptable when a solo advocate can:

1. Install the app on Windows.
2. Create a matter.
3. Scan or import a document.
4. OCR it.
5. Classify it.
6. Search for it.
7. Create a signed/filed version record.
8. Generate a filing pack.
9. Manually upload files to e-filing.
10. Attach the resulting receipt and court notice.
11. Back up the vault.
12. Restore the vault on another Windows machine.
