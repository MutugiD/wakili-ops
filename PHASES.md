# Wakili Ops Product Phases

This repository is organized as three independent but connected products. Each phase has its own version folder, documentation, app boundary, tests, and release criteria.

The order matters:

1. Windows Legal Document Vault creates the trusted local document system of record.
2. Local Matter RAG Connector reads approved matter context from that system of record.
3. Wakili-Mkononi Matter AI Integration connects Wakili-Mkononi to retrieved matter context and saves generated work back as drafts.

## Phase 1: Windows Legal Document Vault

Path:

```text
windows-legal-document-vault/v1
```

Detailed plan:

- [Phase 1 Delivery Plan](windows-legal-document-vault/v1/documentation/16-phase-1-delivery-plan.md)
- [Windows Legal Document Vault V1 Handbook](windows-legal-document-vault/v1/documentation/README.md)

### Product Purpose

Windows Legal Document Vault is the local-first Windows application that helps Kenyan advocates and small firms own, organize, search, back up, and recover their matter documents.

It handles the firm-side document problem that court e-filing does not solve:

- Keeping scanned pleadings, affidavits, authorities, receipts, and court outputs organized by matter.
- Preserving local softcopies when internet access, office access, or staff availability is unreliable.
- Maintaining encrypted backups and restore drills before data loss happens.
- Preparing filing packs for manual e-filing without making the app a court portal.

### V1 Scope

- Windows WPF desktop app.
- First-run setup with firm, device, vault, scan folder, backup folder, license, and recovery-key confirmation.
- Encrypted local vault for document bytes.
- SQLite metadata store for matters, documents, versions, scans, search index, and admin state.
- Matter creation and update.
- Manual document import.
- Watched scan-folder inbox.
- Document classification and lifecycle status.
- DOCX and text-like PDF indexing.
- Matter-scoped search.
- Filing-pack export.
- Receipt and court-output capture.
- Local encrypted backup snapshots.
- Non-destructive restore drills.
- Optional encrypted cloud backup package flow using a local filesystem provider adapter.
- Local owner admin install registry for enable, disable, and delete operations.
- Windows package, local install, and installed-app workflow tests.

### Explicit Non-Goals

- No direct court filing automation in V1.
- No hosted payment or entitlement integration in the current implementation loop.
- No upload of matter contents, OCR text, filenames, case numbers, or recovery keys to admin telemetry.
- No automatic restore over the live vault.
- No model training on firm documents.

### Current Implementation Status

Implemented:

- App shell and WPF workflow.
- First-run setup.
- Encrypted vault service.
- SQLite matter and document repositories.
- Document import, scan inbox, classification, versions, OCR/search, filing pack, court-output capture.
- Local backup and restore drill.
- Optional encrypted cloud backup adapter and Backup Center UI flow.
- Local install package and installed-app interactive E2E workflow.
- Local owner admin registry and install-control CLI.

Skipped by instruction:

- Hosted admin/payment entitlement integration.

Next practical candidates:

- Restore workspace browser for local backup snapshots.
- Production cloud provider adapters after a provider is chosen.
- Cross-machine restore wizard.
- Better UI shell and navigation once workflows stabilize.

## Phase 2: Local Matter RAG Connector

Path:

```text
local-matter-rag-connector/v1
```

Detailed plan:

- [Phase 2 Product Plan](local-matter-rag-connector/v1/documentation/01-phase-2-product-plan.md)

### Product Purpose

Local Matter RAG Connector is a local retrieval layer. It makes approved matter documents usable by agentic drafting and legal research workflows without turning the AI layer into the document vault.

It reads from Windows Legal Document Vault or another approved local document database, builds a matter-scoped retrieval index, and exposes a controlled API for downstream tools.

### V1 Scope

- Local service or app process.
- Read-only connector to approved matter/document metadata.
- Local index over OCR text and extracted document text.
- Matter-scoped retrieval API.
- Source citations including matter ID, document ID, version ID, snippet, document type, and confidence metadata.
- Draft/sensitive-document inclusion controls.
- Reindexing pipeline.
- Retrieval evaluation test pack.
- Audit log for retrieval requests.

### Explicit Non-Goals

- No document custody.
- No direct writes to the vault except through approved future contracts.
- No cross-matter retrieval without explicit scope.
- No court filing.
- No hosted vector database by default.

### Readiness Dependency

Phase 2 starts when Phase 1 has stable:

- Matter IDs.
- Document IDs and version IDs.
- Searchable text extraction.
- Backup and restore confidence.
- Permission and lifecycle status semantics.

## Phase 3: Wakili-Mkononi Matter AI Integration

Path:

```text
wakili-mkononi-matter-ai-integration/v1
```

Detailed plan:

- [Phase 3 Product Plan](wakili-mkononi-matter-ai-integration/v1/documentation/01-phase-3-product-plan.md)

### Product Purpose

Wakili-Mkononi Matter AI Integration connects Wakili-Mkononi to matter-scoped context from the Local Matter RAG Connector.

It should help with drafting, chronology extraction, document comparison, issue spotting, and filing-pack assistance while keeping the Windows Legal Document Vault as the source of truth.

### V1 Scope

- Matter linking between Wakili-Mkononi and the local retrieval connector.
- Retrieval-backed drafting.
- Citation display.
- Source document traceability.
- Draft output save-back workflow.
- User confirmation before any generated work is stored.
- Guardrails around filed, served, and court-returned documents.

### Explicit Non-Goals

- No automatic filing.
- No generated output marked as filed or served.
- No direct vault sync by default.
- No unrelated-matter access.
- No hidden cloud document upload.

### Readiness Dependency

Phase 3 starts when Phase 2 has stable:

- Retrieval API.
- Citation format.
- Matter scope enforcement.
- Draft inclusion controls.
- Retrieval quality tests.

## Cross-Phase Rules

- Each phase remains useful on its own.
- Each phase must have a V1 folder before code starts.
- Each phase must keep documentation near the version it describes.
- Later phases consume earlier phases through explicit contracts, not shared assumptions.
- Matter content stays local unless the user explicitly enables a flow that moves encrypted or approved data.
- Admin/payment systems may control installation state, but must not receive matter content.

## Release Gate Pattern

Every phase should pass:

- Build.
- Unit and functional tests.
- End-to-end workflow test for the main user path.
- Security and secret scans.
- Markdown link validation.
- Product naming validation.
- Documentation update for any behavior change.

