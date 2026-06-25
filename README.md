# Wakili Ops

Wakili Ops is a multi-product legal operations workspace for Kenyan advocates and small law firms. The repository is organized by product and version so each product can evolve independently.

## Product Phases

For the detailed phase-by-phase delivery map, see [Wakili Ops Product Phases](PHASES.md).

### Phase 1: Windows Legal Document Vault

Path:

```text
windows-legal-document-vault/v1
```

Purpose:

- Windows plug-and-play local document vault.
- Matter management.
- Encrypted local storage.
- Scanner-folder import workflow.
- Document versioning.
- Filing-pack preparation.
- Local/external backup.
- Optional encrypted cloud backup add-on.
- Owner-only licensing/install control through a future admin dashboard.

Current status:

- V1 app scaffold exists.
- .NET 10 WPF app builds.
- Setup wizard is implemented.
- Encrypted vault service is implemented.
- SQLite matter management is implemented.
- Functional/security test harness is active.
- Local owner admin install-control scaffold is implemented.

### Phase 2: Local Matter RAG Connector

Path:

```text
local-matter-rag-connector/v1
```

Purpose:

- Local retrieval layer over the Windows Legal Document Vault or another approved document database.
- Matter-scoped search and retrieval.
- Draft include/exclude controls.
- Citation-safe context assembly.
- Local API for downstream AI tools.

Current status:

- V1 scaffold exists.
- No application code yet.
- Implementation starts after the Windows Legal Document Vault has reliable import, OCR/search, and backup flows.

### Phase 3: Wakili-Mkononi Matter AI Integration

Path:

```text
wakili-mkononi-matter-ai-integration/v1
```

Purpose:

- Connect Wakili-Mkononi to matter-scoped context from the Local Matter RAG Connector.
- Support legal drafting, chronology extraction, document comparison, and filing-pack assistance.
- Keep the Windows Legal Document Vault as the source of truth.

Current status:

- V1 scaffold exists.
- No application code yet.
- Implementation starts after the Local Matter RAG Connector has stable retrieval APIs.

## Repository Structure

```text
.
  README.md
  wakili-ops-documentation/
  windows-legal-document-vault/
    v1/
      documentation/
      src/
      tests/
      WakiliDms.sln
  local-matter-rag-connector/
    v1/
      documentation/
      app/
  wakili-mkononi-matter-ai-integration/
    v1/
      documentation/
      app/
  scripts/
    ci/
  .github/
```

## Strategic Documentation

The broad research and product documentation lives in:

```text
wakili-ops-documentation/
```

Start with:

- [Wakili Ops Documentation](wakili-ops-documentation/README.md)
- [AI-Ready Python Native Windows Redesign Specification](wakili-ops-documentation/12-ai-ready-technical-specification-python-native-windows.md)
- [Wakili Ops Product Phases](PHASES.md)
- [Windows Legal Document Vault V1 Handbook](windows-legal-document-vault/v1/documentation/README.md)

## Local Development

For the first product:

```powershell
cd windows-legal-document-vault\v1
dotnet build WakiliDms.sln
dotnet run --project tests\WakiliDms.Tests\WakiliDms.Tests.csproj
dotnet run --project src\WakiliDms.App\WakiliDms.App.csproj
```

If the global `dotnet` command does not expose a .NET 10 SDK on the development machine, use:

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" build WakiliDms.sln
```

## CI/CD and Security

CI is intentionally security-heavy because this repo handles legal document infrastructure.

Required gates include:

- Product scaffold validation.
- Markdown link validation.
- Legacy numbered-product terminology check.
- .NET format verification.
- Release build.
- Functional/security test harness.
- Dependency vulnerability scan.
- CodeQL static analysis.
- Gitleaks secret scanning.
- Dependabot for NuGet and GitHub Actions.

See:

- [CI/CD and Security Gates](windows-legal-document-vault/v1/documentation/15-ci-cd-and-security-gates.md)

## Data Protection Boundary

The Windows app must never send client document contents, matter names, case numbers, OCR text, recovery keys, or filing-pack contents to the admin dashboard or cloud backup metadata.

Cloud backup, when implemented, must upload only client-side encrypted snapshots.

Admin/licensing controls may track installation IDs, license status, app version, last check-in, enabled/disabled state, and backup health metadata only.
