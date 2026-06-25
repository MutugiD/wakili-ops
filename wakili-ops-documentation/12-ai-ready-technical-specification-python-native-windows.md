# AI-Ready Technical Specification - Wakili Local-First Legal Operations Suite

STATUS: Redesign baseline for technical planning before further feature coding

TARGET PLATFORM: Windows 10/11, 64-bit, local-first, usable without internet after installation

DISTRIBUTION: Single-folder Windows application bundle with optional installer

PRIMARY IMPLEMENTATION STACK: Python 3.11.x, PySide6, SQLite, native Windows build tools, bundled command-line engines

PRODUCTS COVERED:

1. Windows Legal Document Vault
2. Local Matter RAG Connector
3. Wakili-Mkononi Matter AI Integration

This document intentionally mirrors the style of an AI-ready engineering specification. It is more prescriptive than the broad strategic documentation and should be treated as the redesign contract before new code is written.

## 1. System Overview

Build a local-first Windows suite for Kenyan advocates and small law firms that converts messy legal document handling into a controlled matter record.

The suite has three independently shippable products:

Target 1: Windows Legal Document Vault

Purpose: Own, organize, encrypt, search, classify, version, back up, and restore the firm's matter documents on Windows.

Target 2: Local Matter RAG Connector

Purpose: Read approved matter content from the local vault, build local retrieval indexes, and expose citation-safe context to AI systems without becoming the document store.

Target 3: Wakili-Mkononi Matter AI Integration

Purpose: Connect Wakili-Mkononi to local matter context for drafting, chronology, issue extraction, document comparison, and filing-pack assistance.

All legal document custody is local by default. No pleadings, affidavits, exhibits, authorities, OCR text, case numbers, matter names, recovery keys, or filing packs may be sent to an admin service by default.

The official Judiciary e-filing portal remains outside the V1 automation boundary. The app prepares court-ready filing packs and records receipts/court outputs after the user files manually.

## 2. Grounding Context

The Judiciary of Kenya has rolled out e-filing nationally and launched supporting digital services including the Causelist Portal and Data Tracking Dashboard. The Judiciary announcement states that courts should not print pleadings and documents from July 1, 2024, and describes a move toward paperless digital operation.

This creates a real operational gap for law firms:

- Court-side filing may be digital.
- Firm-side custody, scanning, draft control, backups, OCR, matter search, and document reuse remain the advocate's responsibility.
- Advocates need a private system of record before and after e-filing.

Registry and practice-direction materials show the document ecosystem is not just "upload a PDF". It includes notices, memoranda, bills of costs, applications, checklists, practice directions, judgments, rulings, receipts, court outputs, and registry workflows.

## 3. Technology Stack

Use Python 3.11.x exactly for the redesigned product line unless a later technical decision record overrides it.

CRITICAL WARNING: Do not move to Python 3.12+ until all packaging, OCR, PDF, local model, and native-extension dependencies have been tested in a clean Windows VM. Python 3.11 is the lock for commercial packaging predictability.

| Component | Version / Target | Purpose | Source |
| --- | --- | --- | --- |
| Python | 3.11.x, 64-bit | Main runtime | python.org |
| C++ compiler | MSVC Build Tools 2022, v143 toolchain | Native extensions, optional local LLM/vector builds | Microsoft Build Tools |
| UI framework | PySide6 6.11.x | Native Windows desktop UI | PyPI / Qt for Python |
| Packaging | PyInstaller 6.21.x | One-folder .exe distribution | PyInstaller docs |
| Data validation | Pydantic 2.x | Strict schemas for all app contracts | PyPI |
| Metadata DB | SQLite 3.x with FTS5 | Local metadata, search, audit, queues | SQLite docs |
| PDF extraction | PyMuPDF 1.x | PDF text, page count, thumbnails, rendering | PyPI / PyMuPDF docs |
| DOCX extraction | python-docx 1.x | Word document text extraction and draft metadata | PyPI |
| OCR engine | Bundled Tesseract 5.x + tessdata | Image/scanned PDF OCR | Tesseract docs |
| Crypto | cryptography 49.x | AES-GCM vault encryption, RSA license validation | PyPI / cryptography docs |
| Embeddings | ONNX Runtime CPU | Local embedding inference | ONNX Runtime docs |
| Vector index | hnswlib 0.8.x or Qdrant local | Matter-scoped vector search | PyPI / Qdrant docs |
| Local LLM optional | llama-cpp-python | Optional local drafting/runtime experiments | llama-cpp-python docs |
| Cloud backup adapters | rclone.exe or provider SDKs | Optional encrypted snapshot upload | Provider-specific |

