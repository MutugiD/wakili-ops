# Phase 1 Delivery Plan: Windows Legal Document Vault

This document tracks Phase 1 as the first shipped product in Wakili Ops. It is narrower than the strategic research pack and stays close to the V1 implementation.

## Product Definition

Windows Legal Document Vault is a Windows local-first legal document management app for Kenyan advocates and small firms.

The app solves firm-owned document custody:

- Import scanned and drafted legal documents.
- Store original bytes in an encrypted local vault.
- Organize by matter.
- Track document type, status, and version history.
- Search matter documents locally.
- Export filing packs for manual court e-filing.
- Capture receipts and court outputs.
- Create backups and prove that they restore.
- Optionally upload a second encrypted backup package to a configured provider.

## Users

Primary users:

- Solo advocates.
- Small law firms.
- Legal clerks handling scanning and filing.
- Firm owner/admin responsible for continuity and backup discipline.

Secondary users:

- IT support setting up a machine.
- Product owner reviewing installation health.

## V1 User Outcomes

The user should be able to:

- Install the app locally on Windows.
- Complete first-run setup.
- Create a matter.
- Import a DOCX or PDF.
- Import scanner-folder documents.
- Classify a document.
- Search imported text.
- Export a filing pack.
- Capture a filing receipt or court output.
- Run a local encrypted backup.
- Run a restore drill without overwriting the live vault.
- Refresh local backup snapshots and verify a selected backup into a restore workspace.
- Verify a backup folder copied from another machine into a separate restore workspace.
- Produce a restore verification report for local, external, and cloud restore checks.
- See backup health and last backup timestamps in the app.
- Delete selected local and cloud backup snapshots without touching the live vault.
- Preview and apply local backup retention cleanup.
- Enable the local provider cloud-backup option.
- Upload an encrypted backup package.
- Select a cloud backup snapshot and verify restore.

## Implementation Modules

### App Shell

Status: implemented.

Owns:

- WPF application startup.
- Main window.
- View model command orchestration.
- Local path overrides for testing.

Acceptance:

- App starts from source.
- App starts from installed package.
- Startup smoke can launch and stop the app.

### Setup And Settings

Status: implemented.

Owns:

- Firm name.
- Primary user.
- Device nickname.
- License key.
- Vault path.
- Scan folder path.
- Backup target path.
- Recovery-key confirmation.
- Installation ID creation.

Acceptance:

- Required fields are validated.
- Settings persist.
- Setup does not enable cloud backup automatically.

### Encrypted Vault

Status: implemented.

Owns:

- Local encrypted document object storage.
- Vault manifest.
- Recovery-key based encryption.
- Wrong-key rejection.

Acceptance:

- Stored object bytes are unreadable as plain text.
- Original bytes round trip with the correct recovery key.
- Wrong recovery key fails safely.

### Matter And Document Management

Status: implemented.

Owns:

- Matters.
- Document metadata.
- Version metadata.
- Document type and status.
- Immutability for filed and served documents.

Acceptance:

- Matter persists across app restarts.
- Document import records metadata.
- Classification updates persist.
- Filed/served statuses cannot be overwritten through ordinary classification.

### Scan Inbox

Status: implemented.

Owns:

- Watched-folder refresh.
- Pending scan queue.
- Import pending scan to selected matter.

Acceptance:

- Supported files are queued.
- Unsupported files are ignored.
- Duplicate pending scans are detected.
- Selected scan imports into the selected matter.

### OCR And Search

Status: implemented for DOCX and text-like PDF extraction.

Owns:

- Text extraction.
- Local search indexing.
- Matter-scoped search results.

Acceptance:

- DOCX text can be indexed and searched.
- Text-like PDF content can be indexed and searched.
- Results include document source context.

### Filing Pack

Status: implemented.

Owns:

- Export selected matter documents.
- Generate an index and checklist.
- Warn that exported filing-pack folders are not encrypted.

Acceptance:

- Exported copies match selected vault documents.
- Manifest exists.
- Readiness file exists.

### Receipt And Court Output Capture

Status: implemented.

Owns:

- Filing receipts.
- Payment receipts.
- Notices.
- Orders.
- Rulings.
- Judgments.

Acceptance:

- Court output is stored under the selected matter.
- Non-output document types are rejected by the court-output capture service.

### Backup And Restore

Status: implemented.

Owns:

- Encrypted local backup snapshot.
- Backup manifest.
- Hash verification.
- Non-destructive restore drill.
- Local backup snapshot catalog.
- Selected local backup restore workspace.
- External backup folder restore verification.
- Privacy-preserving restore verification reports.
- Backup health summary.
- Backup snapshot cleanup controls.
- Local backup retention policy automation.

Acceptance:

- Backup creates encrypted vault and database artifacts.
- Restore drill verifies every manifest entry.
- Restore drill rejects unsafe targets.
- Tampered backup hashes fail.
- Local backup list shows valid snapshots.
- Selected local backup can be verified into a restore workspace.
- Copied external backup folders can be verified without the original backup target.
- Restore workspaces include a report that excludes matter names, document text, and recovery-key values.
- Backup health flags missing or stale local backups and confirms when local/cloud snapshots are available.
- Deleting selected backup snapshots does not delete the live vault.
- Retention cleanup keeps the newest configured snapshots and deletes only valid local backup snapshots.

### Optional Cloud Backup

Status: local provider adapter and app UI implemented.

Owns:

- Provider interface.
- Encrypted package creation.
- Local filesystem provider adapter.
- Snapshot list.
- UI upload.
- UI restore drill from selected cloud snapshot.

Acceptance:

- Upload requires cloud backup to be enabled.
- Upload requires active/trial local access.
- Metadata excludes document and matter details.
- Package bytes do not expose document text or filenames.
- Downloaded package can be restored and verified.

### Admin Install Control

Status: local owner admin registry implemented.

Owns:

- Installation ID.
- License state.
- Enable/disable/delete registry operations.
- Sanitized check-in payload model.

Skipped by instruction:

- Hosted admin/payment entitlement integration.

Acceptance:

- Admin delete does not delete local vault data.
- Check-in payload excludes matter names, document names, case numbers, OCR text, recovery keys, and local paths.

## Test Strategy

Required before merge:

- `dotnet format WakiliDms.sln --verify-no-changes --verbosity minimal`
- `dotnet build WakiliDms.sln --configuration Release`
- `dotnet run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj --configuration Release`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1 -Visible -IncludePackageSmoke`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage`
- `dotnet list WakiliDms.sln package --include-transitive --vulnerable`
- Gitleaks secret scan.
- Markdown link validation.
- Legacy numbered-product label validation.

## Current Next Features

The next implementation slices should remain close to document safety and recovery:

1. Production cloud provider adapter after provider choice.
2. Backup cleanup confirmation prompts.
3. UI navigation polish after workflows stabilize.
4. Phase 2 export contract for Local Matter RAG Connector.
