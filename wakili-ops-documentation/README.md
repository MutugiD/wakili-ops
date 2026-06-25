# Wakili Local-First Legal Document Platform Documentation

This documentation pack defines a documentation-first product plan for a Windows plug-and-play legal document system for Kenyan solo advocates and small law firms.

The immediate product focus is the Windows Legal Document Vault: a local-first Windows document management, scanning, OCR, filing-pack, and backup system. The Local Matter RAG Connector and Wakili-Mkononi Matter AI Integration are documented as separate future products so the architecture does not collapse into one large platform too early.

For the implementation phase map, see [Wakili Ops Product Phases](../PHASES.md).

## Product Set

### Windows Legal Document Vault

Windows Legal Document Vault is the first product to build. It is a Windows installer and tray app that helps advocates keep their own organized, searchable, backed-up, versioned softcopies of legal documents.

It addresses the gap left by court e-filing: e-filing supports online submission and court-side tracking, but advocates still need a private matter vault for scans, drafts, filed copies, receipts, court outputs, and future reuse.

### Local Matter RAG Connector

Local Matter RAG Connector is a separate local indexing and retrieval service. It reads the local document vault or existing document database, creates a searchable index, and exposes matter-scoped retrieval without becoming the document store.

### Wakili-Mkononi Matter AI Integration

Wakili-Mkononi Matter AI Integration connects Wakili-Mkononi to the RAG connector. Wakili consumes matter-scoped retrieval and drafting context, but does not become the default store of confidential law firm documents.

## Why This Exists

The Judiciary of Kenya has digitized major parts of filing and case interaction. The official e-filing portal is available at [efiling.court.go.ke](https://efiling.court.go.ke/). The Judiciary announced nationwide e-filing rollout and related digital services, including the Causelist Portal and Data Tracking Dashboard, with a direction that courts should not print pleadings and documents from July 1, 2024. See [All Courts Nationwide Go Digital](https://judiciary.go.ke/judiciary-launches-e-filing-in-all-courts-data-tracking-dashboard-and-causelist-portal-portal/).

This solves a court submission problem, but not the law firm document ownership problem. Advocates still need:

- Local ownership of their case files and legal drafting material.
- Organized softcopies and backups of voluminous legal documents.
- Searchable OCR for scanned pleadings, affidavits, annexures, authorities, orders, and receipts.
- Version history across drafts, signed copies, filed copies, amended pleadings, and served documents.
- Filing-pack preparation before manual upload to e-filing.
- A future RAG layer for agentic drafting from the firm's own matter record.

## Documentation Map

- [00-source-notes-and-assumptions.md](00-source-notes-and-assumptions.md): citations, source notes, assumptions, and caveats.
- [01-problem-analysis-kenyan-legal-document-workflows.md](01-problem-analysis-kenyan-legal-document-workflows.md): detailed problem analysis.
- [02-windows-legal-document-vault.md](02-windows-legal-document-vault.md): Windows Legal Document Vault product specification.
- [03-windows-legal-document-vault-flow-and-operations.md](03-windows-legal-document-vault-flow-and-operations.md): document flow, daily operations, and filing-pack process.
- [04-windows-legal-document-vault-architecture.md](04-windows-legal-document-vault-architecture.md): proposed Windows local-first architecture.
- [05-windows-legal-document-vault-security-backup-and-compliance.md](05-windows-legal-document-vault-security-backup-and-compliance.md): privacy, security, backup, and Kenya Data Protection Act posture.
- [06-local-matter-rag-connector.md](06-local-matter-rag-connector.md): separate local RAG connector documentation.
- [07-wakili-mkononi-matter-ai-integration.md](07-wakili-mkononi-matter-ai-integration.md): Wakili integration documentation.
- [08-product-roadmap.md](08-product-roadmap.md): staged implementation roadmap.
- [09-glossary.md](09-glossary.md): glossary of legal-tech, filing, and architecture terms.
- [10-validation-research-plan.md](10-validation-research-plan.md): field validation plan and interview guide for advocates and clerks.
- [11-windows-v1-product-requirements.md](11-windows-v1-product-requirements.md): Windows MVP requirements and technical decision record.
- [12-ai-ready-technical-specification-python-native-windows.md](12-ai-ready-technical-specification-python-native-windows.md): strict AI-ready redesign specification for the three-product suite using a Python 3.11 native Windows delivery model.

## V1 Build Handbook

The implementation-focused V1 handbook is separate from this strategic documentation pack:

- [windows-legal-document-vault/v1](../windows-legal-document-vault/v1/documentation/README.md): feature-by-feature build plan, architecture, data model, UI workflows, testing, and iteration log.

## First Build Principle

The first commercial target is a solo advocate or small firm with a Windows PC, printer/scanner, intermittent internet, and limited IT budget.

The product should work even when the internet is unavailable. Documents should remain local by default. Cloud should be treated as encrypted backup only, not as the primary document store.

## E-Filing Boundary

Version 1 does not automate filing into `efiling.court.go.ke`. It prepares a court-ready filing pack that the advocate or clerk manually uploads to the official portal.

This boundary reduces legal, operational, and authentication risk while still solving a real pain: getting files scanned, organized, checked, named, split, indexed, and backed up before filing.

## Legal and Compliance Notice

This documentation is product and technical planning material. It is not legal advice. Kenya data protection, professional responsibility, court filing rules, and judiciary practice requirements should be reviewed by qualified Kenyan counsel before production rollout.