Implementation Note:

The commercial user's machine must not need Python, Visual Studio, or a compiler installed. The compiler belongs to the developer/build machine only. The delivered artifact is a packaged Windows application with bundled runtime files and native tools.

## 4. Repository and Product Layout

The repository should keep the existing product names and versioned boundaries, but the Python-native redesign should live beside or inside the relevant product version only after this specification is accepted.

Required layout:

```text
wakili-ops/
  README.md
  PHASES.md
  wakili-ops-documentation/
    12-ai-ready-technical-specification-python-native-windows.md
  windows-legal-document-vault/
    v1/
      documentation/
      app/
      tests/
      tools/
      build/
  local-matter-rag-connector/
    v1/
      documentation/
      app/
      tests/
  wakili-mkononi-matter-ai-integration/
    v1/
      documentation/
      app/
      tests/
```

Current .NET WPF implementation status:

The existing .NET implementation can remain as a discovery prototype and validation harness. New production code should not continue blindly until the Python-native redesign is accepted or rejected. If accepted, the next work item is a migration plan that decides which parts are ported, which tests remain useful, and which data schemas become canonical.

## 5. Core IP: Legal Document Orchestration Logic

The core product value is not the UI. It is the legal document orchestration layer.

### 5.1 Matter Binder Model

Every document must belong to a matter binder unless explicitly placed in an intake/quarantine queue.

Required matter fields:

- matter_id
- firm_id
- client_name
- matter_title
- court
- division
- case_number
- case_year
- parties
- advocate_responsible
- clerk_responsible
- matter_status
- created_at
- updated_at

Matter identifiers must be stable across backup/restore and RAG indexing.

### 5.2 Legal Document Classification

The app must support at least these document categories:

- Pleading
- Affidavit
- Witness statement
- Annexure
- Authority
- Application
- Notice
- Order
- Judgment
- Ruling
- Correspondence
- Receipt
- Court output
- Filing pack manifest
- Other

Classification may begin manual-first, then add assisted classification later.

### 5.3 Document Lifecycle Status

Each document version must carry a lifecycle status:

- Draft
- Reviewed
- Signed
- Filed
- Served
- Court returned
- Superseded
- Archived

Lifecycle transitions must be audited. The app must never silently mark generated AI text as filed, served, signed, or court returned.

### 5.4 Version and Change Session Ledger

Every import, replacement, OCR refresh, metadata edit, file rename, status change, backup, restore drill, RAG export, or Wakili save-back must create an immutable ledger entry.

Required ledger fields:

- event_id
- event_type
- actor
- machine_id
- matter_id
- document_id
- version_id
- before_hash
- after_hash
- timestamp_utc
- local_timestamp
- source_path_hash
- notes

Do not store raw source paths in central admin telemetry. Local storage may keep full paths when needed.

## 6. Product 1: Windows Legal Document Vault

### 6.1 Scope

The vault is the source of truth for matter documents.

Required modules:

- First-run setup
- License activation
- Matter binder
- Scan inbox
- Manual import
- OCR/text extraction queue
- Classification and lifecycle controls
- Version ledger
- Encrypted object store
- SQLite metadata store
- Matter search
- Filing-pack builder
- Receipt and court-output capture
- Local backup snapshots
- Optional encrypted cloud backup
- Restore drill workspace
- Owner-only admin/install control

### 6.2 Dataflow

For each imported file:

1. Copy the source file into a quarantine working folder.
2. Calculate SHA-256 hash and file signature.
3. Validate extension and MIME-like signature.
4. Extract basic metadata: file size, page count if available, created/modified timestamps.
5. Extract text with the correct extractor:
   - DOCX: python-docx.
   - PDF with embedded text: PyMuPDF.
   - Scanned PDF/image: Tesseract OCR pipeline.
6. Ask user for matter and document type if confidence is low.
7. Store encrypted bytes as an immutable vault object.
8. Write metadata, version row, and audit ledger entry.
9. Index extracted text into SQLite FTS5.
10. Queue backup health update.
11. Delete temporary quarantine copy after successful vault write.

### 6.3 Encryption

Use envelope encryption:

- Master key derived from recovery key using Argon2id or PBKDF2-HMAC-SHA256 if Argon2 packaging is not accepted.
- Per-object random data encryption key.
- AES-256-GCM for document bytes.
- Store nonce, tag, algorithm, key_version, and encrypted data key in metadata.
- Never store the raw recovery key.

Required failure behavior:

