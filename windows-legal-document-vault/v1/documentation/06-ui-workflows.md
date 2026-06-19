# UI Workflows

## UI Principle

The app should feel like a dependable Windows office utility. It should prioritize clarity, speed, and confidence over decorative layout.

## Setup Wizard

Purpose:

- Complete first-run configuration.

Steps:

1. Firm profile.
2. Vault location.
3. Watched scan folder.
4. Backup target.
5. Recovery key.
6. Review and create vault.

Empty/error states:

- Missing required field.
- Invalid folder path.
- Folder not writable.
- Backup target unavailable.
- Recovery key not confirmed.

## Home

Purpose:

- Show immediate operational status.

Content:

- Recent matters.
- Scan inbox count.
- Backup status.
- Filing packs awaiting receipt.
- Search bar.

Primary actions:

- New matter.
- Import document.
- Open scan inbox.
- Run backup.

## Matters

Purpose:

- List and find matters.

Content:

- Matter name.
- Internal reference.
- Court case number.
- Client.
- Status.
- Updated date.

Primary actions:

- Create matter.
- Open matter.
- Search/filter matters.

## Matter Detail

Purpose:

- Operate from a matter-specific workspace.

Tabs:

- Timeline.
- Documents.
- Filing packs.
- Court outputs.
- Parties.
- Audit log.

Primary actions:

- Import document.
- Create filing pack.
- Attach court output.
- Search within matter.

## Scan Inbox

Purpose:

- Review incoming files from watched folders.

Content:

- File name.
- Source path.
- Import status.
- Suggested matter.
- Suggested document type.
- Warnings.

Primary actions:

- Assign matter.
- Confirm import.
- Skip file.
- View import report.

## Document Detail

Purpose:

- Show document metadata, versions, OCR status, and actions.

Content:

- Title.
- Type.
- Current status.
- Versions.
- Hash.
- OCR status.
- Linked filing packs.

Primary actions:

- Change classification.
- Add version.
- Mark approved/signed/filed/served.
- Export document.

## Filing Pack

Purpose:

- Prepare a manual e-filing export.

Content:

- Selected documents.
- Readiness checks.
- Warnings.
- Generated index preview.
- Export location.

Primary actions:

- Add/remove documents.
- Run checks.
- Export pack.
- Attach receipt.
- Mark filed after confirmation.

## Backup Center

Purpose:

- Show backup and restore health.

Content:

- Last backup.
- Backup target.
- Last restore test.
- Snapshot list.
- Warnings.

Primary actions:

- Run backup now.
- Test restore.
- Restore matter.
- Change backup target.

## Settings

Purpose:

- Manage firm profile, paths, backup target, and recovery settings.

Settings changes that affect vault, backup, or security must create audit events.

