# Testing and Installation Strategy

## Testing Layers

V1 testing uses four layers:

- Unit tests.
- Integration tests.
- Filesystem/security tests.
- Manual Windows acceptance tests.

## Unit Tests

Unit tests cover:

- Entity validation.
- Status transitions.
- Filing-pack status rules.
- Supported file type checks.
- Readiness-check logic.
- Error/result handling.

## Integration Tests

Integration tests cover:

- SQLite repository round trips.
- SQLite migrations.
- Encrypted vault object write/read.
- File import into vault.
- Search indexing.
- Backup manifest creation.

Tests should use temporary folders and synthetic documents.

## Filesystem and Security Tests

Tests must prove:

- Vault objects are not plain text.
- Backup snapshots are not plain text.
- File hashes remain stable.
- Wrong recovery key fails.
- Restore does not overwrite live vault without explicit command.

## Manual Acceptance Tests

Manual tests must cover:

- First-run setup.
- Create matter.
- Import PDF.
- Import file through watched scan folder.
- Classify document.
- Add version.
- Generate filing pack.
- Attach receipt.
- Run backup.
- Test restore.
- App restart.
- Offline operation.

## Test Documents

Use only synthetic or redacted documents.

Recommended test set:

- Text PDF.
- Image-only PDF.
- JPG scan.
- PNG scan.
- DOCX draft.
- Large PDF near file-size warning threshold.
- Password-protected PDF.
- Corrupt PDF.
- Duplicate PDF.

## Installation Tests

V1 install verification:

- Install on Windows 10 64-bit.
- Install on Windows 11 64-bit.
- Launch app without admin-only runtime assumptions.
- Create local vault.
- Restore sample backup.
- Uninstall without deleting user vault unless user explicitly chooses.

## Done Criteria

The MVP is installation-ready when:

- Automated tests pass.
- Manual acceptance flow passes on developer machine.
- Cross-machine restore passes.
- Installer smoke test passes.
- Documentation matches implemented behavior.