- If encryption fails, the import fails and no plaintext copy remains in app-managed folders.
- If metadata write fails after encryption, the encrypted object is quarantined for repair and not indexed.
- If restore validation fails, do not overwrite the live vault.

### 6.4 Backup

Local backup package contents:

- encrypted vault objects
- encrypted metadata export
- audit ledger export
- restore manifest
- hash manifest
- app version
- schema version
- backup timestamp

Optional cloud backup:

- Cloud receives only encrypted backup packages.
- Cloud metadata may include package ID, app version, backup timestamp, approximate size, and health status.
- Cloud metadata must not include matter names, client names, case numbers, filenames, OCR snippets, document titles, or recovery keys.

## 7. Product 2: Local Matter RAG Connector

### 7.1 Scope

The connector is a local retrieval system. It must not become another document store.

Required modules:

- Read-only vault adapter
- Matter-scoped text export
- Chunking pipeline
- Embedding pipeline
- Vector index
- Hybrid search using FTS5 + vector search
- Citation assembler
- Draft/sensitive document inclusion policy
- Local HTTP API
- Retrieval audit log
- Evaluation pack

### 7.2 Dataflow

For each approved matter:

1. Read document metadata and extracted text from the vault adapter.
2. Exclude documents marked as draft/sensitive unless user explicitly allows them.
3. Chunk text by document structure:
   - page boundary when available
   - heading boundary when available
   - paragraph boundary as fallback
4. Create embeddings locally using ONNX Runtime CPU.
5. Store vectors in local index with citation payload.
6. Store lexical search records in FTS5.
7. Expose retrieval endpoint on localhost only.

### 7.3 Retrieval Contract

Every retrieved item must include:

- matter_id
- document_id
- version_id
- document_type
- lifecycle_status
- page_number if available
- chunk_id
- snippet
- score
- source_hash
- citation_label

No downstream AI tool may receive uncited context.

## 8. Product 3: Wakili-Mkononi Matter AI Integration

### 8.1 Scope

This product connects Wakili-Mkononi to the Local Matter RAG Connector.

Required modules:

- Local connector discovery
- Matter linking
- Retrieval request UI/API
- Prompt/context pack builder
- Citation display
- Draft generator bridge
- Save-back to vault as Draft only
- User confirmation before storing generated output
- Audit log

### 8.2 Dataflow

For each drafting request:

1. User selects a matter in Wakili-Mkononi.
2. Integration asks the Local Matter RAG Connector for scoped context.
3. Connector returns cited chunks only.
4. Wakili-Mkononi builds a prompt/context pack.
5. Draft output is created.
6. User reviews output.
7. If user saves, output is written back to the vault as a new Draft document/version.
8. Audit ledger records retrieval IDs, source IDs, and save-back details.

## 9. Local Licensing and Owner Admin Control

### 9.1 Machine Hash

On first startup, generate a machine-bound installation ID.

Input signals may include:

- Windows MachineGuid
- motherboard/BIOS serial if available without admin rights
- app-generated install UUID

Never use the machine hash as an encryption key.

### 9.2 Offline License File

License file format:

```json
{
  "installation_id": "<stable-install-id>",
  "firm_name": "<licensed-firm>",
  "products": ["vault", "rag_connector", "wakili_integration"],
  "features": {
    "cloud_backup": true,
    "local_rag": false,
    "wakili_bridge": false
  },
  "expiry": "2027-12-31",
  "signature": "<base64-rsa-pss-signature>"
}
```

Validation rules:

1. Load the hard-coded public key from the packaged app.
2. Verify RSA-PSS signature using SHA-256.
3. Confirm installation ID.
4. Confirm expiry.
5. Confirm product/feature entitlements.
6. Record last successful validation timestamp locally.

### 9.3 Optional Hosted Admin

The hosted admin service is optional and may be skipped during early product development.

If implemented later, it may track only:

- installation ID
- firm display name
- licensed products/features
- app version
- last check-in
- enabled/disabled status
- coarse backup health

It must not receive:

- document files
- OCR text
- filenames
- case numbers
- client names
- matter names
- recovery keys
- RAG prompts
- generated drafts

## 10. UI Specification

Use a professional light-mode desktop interface.

Palette:

- Primary: #1F4E79
- Accent: #2E7D32
- Warning: #B26A00
- Error: #B00020
- Background: #FFFFFF
- Surface: #F7F9FC
- Text: #111827
- Secondary text: #4B5563
- Border: #D0D7DE

Window 1: License Activation

- Machine/installation ID
- Copy ID button
- License file browse button
- Activate button
- Offline activation instructions
- Clear invalid-license message

Window 2: Main Vault

