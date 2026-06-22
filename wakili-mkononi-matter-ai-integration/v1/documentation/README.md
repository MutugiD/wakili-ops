# Wakili-Mkononi Matter AI Integration V1

This folder is the V1 documentation pack for the Wakili-Mkononi Matter AI Integration.

Start with:

- [Phase 3 Product Plan](01-phase-3-product-plan.md)

## Purpose

This product will connect Wakili-Mkononi to matter-scoped context from the Local Matter RAG Connector.

## Depends On

- Windows Legal Document Vault as the document source of truth.
- Local Matter RAG Connector as the retrieval layer.
- Consent controls for drafts, sensitive matters, and cloud model usage.

## V1 Goals

- Link a Wakili matter to a local matter context.
- Search matter documents through the Local Matter RAG Connector.
- Return cited snippets to Wakili.
- Support drafting, chronology extraction, comparison, and filing-pack assistance.
- Save generated outputs back as drafts, never as filed documents.

## Planned Structure

```text
wakili-mkononi-matter-ai-integration/v1/
  documentation/
  app/
```

## Non-Goals

- No raw vault sync by default.
- No automatic legal filing.
- No generated output marked as filed or served.
- No access to unrelated matters.
