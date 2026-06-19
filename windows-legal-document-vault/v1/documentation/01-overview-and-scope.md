# Overview and Scope

## Purpose

This document defines the V1 implementation scope for the Windows Legal Document Vault.

The product is an office utility for advocates and clerks who need reliable local control of legal documents before and after Judiciary e-filing. It should feel practical, dependable, and recoverable.

## Primary Users

- Solo advocates.
- Small firms with 1 to 10 advocates.
- Clerks and legal assistants who scan, classify, file, and retrieve documents.
- Firm owners who need backup and audit confidence.

## Core V1 Workflows

1. First-run setup creates a local encrypted vault.
2. User creates a matter.
3. User imports or scans documents into the matter.
4. App records metadata, hashes, classification, versions, and audit events.
5. User searches and reviews matter documents.
6. User prepares a filing pack.
7. User manually uploads the pack to the official Judiciary portal.
8. User attaches receipts, notices, orders, or registry outputs.
9. User backs up and later restores the vault.

## In Scope

- Windows desktop app.
- First-run setup wizard.
- Encrypted local vault.
- SQLite metadata database.
- Matter creation and management.
- Manual file import.
- Watched scan folder import.
- Basic duplicate detection.
- Document classification.
- Document version lifecycle.
- Basic OCR adapter and OCR status tracking.
- Full-text search.
- Filing-pack builder.
- Receipt and court-output attachment.
- Local/external encrypted backup.
- Restore drill.
- Audit log.
- Windows installer packaging.

## Out of Scope

- Direct automation of `efiling.court.go.ke`.
- Cloud-first storage.
- Cloud OCR by default.
- Local Matter RAG Connector.
- Wakili-Mkononi Matter AI Integration.
- Billing, trust accounting, invoicing, or full practice management.
- Multi-office synchronization.
- Enterprise SSO.
- Legal advice generation.

## Success Criteria

V1 succeeds when a solo advocate or clerk can complete this flow on Windows:

1. Install the app.
2. Create a vault.
3. Configure a watched scanner folder.
4. Create a matter.
5. Import a scanned PDF.
6. Classify and version the document.
7. Search for it.
8. Generate a filing pack.
9. Manually file on the Judiciary portal.
10. Attach the resulting receipt.
11. Back up the vault.
12. Restore the matter on another Windows machine.

## Non-Negotiable Product Principles

- Documents stay local by default.
- Encryption is present from the first storage slice.
- Backup is treated as core functionality, not a later extra.
- Filing pack preparation is clearly separate from actual filing.
- Filed and served versions are never overwritten.
- Every important action creates an audit event.

