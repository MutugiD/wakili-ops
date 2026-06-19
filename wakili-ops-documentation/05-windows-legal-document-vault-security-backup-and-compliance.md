# Windows Legal Document Vault Security, Backup, and Compliance

## Security Posture

Windows Legal Document Vault must be local-first, privacy-by-default, and recovery-oriented.

Law firm documents may contain privileged information, client instructions, identity documents, medical information, financial records, family details, land records, criminal allegations, employment records, business records, and court materials.

The product must assume documents are confidential unless the user explicitly exports or backs them up externally.

## Kenya Data Protection Context

The ODPC identifies the Data Protection Act, 2019 and its regulations as Kenya's data protection regulatory framework. Source: [ODPC Data Protection Laws Kenya](https://www.odpc.go.ke/data-protection-laws-kenya/).

The Data Protection Act includes principles and obligations around:

- Lawful, fair, and transparent processing.
- Explicit, specified, and legitimate purposes.
- Data minimization.
- Accuracy.
- Storage limitation.
- Restrictions on transfers outside Kenya unless safeguards or consent exist.
- Technical and organizational safeguards.
- Encryption and restoration capability.
- Breach notification obligations in certain cases.

Source: [Kenya Data Protection Act](https://new.kenyalaw.org/akn/ke/act/2019/24/eng%402022-12-31).

The ODPC handbook explains personal data, sensitive personal data, controller/processor roles, rights of data subjects, and compliance measures such as safeguards, breach reporting, and localization/serving-copy considerations. Source: [ODPC Personal Data Protection Handbook](https://www.odpc.go.ke/wp-content/uploads/2024/02/PERSONAL-DATA-PROTECTION-HANDBOOK.pdf).

## Product Compliance Position

This product should support compliance but cannot by itself make a firm compliant.

The DMS should help firms:

- Know where documents are.
- Restrict access.
- Encrypt stored documents.
- Preserve audit trails.
- Back up and restore.
- Apply retention policies.
- Export data when required.
- Record breach facts and remedial action.
- Avoid unnecessary cloud transfer.

The firm remains responsible for professional and legal compliance.

## Local-First Default

### Default Rule

No matter document leaves the user's device, external drive, or configured office storage unless the user explicitly exports it or enables encrypted backup/sync.

### Why

Small Kenyan law firms may be cautious about cloud storage because legal files can include confidential and personal data. Local-first design builds trust and reduces unnecessary transfer.

### User-Facing Language

"Your documents stay on your machine by default. Cloud backup is optional and encrypted before upload."

## Encryption Model

### At Rest

Encrypt:

- Vault files.
- Backup snapshots.
- Sensitive metadata where practical.
- Recovery exports.

### In Transit

If cloud backup is enabled:

- Encrypt before upload.
- Use TLS transport.
- Do not upload raw unencrypted files.

### Key Management

V1 should support:

- Firm recovery key.
- Optional Windows account binding.
- Recovery key export.
- Recovery warning if key not saved.

Risk: If the user loses the only recovery key, encrypted backups may be unrecoverable.

## Access Control

### Solo Mode

- Windows user unlock.
- Optional app PIN.
- Vault owner account.

### Small Firm Mode

Minimum roles:

- Owner/Admin.
- Advocate.
- Clerk/Assistant.
- Read-only.

Role examples:

- Admin can configure backup and users.
- Advocate can approve filing packs and mark filed.
- Clerk can import scans and prepare packs.
- Read-only can view and search.

### Sensitive Matter Flag

Some matters should be marked sensitive:

- Criminal.
- Family.
- Children.
- Health.
- High-profile clients.
- Protected persons.

Sensitive matters should require stricter access and clearer export warnings.

## Audit Log

The audit log should record:

- Login/open vault.
- Matter creation.
- Document import.
- Document view where feasible.
- Export.
- Filing pack generation.
- Receipt attachment.
- Status change.
- Backup.
- Restore.
- Delete/archive.
- Permission change.

Audit events should be tamper-evident for normal users.

## Backup Strategy

## Backup Principles

- Backups must be encrypted.
- Backups must be verifiable.
- Backups must be restorable.
- Backup status must be visible.
- Cloud is optional.
- Backup does not equal filing.

## Backup Targets

### Same-PC Snapshot

Use for short-term protection against accidental deletion.

Not enough for hardware failure.

### External Drive

Recommended for solo advocates.

The tray app should remind users to connect the drive.

### Office NAS or Shared Folder

Recommended for small firms that already have shared storage.

Requires network reliability checks.

### Optional Cloud Backup

Allowed only after explicit opt-in.

Requirements:

- Client-side encryption.
- Clear provider selection.
- Retention policy.
- Restore test.
- Warning about cross-border transfer if applicable.
- Serving-copy/local copy retained where appropriate.

## Restore Requirements

The product must support:

- Restore entire vault.
- Restore one matter.
- Restore one document version.
- Test restore without overwriting live vault.
- Restore report.

## Retention

The DMS should allow firm-configurable retention policies.

Examples:

- Active matters retained indefinitely until archived.
- Closed matters retained according to firm policy.
- Deleted documents soft-deleted for a retention window.
- Filing receipts and court outputs retained with matter.

No default automatic destruction should occur without explicit configuration.

## Breach and Incident Support

The system should help record:

- Date/time detected.
- Affected matter(s).
- Affected document(s).
- User accounts involved.
- Backup state.
- Whether encrypted data was exposed.
- Remedial actions.
- Exportable incident report.

This supports the firm's compliance workflow if a reportable breach occurs.

## Data Export and Portability

The firm should be able to export:

- A full matter folder.
- A document and all versions.
- A filing pack.
- Audit log for a matter.
- Backup manifest.
- OCR text and metadata.

Exports should be clearly marked as unencrypted if exported to normal folders.

## Cloud Backup Consent

Before enabling cloud backup, the app should display:

- What will be backed up.
- Whether files are encrypted before upload.
- Where the provider may store data.
- Who controls the recovery key.
- What happens if the key is lost.
- How to disable backup.
- How to delete cloud snapshots.

## Filing-Pack Security

Filing packs are exported into normal folders for manual upload. This is useful but creates risk.

Controls:

- Warn that exported packs may be unencrypted.
- Offer auto-cleanup after filing.
- Track export location.
- Attach receipt before closing pack.
- Warn if pack remains on Desktop/Downloads for too long.

## Security Acceptance Criteria

The product is acceptable when:

- A new vault is encrypted.
- A backup snapshot is encrypted.
- A restore can be tested.
- A document export creates an audit event.
- Cloud backup is disabled by default.
- Cloud backup cannot start without consent.
- Filing pack export warns about unencrypted output.
- User can see last backup and last restore test.
- User can export a matter without vendor lock-in.

## Compliance Caveat

This product documentation is not legal advice. It describes product controls aligned to privacy, security, and operational needs. Firms should obtain professional advice on Data Protection Act registration, controller/processor obligations, retention duties, breach reporting, and professional conduct obligations.
