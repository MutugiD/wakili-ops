# Local Matter RAG Connector V1

This folder is the V1 documentation scaffold for the Local Matter RAG Connector.

## Purpose

The Local Matter RAG Connector will index approved local matter records and expose matter-scoped retrieval for AI workflows without becoming the document vault.

## Depends On

- Windows Legal Document Vault import pipeline.
- OCR/search text availability.
- Matter and document metadata.
- Permission controls for drafts and sensitive matters.

## V1 Goals

- Run locally by default.
- Index matter-scoped OCR text and metadata.
- Support search by matter, document type, date, and status.
- Return snippets with source document IDs and page references.
- Exclude drafts unless explicitly included.
- Avoid uploading the whole firm archive.

## Planned Structure

```text
local-matter-rag-connector/v1/
  documentation/
  app/
```

## Non-Goals

- No direct court filing.
- No raw document custody.
- No training models on firm data.
- No cross-matter retrieval without explicit permission.