- Left navigation: Dashboard, Matters, Scan Inbox, Import, Search, Filing Packs, Backup, Settings
- Center workspace changes by selected module
- Right inspection panel for document metadata, lifecycle, versions, and audit history
- Status bar for vault lock state, backup health, OCR queue, and license state

Window 3: Restore Drill

- Backup snapshot list
- Verify button
- Restore-to-workspace button
- Hash validation report
- Open restored folder button
- Explicit warning that live vault is not overwritten

Window 4: Local RAG Connector

- Matter index status
- Included/excluded document rules
- Reindex button
- Retrieval test console
- Citation preview

## 11. Packaging and Build Protocol

### 11.1 Development Machine Setup

Required:

- Windows 11 64-bit build machine
- Python 3.11.x 64-bit
- Visual Studio Build Tools 2022 with C++ workload
- Git
- PowerShell 7 optional but recommended

### 11.2 Virtual Environment

```powershell
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip wheel setuptools
pip install -r requirements.txt
```

### 11.3 PyInstaller One-Folder Build

Required shape:

```powershell
pyinstaller `
  --onedir `
  --name "WakiliLegalDocumentVault" `
  --noconsole `
  --clean `
  --noupx `
  --add-data "tools\tesseract;tesseract" `
  --add-data "models;models" `
  --add-data "schemas;schemas" `
  app\main.py
