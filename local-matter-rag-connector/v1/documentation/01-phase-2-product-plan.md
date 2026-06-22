# Phase 2 Product Plan: Local Matter RAG Connector

Local Matter RAG Connector is the second Wakili Ops product. It is separate from the Windows Legal Document Vault and should not become a document vault, file sync tool, or drafting UI.

## Product Definition

Local Matter RAG Connector is a local retrieval service that reads approved matter text and metadata, builds a retrieval index, and returns cited matter context to authorized local or connected AI workflows.

It exists because agentic legal drafting needs reliable context, but the retrieval system must respect matter boundaries, document lifecycle status, and client confidentiality.

## Primary Users

- Advocate preparing a draft from existing matter documents.
- Legal assistant checking which documents are relevant.
- Wakili-Mkononi integration requesting cited context.
- Firm owner who wants local-first controls before using AI.

## Core Problem

Legal drafting agents need:

- Matter-specific facts.
- Procedural history.
- Pleadings and annexures.
- Orders, rulings, notices, and filing receipts.
- Source citations.

But AI tools must not:

- Search unrelated matters by accident.
- Use draft documents unless explicitly allowed.
- Hide the source of retrieved statements.
- Upload an entire firm archive.
- Become the system of record.

## V1 Goals

- Run locally by default.
- Read from approved document metadata and extracted text.
- Build a matter-scoped index.
- Support exact and semantic retrieval once embeddings are selected.
- Return cited snippets with stable source references.
- Enforce matter scope.
- Allow draft inclusion only when explicitly requested.
- Log retrieval requests without storing confidential prompt text unnecessarily.
- Provide a simple local API for Wakili-Mkononi Matter AI Integration.

## V1 Non-Goals

- No document custody.
- No direct document editing.
- No direct court filing.
- No hosted vector database by default.
- No cross-matter search by default.
- No model training on firm documents.
- No replacement for Windows Legal Document Vault search.

## Dependencies From Phase 1

The connector needs stable access to:

- Matter ID.
- Matter name or redacted display label.
- Document ID.
- Document version ID.
- Document type.
- Document status.
- Imported timestamp.
- Version timestamp.
- Extracted text.
- OCR status.
- Source path is not required and should not be exported by default.

## Proposed Architecture

```text
Windows Legal Document Vault
  -> Approved metadata/text export or read-only local database adapter
  -> Local Matter RAG Connector ingestion pipeline
  -> Local index store
  -> Matter-scoped retrieval API
  -> Wakili-Mkononi Matter AI Integration
```

## Modules

### Source Adapter

Purpose:

- Read approved matter metadata and extracted text from Windows Legal Document Vault.

Rules:

- Read-only by default.
- No vault recovery key storage.
- No raw file byte access unless a later feature explicitly requires it.
- No unrelated matter reads during a matter-scoped retrieval request.

Acceptance:

- Can list documents for one matter.
- Can detect changed document versions.
- Does not expose local filesystem paths in API responses.

### Text Normalization

Purpose:

- Prepare extracted text for indexing.

Responsibilities:

- Normalize whitespace.
- Split into chunks.
- Preserve document/version references.
- Store page or section hints when available.

Acceptance:

- Chunks retain source IDs.
- Empty documents are skipped with a warning.
- Oversized text is split deterministically.

### Index Store

Purpose:

- Store retrieval-ready chunks.

V1 options:

- SQLite FTS for lexical search.
- Local vector store after embedding provider is selected.

Acceptance:

- Reindexing the same source is idempotent.
- Deleted or archived documents are removed or marked unavailable.
- Index can be rebuilt from source data.

### Retrieval API

Purpose:

- Return cited snippets for a matter-scoped query.

Example request:

```json
{
  "matterId": "matter-guid",
  "query": "What orders were issued on interim relief?",
  "includeDrafts": false,
  "documentTypes": ["CourtOrder", "Ruling"],
  "maxResults": 8
}
```

Example response:

```json
{
  "matterId": "matter-guid",
  "results": [
    {
      "documentId": "document-guid",
      "versionId": "version-guid",
      "documentType": "CourtOrder",
      "status": "Filed",
      "snippet": "The court ordered that...",
      "citationLabel": "CourtOrder / version 1",
      "score": 0.82
    }
  ]
}
```

Acceptance:

- No results from unrelated matters.
- Drafts are excluded by default.
- Every snippet has document and version identifiers.
- Empty query returns a validation error.

### Policy Layer

Purpose:

- Enforce retrieval permissions.

Rules:

- Drafts require explicit inclusion.
- Archived documents are excluded unless requested.
- Filed, served, and court-returned documents can be retrieved but not mutated.
- Sensitive matter flags should be supported once Phase 1 exposes them.

Acceptance:

- Policy tests prove draft exclusion.
- Policy tests prove matter-scope enforcement.

### Audit Log

Purpose:

- Record retrieval activity without leaking matter contents.

Suggested fields:

- Request ID.
- Matter ID.
- Caller app.
- Timestamp.
- Include-drafts flag.
- Result count.
- Error status.

Avoid:

- Full query text where it may contain client secrets.
- Document text.
- OCR text.
- Recovery keys.

## Security Requirements

- Local-first by default.
- No automatic cloud sync.
- Explicit configuration for any embedding provider.
- Redaction boundary for telemetry.
- Encrypted-at-rest index if the index contains extracted document text.
- Easy delete/rebuild of the index.

## Testing Plan

Automated tests:

- Source adapter reads only requested matter.
- Chunker preserves source IDs.
- Draft documents excluded by default.
- Retrieval returns stable citation fields.
- Index rebuild produces equivalent results.
- Empty or unsupported documents do not break ingestion.

Functional tests:

- Seed sample matter with pleadings, affidavit, receipt, and ruling.
- Index the matter.
- Query by issue, party, date, and document type.
- Confirm returned snippets cite the correct document versions.

Security tests:

- Ensure no raw local paths appear in default API responses.
- Ensure unrelated matter text cannot be retrieved by a scoped query.
- Ensure logs do not contain document text.

## First Code Slice

Recommended first slice:

- Create the app/test scaffold.
- Define source DTOs and retrieval DTOs.
- Implement an in-memory source adapter and lexical index.
- Add tests for matter scope and draft exclusion.

Do not connect to the live Phase 1 database until the contract tests pass.

