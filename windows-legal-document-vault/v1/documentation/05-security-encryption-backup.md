# Security, Encryption, and Backup

## Security Promise

The Windows Legal Document Vault keeps documents local by default. No matter document leaves the user's configured device, external drive, or office storage unless the user explicitly exports it or enables a future backup/sync option.

## Encryption Requirements

V1 must encrypt:

- Vault document objects.
- Backup snapshots.
- Sensitive recovery exports.

V1 should protect:

- Recovery key material.
- Vault metadata where practical.
- Export operations with warnings and audit logs.

## Recovery Key

During setup, the app must:

- Generate or capture recovery-key material.
- Tell the user that losing the recovery key can make encrypted backups unrecoverable.
- Ask the user to confirm that the recovery key has been saved.
- Record recovery-key setup status without storing the raw recovery phrase in plain text.

## Unlock Behavior

The vault can be unlocked by:

- Windows user context plus app secret where supported.
- Recovery key when restoring or moving machines.

Wrong key behavior:

- Fail safely.
- Do not corrupt the vault.
- Record a diagnostic event.
- Avoid revealing whether a specific document exists.

## Audit Events

Security-relevant audit events:

- Vault created.
- Vault unlocked.
- Failed unlock.
- Document imported.
- Document exported.
- Filing pack exported.
- Backup created.
- Restore test performed.
- Matter restored.
- Permission/settings changed.
- Document archived or deleted.

## Backup Requirements

V1 backup targets:

- Local folder.
- External drive.
- Office shared folder/NAS path if configured.

Cloud backup is an optional paid add-on. It must be opt-in, client-side encrypted, and separate from the local vault. The local vault remains the primary source of truth.

Backup snapshots must:

- Be encrypted before writing.
- Include manifest.
- Include checksums.
- Record app version.
- Record source vault ID.
- Support restore verification.

## Restore Requirements

V1 restore must support:

- Test restore into temporary location.
- Restore one matter.
- Restore full vault.
- Restore report.

Restore must not overwrite the live vault without explicit user confirmation.

## Optional Cloud Backup Add-On

Cloud backup must work as a plug-and-play option for firms that want off-device recovery.

Requirements:

- Disabled by default.
- Requires explicit user consent.
- Requires active license/add-on entitlement.
- Encrypts snapshots locally before upload.
- Uploads encrypted snapshots only.
- Does not upload recovery key.
- Does not upload raw document contents, filenames, matter names, OCR text, or client details.
- Supports backup health status in the local app.
- Supports restore to a new Windows machine with the user's recovery key.

Supported provider strategy:

- V1 should define a provider interface, not hard-code one cloud vendor deeply into the product.
- First implementation can support one managed provider selected by the product owner.
- Later versions can add S3-compatible storage, Azure Blob, Google Cloud Storage, or firm-owned storage.

Cloud backup metadata allowed:

- Installation ID.
- Backup snapshot ID.
- Snapshot size.
- Snapshot hash.
- Created timestamp.
- App version.
- Backup status.

Cloud backup metadata not allowed:

- Matter names.
- Party names.
- Client names.
- Case numbers.
- Document titles.
- OCR text.
- Plain file paths.

## Export Warnings

Warn before:

- Filing-pack export.
- Matter export.
- Document export.
- Restore to normal folder.

Warning language should say that exported files may be readable outside the encrypted vault.

## Security Acceptance Criteria

V1 passes security acceptance when:

- A vault object cannot be read as plain text from disk.
- Backup snapshot cannot be read as plain text from disk.
- Wrong recovery key fails safely.
- Export creates an audit event.
- Cloud backup is off and unavailable by default.
- Optional cloud backup uploads only encrypted snapshots.
- Restore test succeeds without damaging live data.
