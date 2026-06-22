# Phase 3 Product Plan: Wakili-Mkononi Matter AI Integration

Wakili-Mkononi Matter AI Integration is the third Wakili Ops product. It connects Wakili-Mkononi to matter-scoped context from the Local Matter RAG Connector.

It should make legal drafting smarter without weakening the document custody model created by Windows Legal Document Vault.

## Product Definition

This product is an integration layer between Wakili-Mkononi and local matter retrieval.

It lets Wakili-Mkononi ask for cited matter context, generate draft legal work, compare documents, produce chronologies, and save approved outputs back as drafts.

The integration must treat Windows Legal Document Vault as the source of truth and Local Matter RAG Connector as the retrieval boundary.

## Primary Users

- Advocate drafting pleadings, affidavits, submissions, or letters.
- Legal assistant preparing a chronology or document bundle summary.
- Firm owner controlling whether AI may access local matter context.
- Wakili-Mkononi product operator validating quality and safety.

## Core Problem

Wakili-Mkononi can draft better when it has matter-specific context, but direct access to a whole firm vault is too risky.

The integration must:

- Retrieve only the selected matter.
- Show citations for every factual claim derived from local documents.
- Save generated work as drafts only.
- Preserve user control before anything enters the document vault.
- Avoid silent upload of confidential files.

## V1 Goals

- Link a Wakili-Mkononi matter to a local matter ID.
- Query Local Matter RAG Connector for cited snippets.
- Display source citations in Wakili-Mkononi.
- Generate drafts using retrieved context.
- Support chronology extraction.
- Support document comparison.
- Support filing-pack assistance recommendations.
- Save generated output back as draft metadata or a draft document through an explicit approved contract.

## V1 Non-Goals

- No automatic court filing.
- No generated output marked as filed or served.
- No direct raw vault sync.
- No hidden cloud upload of matter documents.
- No unrelated-matter retrieval.
- No bypass of Local Matter RAG Connector.

## Dependencies From Phase 2

Required contracts:

- Matter-scoped retrieval endpoint.
- Citation response format.
- Draft inclusion flag.
- Document type filters.
- Result confidence score.
- Retrieval audit event.
- Error model for missing matter, unavailable index, or blocked policy.

## High-Level Flow

```text
Wakili-Mkononi user opens matter
  -> user links local matter
  -> integration requests cited context
  -> Local Matter RAG Connector enforces matter scope
  -> Wakili-Mkononi receives snippets and citations
  -> AI draft is generated
  -> user reviews
  -> approved output is saved as draft only
```

## Modules

### Matter Linker

Purpose:

- Connect a Wakili-Mkononi matter record to a local matter ID.

Rules:

- Linking requires user confirmation.
- Link can be removed.
- Link does not copy the whole vault.
- Link state should not include recovery keys.

Acceptance:

- User can link one Wakili matter to one local matter.
- Unlinked matters cannot retrieve local context.
- Link removal blocks future retrieval.

### Retrieval Client

Purpose:

- Call Local Matter RAG Connector and return cited context to Wakili-Mkononi.

Responsibilities:

- Build retrieval request.
- Pass matter ID and filters.
- Respect draft inclusion setting.
- Handle connector unavailable errors.
- Preserve citations.

Acceptance:

- Every returned snippet includes document and version identifiers.
- Retrieval errors are visible to the user.
- Drafts are excluded unless the user opts in.

### Drafting Orchestrator

Purpose:

- Use retrieved context to support legal drafting workflows.

Draft types:

- Pleading outline.
- Affidavit draft.
- Submissions outline.
- Chronology.
- Letter.
- Document comparison memo.
- Filing-pack checklist suggestions.

Acceptance:

- Draft output includes citation references.
- Draft output is never marked filed, served, or court-returned.
- User review is required before save-back.

### Save-Back Adapter

Purpose:

- Store generated outputs back into the approved document workflow.

V1 behavior:

- Save as draft only.
- Store provenance metadata showing it was AI-assisted.
- Preserve citations used during generation.
- Require user confirmation.

Acceptance:

- Save-back cannot overwrite filed or served documents.
- Save-back cannot skip document classification.
- Save-back produces a new draft/version.

### Policy And Consent

Purpose:

- Keep AI usage explicit and auditable.

Rules:

- User must opt in to use local matter context.
- Draft inclusion must be visible.
- Sensitive matter blocks should be honored once exposed by Phase 1/2.
- Recovery keys are never sent to Wakili-Mkononi.

Acceptance:

- Integration blocks retrieval without a linked matter.
- Integration blocks retrieval when connector policy denies access.
- Audit events do not include full document text.

## Data Contracts

### Retrieval Request

```json
{
  "matterId": "matter-guid",
  "wakiliMatterId": "wakili-matter-id",
  "query": "Draft chronology for interim application",
  "includeDrafts": false,
  "documentTypes": ["Pleading", "Affidavit", "CourtOrder", "Ruling"],
  "maxResults": 12
}
```

### Retrieval Result

```json
{
  "matterId": "matter-guid",
  "results": [
    {
      "documentId": "document-guid",
      "versionId": "version-guid",
      "documentType": "Affidavit",
      "status": "Filed",
      "snippet": "The applicant states that...",
      "citationLabel": "Affidavit, version 1",
      "score": 0.88
    }
  ]
}
```

### Draft Save Request

```json
{
  "matterId": "matter-guid",
  "wakiliMatterId": "wakili-matter-id",
  "title": "Draft supporting affidavit",
  "documentType": "Affidavit",
  "status": "Draft",
  "body": "Generated draft text...",
  "sourceCitations": [
    {
      "documentId": "document-guid",
      "versionId": "version-guid",
      "citationLabel": "Affidavit, version 1"
    }
  ],
  "aiAssisted": true
}
```

## Security Requirements

- No recovery key transmission.
- No direct vault database access from Wakili-Mkononi.
- No hidden background retrieval.
- No unrelated-matter retrieval.
- No generated output promoted to filed/served status.
- Minimal telemetry.
- Explicit cloud-model disclosure where applicable.

## Testing Plan

Automated tests:

- Retrieval blocked when matter is not linked.
- Drafts excluded by default.
- Cited snippets remain attached to generated output.
- Save-back always uses Draft status.
- Save-back refuses filed/served overwrite.
- Connector unavailable error is handled clearly.

Functional tests:

- Link a sample matter.
- Retrieve cited context.
- Generate a chronology.
- Generate a draft affidavit.
- Save draft output.
- Confirm output appears as draft in the document workflow.

Security tests:

- Ensure request payloads exclude recovery keys.
- Ensure unrelated matter IDs cannot be queried.
- Ensure logs do not contain full source document text.

## First Code Slice

Recommended first slice:

- Create integration scaffold and test harness.
- Define retrieval client interface.
- Use a fake Local Matter RAG Connector response.
- Implement matter link state.
- Prove linked/unlinked retrieval behavior with tests.

Do not integrate with live Wakili-Mkononi until the local contract tests pass.

