# End-to-End Document Testing

## Purpose

This document defines how to test the Windows Legal Document Vault with realistic legal-office documents before each release.

The test suite must prove the system can handle Word drafts, text PDFs, scanned PDFs, images, duplicate documents, invalid documents, and filing-pack readiness data without leaking document contents outside the local machine.

## Test Data Rule

Use only:

- Synthetic documents.
- Redacted documents.
- Public sample documents.

Never use live client material in automated or demo tests.

## Online Sample Documents

The installed-app interactive test downloads public sample files at runtime so the workflow proves it can handle real external documents, not only files generated inside the test harness.

Current sources:

- DOCX sample: `https://raw.githubusercontent.com/rounakdatta/CorrectLy/master/sample.docx`
- PDF sample: `https://ontheline.trincoll.edu/images/bookdown/sample-local-pdf.pdf`

Run:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage
```

Expected workflow coverage:

- Installed `.exe` starts from `%LOCALAPPDATA%\Programs\WindowsLegalDocumentVault`.
- First-run setup is completed through WPF controls.
- The online DOCX is imported, encrypted, indexed, and searched.
- The online PDF is copied into the watched scan folder, queued, and imported through Scan Inbox.
- The matter is exported as a filing pack.
- Backup snapshot and restore drill complete.
- Local backup snapshot list refreshes.
- Backup health updates after local backup.
- Selected local backup restore workspace is verified.
- Copied external backup folder restore verification completes.
- Restore verification report files are created for local, external, and cloud restore checks.
- Cloud backup uploads an encrypted package to the local provider adapter.
- Backup health updates after cloud backup.
- Cloud restore drill verifies the selected cloud snapshot.

## Recommended Test Folder

Use a local test folder outside the app source:

```text
D:\commercial\Wakili-OPs-test-data\windows-legal-document-vault\v1
```

Suggested structure:

```text
test-data/
  valid/
    plaint.docx
    plaint-exported.pdf
    verifying-affidavit.pdf
    annexure-a-scan.pdf
    court-order.pdf
    payment-receipt.pdf
  images/
    id-copy.jpg
    land-map.png
  invalid/
    corrupt.pdf
    password-protected.pdf
    unsupported.exe
  duplicates/
    plaint-copy-1.pdf
    plaint-copy-2.pdf
  large/
    large-annexure.pdf
```

## Document Types to Test

### Word Documents

Use `.docx` files for:

- Draft plaint.
- Draft defence.
- Draft affidavit.
- Draft submissions.
- Client letter.

Expected V1 behavior:

- Import accepted as a supported document type.
- Original bytes stored in encrypted vault.
- Metadata records original filename and hash.
- Filing-pack readiness later marks DOCX as non-filing-native until converted to PDF.

### Text PDFs

Use PDFs exported from Word.

Expected behavior:

- Import accepted.
- Hash recorded.
- Duplicate detection works.
- Later OCR/search slice should extract embedded text.

### Scanned PDFs

Use image-only PDFs.

Expected behavior:

- Import accepted.
- File is preserved exactly.
- OCR status starts as pending until OCR slice is active.
- Filing readiness later checks page count, size, readability, and scan quality where possible.

### Image Files

Use `.jpg`, `.jpeg`, `.png`, and `.tiff`.

Expected behavior:

- Import accepted where supported.
- Stored as evidence/supporting material.
- Filing-pack readiness later requires PDF conversion for court filing.

### Receipts and Court Outputs

Use synthetic:

- Filing receipt PDF.
- Payment receipt PDF.
- Court order PDF.
- Registry notice PDF.
- Rejection/correction request PDF.

Expected behavior:

- Import accepted.
- Classified as court output or receipt once classification slice is active.
- Linked to matter or filing pack once receipt-capture slice is active.

## Core Test Scenarios

## Automated End-to-End Workflow

Run the current automated Windows workflow from the V1 app root:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1
```

