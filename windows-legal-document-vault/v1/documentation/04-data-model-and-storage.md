# Data Model and Storage

## Storage Overview

V1 uses:

- SQLite for metadata.
- SQLite FTS for searchable text.
- Encrypted vault objects for document bytes.
- SHA-256 hashes for integrity and duplicate detection.
- Explicit export folders for filing packs and matter exports.

## Core Entities

### Matter

Fields:

- `id`
- `name`
- `internal_reference`
- `court_case_number`
- `court`
- `court_station`
- `division`
- `practice_area`
- `client_name`
- `responsible_advocate`
- `status`
- `created_at`
- `updated_at`

### Party

Fields:

- `id`
- `matter_id`
- `name`
- `role`
- `contact_details`
- `identifier_notes`

### Document

Fields:

- `id`
- `matter_id`
- `original_file_name`
- `extension`
- `vault_object_id`
- `sha256_hash`
- `byte_length`
- `document_type`
- `status`
- `imported_at`

Slice 4 stores imported file metadata directly in the `documents` table. Later versioning slices can split version-specific fields into `DocumentVersion` once multiple versions per document are implemented.

### DocumentVersion

Fields:

- `id`
- `document_id`
- `version_number`
- `status`
- `vault_object_id`
- `source_vault_object_id`
- `file_hash`
- `original_filename`
- `mime_type`
- `page_count`
- `ocr_status`
- `created_by`
- `created_at`
- `notes`

Slice 6 creates the initial version record when a document is imported. Later document amendment flows can append version 2+ records while keeping filed and served versions immutable.

### ScanInboxItem

Fields:

- `id`
- `source_path`
- `original_file_name`
- `extension`
- `sha256_hash`
- `byte_length`
- `status`
- `document_id`
- `detected_at`
- `imported_at`

Scan inbox rows represent files detected in the configured watched scan folder before they are attached to a matter. The inbox stores metadata only; document bytes remain in the scan folder until the user imports the pending scan into a matter, at which point the import service writes encrypted bytes into the vault.

### DocumentTextIndex

Fields:

- `document_id`
- `matter_id`
- `original_file_name`
- `text_content`
- `indexed_at`

Slice 7 writes local extracted text to SQLite and mirrors it into an FTS5 virtual table for matter-scoped search. The index remains local to the Windows machine and is rebuilt from encrypted vault objects when needed.

### FilingPack

Fields:

- `id`
- `matter_id`
- `filing_type`
- `status`
- `export_path`
- `readiness_status`
- `created_by`
- `created_at`
- `filed_at`

### FilingEvent

Fields:

- `id`
- `filing_pack_id`
- `event_type`
- `portal_reference`
- `receipt_document_id`
- `notes`
- `created_at`

### BackupSnapshot

Fields:

- `id`
- `target_type`
- `target_label`
- `snapshot_hash`
- `status`
- `started_at`
- `completed_at`
- `restore_tested_at`

### AuditEvent

Fields:

- `id`
- `actor`
- `event_type`
- `entity_type`
- `entity_id`
- `timestamp`
- `details`
- `hash_chain_value`

### AppSettings

Fields:

- `firm_name`
- `primary_user`
- `vault_path`
- `scan_folder_path`
- `backup_target_path`
- `setup_completed_at`
- `cloud_backup_enabled`

## Document Types

V1 document types:

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

## Document Statuses

V1 statuses:

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

Filed and served versions are immutable.

## Filing-Pack Statuses

V1 statuses:

- Draft.
- Prepared.
- Exported.
- Filed.
- Rejected.
- Corrected.
- Archived.

Prepared or exported does not mean filed.

## Vault Object Rules

- Store original imported file bytes as encrypted objects.
- Store generated artifacts as encrypted objects unless explicitly exported.
- Store file hash, size, extension, and MIME type in metadata.
- Never rely on filename alone for identity.
- Preserve original file even if OCR fails.

## Export Rules

Normal-folder exports are allowed for:

- Filing packs.
- Matter export.
- Restore test output.

Exports must warn the user that normal folders may be unencrypted.
