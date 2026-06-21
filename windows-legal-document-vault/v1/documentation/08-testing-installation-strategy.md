# Testing and Installation Strategy

## Testing Layers

V1 testing uses four layers:

- Unit tests.
- Integration tests.
- Filesystem/security tests.
- Automated end-to-end Windows workflow tests.
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
- Full matter workflow across setup, vault, scan inbox, import, search, export, court-output capture, backup, restore drill, and admin registry.

Tests should use temporary folders and synthetic documents.

## Filesystem and Security Tests

Tests must prove:

- Vault objects are not plain text.
- Backup snapshots are not plain text.
- File hashes remain stable.
- Wrong recovery key fails.
- Restore does not overwrite live vault without explicit command.
- Restore drill rejects destructive target paths.
- Restore drill rejects tampered backup artifacts.

## Automated Windows End-to-End Test

Run the focused workflow:

```powershell
cd D:\commercial\Wakili-OPs\windows-legal-document-vault\v1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1
```

Run it with a visible app launch and packaged executable smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1 -Visible -IncludePackageSmoke
```

Expected coverage:

- Builds the solution.
- Runs the filtered end-to-end test.
- Starts the WPF app on Windows.
- Optionally verifies the packaged executable.

Run the installed app interactive workflow when validating the actual Windows package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-InstalledAppInteractiveWorkflow.ps1 -BuildAndInstallPackage
```

Or include it from the broader E2E runner:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-EndToEndWorkflow.ps1 -Visible -IncludePackageSmoke -IncludeInteractiveInstalledApp
```

Expected coverage:

- Downloads public online DOCX/PDF samples.
- Installs the self-contained package.
- Completes first-run setup through WPF controls.
- Creates a matter, imports and indexes a DOCX, searches it, imports a PDF through Scan Inbox, exports a filing pack, and runs backup plus restore drill.

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
- Build self-contained `win-x64` package.
- Run packaged executable smoke test.
- Run installed executable smoke test.
- Run installed app interactive workflow test with online sample documents.
- Install into `%LOCALAPPDATA%\Programs\WindowsLegalDocumentVault`.
- Uninstall while preserving user vault data by default.
- Create local vault.
- Restore sample backup.
- Uninstall without deleting user vault unless user explicitly chooses.

## Done Criteria

The MVP is installation-ready when:

- Automated tests pass.
- Automated end-to-end workflow passes.
- Manual acceptance flow passes on developer machine.
- Cross-machine restore passes.
- Installer smoke test passes.
- Installed executable smoke test passes.
- Installed app interactive workflow test passes.
- Documentation matches implemented behavior.