For local release confidence, run with a visible WPF launch and package smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1 -Visible -IncludePackageSmoke
```

Current automated E2E coverage:

- First-run style settings creation.
- Installation ID persistence.
- Encrypted vault creation.
- SQLite matter creation.
- Watched scan folder queueing and duplicate detection.
- DOCX scan import into a matter.
- PDF document import into the same matter.
- Classification update.
- Initial version metadata.
- DOCX and text-like PDF indexing.
- Matter-scoped search.
- Filing-pack export.
- Receipt/court-output capture.
- Encrypted backup snapshot.
- Restore drill.
- Admin registry check-in and disable operation.

Current automated edge coverage:

- Unsupported and empty files are ignored/rejected.
- Unsupported import file types are rejected.
- Wrong vault recovery key fails.
- Filed/served classifications are immutable.
- Court-output capture rejects non-output document types.
- Backup target inside the vault is rejected.
- Restore targets that could delete the backup are rejected.
- Tampered backup artifacts fail restore hash validation.
- Admin registry delete does not delete local vault data.

### Scenario 1: New Matter With Word Draft and PDF Filing Copy

1. Create matter.
2. Import `plaint.docx`.
3. Import `plaint-exported.pdf`.
4. Confirm both files are attached to the same matter.
5. Confirm each file has a unique hash.

Pass criteria:

- Both files persist after app restart.
- Original bytes can be read back from encrypted vault.

### Scenario 2: Scanned Affidavit

1. Import `verifying-affidavit.pdf`.
2. Confirm document is stored.
3. Confirm OCR status is pending or not processed until OCR module exists.

Pass criteria:

- Scan remains accessible.
- Import does not require internet.

### Scenario 3: Duplicate Detection

1. Import `plaint-copy-1.pdf`.
2. Import identical `plaint-copy-2.pdf`.

Pass criteria:

- Second import is flagged as duplicate by hash.
- User is not forced to store duplicate bytes.

### Scenario 4: Unsupported File

1. Attempt to import `unsupported.exe`.

Pass criteria:

- Import is rejected.
- User sees clear message.
- No vault object is created.

### Scenario 5: Corrupt PDF

1. Attempt to import `corrupt.pdf`.

Pass criteria:

- App records import failure.
- App does not crash.
- No partial document record remains as successful import.

### Scenario 6: Filing-Pack Readiness

1. Select approved PDF documents.
2. Generate filing-pack readiness report.

Pass criteria:

- Report includes file size, type, hash, selected version, and warnings.
- DOCX files are flagged as not directly filing-ready.
- Password-protected PDFs are flagged.

## Schemas

### Document Metadata Schema

```json
{
  "documentId": "guid",
  "matterId": "guid",
  "title": "Plaint",
  "documentType": "Pleading",
  "currentVersionId": "guid",
  "status": "Imported",
  "createdAt": "2026-06-19T20:00:00Z",
  "updatedAt": "2026-06-19T20:00:00Z"
}
```

### Document Version Schema

```json
{
  "documentVersionId": "guid",
  "documentId": "guid",
  "versionNumber": 1,
  "status": "Imported",
  "vaultObjectId": "object-id",
  "originalFilename": "plaint-exported.pdf",
  "mimeType": "application/pdf",
  "sha256Hash": "HEX_HASH",
  "fileSizeBytes": 123456,
  "pageCount": 12,
  "ocrStatus": "Pending",
  "createdAt": "2026-06-19T20:00:00Z"
}
```

### Import Report Schema

```json
{
  "importId": "guid",
  "matterId": "guid",
  "sourcePath": "D:\\test-data\\valid\\plaint-exported.pdf",
  "status": "Imported",
  "warnings": [],
  "errors": [],
  "duplicateOfDocumentVersionId": null,
  "createdVaultObjectId": "object-id"
}
```

### Filing Readiness Report Schema

```json
{
  "filingPackId": "guid",
  "matterId": "guid",
  "status": "Prepared",
  "checks": [
    {
      "documentVersionId": "guid",
      "check": "PdfFormat",
      "severity": "Pass",
      "message": "Document is a PDF."
    },
    {
      "documentVersionId": "guid",
      "check": "FileSize",
      "severity": "Warning",
      "message": "Confirm current portal size limits before upload."
    }
  ]
}
```

## Release Gate

Before a release is considered usable:

- All automated tests pass.
- Manual import tests pass for DOCX, PDF, scanned PDF, JPG, PNG, duplicate file, corrupt PDF, password-protected PDF, and unsupported file.
- No test uploads raw document contents to cloud or admin dashboard.
- Local vault remains usable offline.
