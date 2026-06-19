# CI/CD and Security Gates

## Purpose

This document defines the CI/CD process for the Windows Legal Document Vault V1 app.

The product handles confidential legal files, so CI must prove more than "the app compiles." It must also check encryption behavior, vault safety, dependency vulnerabilities, secret leaks, and code quality.

## Workflow Files

Repository-level automation:

- `.github/workflows/repository-governance.yml`
- `.github/workflows/windows-legal-document-vault-v1-ci.yml`
- `.github/workflows/codeql.yml`
- `.github/dependabot.yml`
- `.gitleaks.toml`

The V1 app lives at:

```text
windows-legal-document-vault/v1
```

All build and test commands run from that folder.

## CI Trigger Rules

CI runs on:

- Push to `dev`.
- Push to `main` or `master`.
- Pull request targeting `dev`, `main`, or `master`.
- Changes under `windows-legal-document-vault/v1/**`.
- Changes to workflow or leak-scan configuration files.

CodeQL also runs weekly.

## Required CI Gates

### Product Structure

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/ci/Assert-ProductStructure.ps1
```

Purpose:

- Confirms each product has a versioned scaffold.
- Confirms the first product keeps app code under `windows-legal-document-vault/v1`.
- Prevents future products from being added as loose root-level folders.

### Markdown Links

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/ci/Test-MarkdownLinks.ps1
```

Purpose:

- Ensures local documentation links resolve.
- Prevents broken handoff docs.

### Product Naming

Command:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/ci/Test-NoLegacySolutionLabels.ps1
```

Purpose:

- Keeps docs and code self-explanatory.
- Prevents reintroducing numbered product labels.

### Restore

Command:

```powershell
dotnet restore WakiliDms.sln
```

Purpose:

- Confirms NuGet dependency graph resolves.
- Fails early if packages cannot be restored.

### Code Style

Command:

```powershell
dotnet format WakiliDms.sln --verify-no-changes --verbosity minimal
```

Purpose:

- Prevents formatting drift.
- Keeps future diffs readable.
- Avoids style-only churn during feature work.

### Build

Command:

```powershell
dotnet build WakiliDms.sln --configuration Release --no-restore
```

Purpose:

- Compiles WPF app, core library, infrastructure library, and tests.
- Treats warnings as errors through `Directory.Build.props`.

### Functional and Security Test Harness

Command:

```powershell
dotnet run --configuration Release --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj --no-build
```

Current required tests:

- Setup validation accepts complete local-first settings.
- Setup validation rejects cloud backup in V1 unless implemented as licensed add-on.
- Matter creation trims required name.
- Filed and served document statuses are immutable.
- JSON settings store saves and loads setup state.
- Encrypted vault creates manifest and stores unreadable object bytes.
- Encrypted vault rejects wrong recovery key.
- SQLite matter repository persists and lists matters.
- SQLite matter repository updates matter details.

As new modules are added, tests must be added before the slice is marked complete.

### Dependency Vulnerability Scan

Command:

```powershell
dotnet list WakiliDms.sln package --include-transitive --vulnerable
```

Purpose:

- Detects vulnerable direct and transitive NuGet packages.
- Prevents insecure package paths from entering the app.

This is mandatory because the app stores legal documents and encrypted backups.

### Secret Leak Scan

Tool:

- Gitleaks container scan.
- Uses the pinned `ghcr.io/gitleaks/gitleaks:v8.28.0` image in an Ubuntu CI job.

Purpose:

- Detects secrets, tokens, private keys, connection strings, and accidental credentials.

Rules:

- No cloud credentials in repo.
- No admin dashboard secrets in repo.
- No license signing keys in repo.
- No real test client documents or private data in repo.

### CodeQL

Tool:

- GitHub CodeQL for C#.

Purpose:

- Static security analysis.
- Detects common code-level vulnerabilities.
- Runs on push, pull request, and weekly schedule.

## Vault and Encryption Gates

Every vault-related slice must prove:

- Stored vault objects are not plain text.
- Wrong recovery key fails.
- Object hash validates after decrypt.
- Failed decrypt does not corrupt the vault.
- Backup snapshot bytes are encrypted before cloud upload once cloud backup exists.

Tests must not only assert success paths. They must include failure and wrong-key paths.

## Leak Prevention Gates

The app must not send these fields to CI logs, admin dashboard logs, cloud backup metadata, or telemetry:

- Matter names.
- Party names.
- Client names.
- Case numbers.
- Document filenames.
- OCR text.
- Plain file paths from a user's machine.
- Recovery keys.

When logging is added, tests should verify sensitive fields are redacted.

## Branch Strategy

Use:

- `dev`: active development branch.
- `main` or `master`: stable branch after manual review.

Rules:

- All feature work starts on `dev` or feature branches from `dev`.
- CI must pass before merging to stable.
- Security workflow failures block release.

## Release Strategy

V1 release should not be cut until:

- CI passes.
- CodeQL passes.
- Dependency scan has no vulnerable packages.
- Gitleaks passes.
- Local Windows install smoke passes.
- End-to-end document tests pass with DOCX, PDF, scanned PDF, image, duplicate, corrupt, and unsupported files.
- Optional cloud backup tests pass once that add-on is implemented.
- Admin/licensing controls are tested without exposing document data.

## Local Pre-Push Checklist

Run from `windows-legal-document-vault/v1`:

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" build WakiliDms.sln
& "$env:USERPROFILE\.dotnet\dotnet.exe" run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj
& "$env:USERPROFILE\.dotnet\dotnet.exe" list package --include-transitive --vulnerable
```

Optional local app smoke:

```powershell
$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
$project = Join-Path (Get-Location) "src\WakiliDms.App\WakiliDms.App.csproj"
$proc = Start-Process -FilePath $dotnet -ArgumentList @("run","--project",$project,"--no-build") -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 5
if (-not $proc.HasExited) {
  Stop-Process -Id $proc.Id -Force
  "PASS App startup smoke"
} else {
  throw "App exited early with code $($proc.ExitCode)"
}
```