```

The production build should use a `.spec` file once native tools, model files, schema files, and Qt plugins are stable.

### 11.4 Bundled Tools

The bundle must include:

- Tesseract executable and required tessdata.
- Any PDF repair/splitting tool selected for production.
- Local embedding model files.
- JSON schema files.
- License public key.

The bundle must not include:

- vendor private signing keys
- sample client matters
- test secrets
- development `.env` files
- raw logs from real users

## 12. Testing Protocol

Payment or production acceptance is blocked until all tests pass.

### 12.1 Clean Windows VM Test

Test on a clean Windows 10/11 VM with:

- no Python installed
- no Visual Studio installed
- no developer PATH assumptions
- no internet required after bundle is copied

### 12.2 Legal Document Corpus

The test corpus must include:

- text PDF
- scanned PDF
- DOCX draft
- image scan
- multi-page pleading-style PDF
- authority PDF
- court practice direction PDF
- ruling/judgment PDF
- receipt-like PDF/image
- corrupted PDF
- duplicate file
- renamed duplicate file
- large PDF over 100 MB where available

### 12.3 End-to-End Test

The automated E2E test must:

1. Start the packaged app.
2. Activate or use a test license.
3. Create a test vault.
4. Create at least two matters.
5. Import DOCX, text PDF, scanned PDF, image, authority, judgment/ruling, and receipt files.
6. OCR at least one scanned document.
7. Classify documents into at least six legal document types.
8. Create a new version of one document.
9. Search within one matter.
10. Export a filing pack.
11. Capture a receipt/court output.
12. Create a local backup.
13. Verify backup hashes.
14. Restore to a separate workspace.
15. Verify restored metadata and files.
16. Build a RAG index for one matter.
17. Run a retrieval query and verify citations.
18. Send a retrieval-backed draft request through the Wakili integration mock.
19. Save generated output back as Draft only.
20. Produce a signed test report with screenshots.

## 13. Error Handling Requirements

Scenario 1: Corrupt PDF

Action: Mark as failed import, keep audit event, do not write partial document to vault, continue queue.

Scenario 2: OCR failure

Action: Store original encrypted document, mark OCR status failed, allow manual retry, preserve file.

Scenario 3: Duplicate document

Action: Show duplicate warning with existing matter/document reference. Let user attach as new version, duplicate in another matter, or cancel.

Scenario 4: Wrong matter selected

Action: Allow controlled move with audit ledger entry. Do not rewrite original import history.

Scenario 5: Backup target unavailable

Action: Show backup failure, keep vault usable, do not mark backup healthy.

Scenario 6: Cloud backup disabled by license

Action: Hide or lock cloud backup UI. Local backup must still work.

Scenario 7: RAG index stale

Action: Retrieval response must include stale-index warning and last indexed timestamp.

Scenario 8: Wakili integration cannot reach local connector

Action: Show local connector offline state and do not fall back to sending full documents to cloud.

Scenario 9: App closed during import/OCR/backup

Action: Ask for confirmation, stop queue safely, preserve resumable job state.

Scenario 10: System clock rollback

Action: Lock licensed features if current time is earlier than the last successful validation timestamp. Do not lock access to export/backup rescue tools without a deliberate emergency-access policy.

## 14. Security and Compliance Rules

- Local-first by default.
- No hidden telemetry.
- No raw legal content in crash reports.
- No remote admin access to matter content.
- No direct court portal automation in V1.
- No plaintext backup package.
- No model training on client documents.
- No cross-matter retrieval unless user explicitly selects multiple matters.
- No AI save-back as anything other than Draft.
- Recovery key must be user-held and never uploaded.
- Every data movement must be logged locally.

## 15. Developer Deliverables

For each product:

- Source code
- Requirements lock file
- Build script
- PyInstaller spec file
- JSON schemas
- Test corpus manifest
- Unit tests
- E2E tests
- Security tests
- Clean Windows VM report
- Installation guide
- Recovery/backup guide
- Admin/licensing guide where relevant
- Architecture decision record for any deviation from this specification

## 16. Redesign Implementation Order

Do not build the three products together.

### Milestone A: Redesign Acceptance

- Review this specification.
- Decide whether Python 3.11 native Windows becomes the production direction.
- Decide what happens to the existing .NET prototype.
- Lock exact package versions in `requirements.txt`.

### Milestone B: Windows Legal Document Vault Skeleton

- Python package structure.
- PySide6 app shell.
- License activation window.
- First-run setup.
- SQLite schema creation.
- Vault path initialization.
- Test license flow.

### Milestone C: Ingestion and Vault

- Manual import.
- Scan inbox.
- File validation.
- Encryption.
- Metadata write.
- Audit ledger.
- Unit and E2E test.

### Milestone D: Extraction and Search

- DOCX extraction.
- PDF extraction.
- OCR for scanned docs.
- FTS5 indexing.
- Matter-scoped search.
- Test corpus expansion.

### Milestone E: Filing and Backup

- Filing pack builder.
- Receipt/court-output capture.
- Backup snapshot.
- Restore drill.
- Optional encrypted cloud package adapter.

### Milestone F: Local Matter RAG Connector

- Read-only vault adapter.
- Chunking.
- ONNX embeddings.
- Vector index.
- Citation API.
- Retrieval evaluation.

### Milestone G: Wakili-Mkononi Integration

- Local connector discovery.
- Matter linking.
- Retrieval-backed draft mock.
- Save-back as Draft.
- Audit trail.

## 17. Source Anchors Reviewed

- Judiciary e-filing portal: https://efiling.court.go.ke/
- Judiciary nationwide e-filing rollout announcement: https://judiciary.go.ke/judiciary-launches-e-filing-in-all-courts-data-tracking-dashboard-and-causelist-portal-portal/
- Court of Appeal Registry Manual: https://judiciary.go.ke/wp-content/uploads/2023/07/COA-REG-Manual.pdf
- Supreme Court General Practice Directions, 2020: https://supremecourt.judiciary.go.ke/wp-content/uploads/2022/11/Supreme-Court-General-Practice-Directions-of-2020.pdf
- Supreme Court Self-Representing Litigants E-Guide: https://supremecourt.judiciary.go.ke/wp-content/uploads/2025/06/SUPREME-COURT-OF-KENYA-SELF-REPRESENTING-LITIGANTS-E-GUIDE.pdf
- Python 3.11 release page: https://www.python.org/downloads/release/python-3119/
- Microsoft Visual Studio Build Tools component directory: https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools
- PyInstaller requirements: https://pyinstaller.org/en/stable/requirements.html
- PySide6 package metadata: https://pypi.org/project/PySide6/
- SQLite FTS5 documentation: https://sqlite.org/fts5.html
- Tesseract installation documentation: https://tesseract-ocr.github.io/tessdoc/Installation.html
- cryptography package metadata and RSA documentation: https://pypi.org/project/cryptography/ and https://cryptography.io/en/latest/hazmat/primitives/asymmetric/rsa/
- ONNX Runtime Python documentation: https://onnxruntime.ai/docs/get-started/with-python.html
- hnswlib package metadata: https://pypi.org/project/hnswlib/
- Qdrant local quickstart: https://qdrant.tech/documentation/quickstart/
- llama-cpp-python Windows/native build notes: https://github.com/abetlen/llama-cpp-python

## 18. Final Developer Notes

- This specification is intentionally stricter than the existing documentation.
- It is meant to prevent vague platform drift.
- The next code work should begin only after deciding whether to port from the current .NET prototype to this Python 3.11 native Windows direction.
- If the Python direction is accepted, the first code PR should create only the skeleton, build scripts, package lock, and clean app startup test.
- If the .NET direction is retained, this document should be converted into an equivalent .NET-native technical specification before further feature work.
