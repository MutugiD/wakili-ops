# Problem Analysis: Kenyan Legal Document Workflows

## Executive Problem

Kenyan e-filing helps advocates file matters and documents remotely, but it does not solve the law firm's internal document ownership problem.

For solo advocates and small firms, the real operational pain is not only "how do I upload to court?" It is:

- How do I convert bulky physical files into reliable softcopies?
- How do I keep every draft, signed version, filed copy, receipt, order, ruling, and annexure organized by matter?
- How do I know which version was filed, which was served, and which is still a draft?
- How do I retrieve an old affidavit, annexure, authority, or order while away from the office?
- How do I back up confidential client documents without losing control of them?
- How do I later use these documents for agentic legal drafting and RAG without uploading the whole firm archive to an external system?

Windows Legal Document Vault addresses this internal firm-side gap.

## Current Judiciary Digital Context

The official Judiciary e-filing portal is [efiling.court.go.ke](https://efiling.court.go.ke/). Public portal features include account access, public information kiosk, order and receipt validation, deposit tracking, complaints, and probate-related trackers.

The Judiciary announced nationwide e-filing rollout and related digital services, including a Causelist Portal and Data Tracking Dashboard. The announcement also states that courts should not print pleadings and documents from July 1, 2024. Source: [All Courts Nationwide Go Digital](https://judiciary.go.ke/judiciary-launches-e-filing-in-all-courts-data-tracking-dashboard-and-causelist-portal-portal/).

The High Court registry page describes eFiling as enabling litigants to file new cases, check case status, and file additional documents in cases. Source: [High Court Registry](https://highcourt.judiciary.go.ke/court-registry/).

The Judiciary e-filing survey describes e-filing as supporting case registration, filing documents, retrieving service documents, searching case files and information, and schedules. It also records remote access for filing, case status confirmation, uploads, service, and payments. Source: [Judiciary e-Filing Survey Report](https://www.judiciary.go.ke/wp-content/uploads/2023/07/E-Filing-Survey-April-2023-FINAL-2.pdf).

## What E-Filing Solves

E-filing helps with:

- Remote submission of new cases.
- Filing additional documents in existing matters.
- Payment of court fees.
- Court-side receipt and status workflows.
- Access to court-related digital services.
- Reduced physical trips to court registries.
- Lower dependence on manual registry filing for many workflows.

This is a major improvement for access, speed, and accountability.

## What E-Filing Does Not Solve for Law Firms

E-filing does not give the advocate a complete private document operations system.

### 1. Physical Files Still Need Conversion

Many firms still hold legacy physical files, signed affidavits, annexures, letters, receipts, copies of IDs, land records, contracts, photographs, and court documents.

These must be scanned, cleaned, named, OCRed, stored, linked to matters, and later retrieved. A court portal upload step does not create a durable firm archive.

### 2. Softcopy Ownership Is Fragmented

Small firms often store documents across:

- One advocate's laptop.
- A clerk's desktop.
- WhatsApp.
- Email attachments.
- Flash disks.
- Scanner folders.
- OneDrive or Google Drive folders.
- Downloads from e-filing.
- Physical files that were never scanned.

The result is weak ownership, duplication, missing filed copies, and uncertainty about which version is authoritative.

### 3. Version Drift Is Common

Legal documents naturally move through stages:

- Draft.
- Internal review.
- Client-approved version.
- Signed version.
- Scanned signed copy.
- Filed copy.
- Served copy.
- Amended version.
- Court-rejected or corrected version.

Without version sessions, staff may file or serve the wrong copy, overwrite old drafts, or lose the connection between a filed pleading and its supporting annexures.

### 4. Filing Readiness Is Manual

The Electronic Case Management Practice Directions and court guidance emphasize PDF documents, formatting, scan quality, separate files, signatures, and filing notices. The Supreme Court guidance also stresses consistency between electronic and printed versions. Source: [Supreme Court Guidelines for Filing of Documents](https://supremecourt.judiciary.go.ke/guidelines-for-filing-of-documents/).

Small firms often check these manually:

- Is it PDF?
- Is the scan readable?
- Is it too large?
- Is the annexure separate or combined?
- Is it signed?
- Is it the correct matter?
- Is the receipt saved?
- Has the court returned a notice, order, or rejection?

This creates avoidable filing errors and delays.

### 5. Internet and Portal Downtime Are Operational Risks

The Judiciary survey records challenges such as slow system response and system breakdown. Source: [Judiciary e-Filing Survey Report](https://www.judiciary.go.ke/wp-content/uploads/2023/07/E-Filing-Survey-April-2023-FINAL-2.pdf).

The law firm needs local readiness even when the portal is unavailable:

- Documents should be prepared offline.
- Filing packs should be ready before login.
- Failed upload attempts and retry notes should be preserved.
- Receipts and notices should be saved immediately after successful filing.

### 6. Case Outputs Are Not Captured Into a Firm Knowledge Base

Court outputs may include:

- Filing receipts.
- Payment receipts.
- Notices of electronic filing.
- Court orders.
- Rulings.
- Judgments.
- Cause-list entries.
- Hearing links.
- Service notices.
- Registry messages.

These outputs are valuable matter records, but they often remain in email, browser downloads, screenshots, or the portal. They should be stored in the matter vault.

### 7. Confidentiality Makes Cloud-First Unattractive

Legal files contain personal data, sensitive personal data, privileged communications, financial records, family information, criminal allegations, health details, land records, IDs, company records, and client instructions.

Kenya's Data Protection Act requires data protection principles, safeguards, security measures, and restrictions around unnecessary processing and transfer. Source: [Kenya Data Protection Act](https://new.kenyalaw.org/akn/ke/act/2019/24/eng%402022-12-31). The ODPC handbook also emphasizes controller/processor roles, privacy principles, and safeguarding personal data. Source: [ODPC Personal Data Protection Handbook](https://www.odpc.go.ke/wp-content/uploads/2024/02/PERSONAL-DATA-PROTECTION-HANDBOOK.pdf).

For this market, local-first is not a luxury feature. It is part of trust.

## Stakeholder Pain Points

### Solo Advocate

- Needs quick scanning and retrieval.
- Often works away from the office or court.
- May rely on one laptop and one assistant.
- Needs low monthly cost.
- Needs confidence that files are backed up.
- Does not want client files automatically uploaded to an unknown cloud.

### Small Firm Partner

- Needs matter visibility across staff.
- Needs to know what has been filed, served, and paid.
- Needs auditability when clerks or juniors handle filings.
- Needs a recoverable archive if a PC fails.

### Advocate Clerk or Legal Assistant

- Needs a simple intake inbox.
- Needs checklists for court-ready PDFs.
- Needs clear naming and filing-pack instructions.
- Needs to attach receipts, notices, and court orders to the right matter.

### Client

- Expects the advocate to preserve documents safely.
- May request copies urgently.
- Expects confidentiality.
- Expects the firm to know what was filed and when.

## Product Opportunity

The product opportunity is not to compete with the Judiciary e-filing portal. The opportunity is to become the advocate's local command center before and after filing.

The product should:

- Prepare documents for e-filing.
- Preserve the firm's local source of truth.
- Capture the evidence of what was filed.
- Keep matter files searchable.
- Back up documents safely.
- Later allow agentic drafting from the firm's own document base.

## Success Definition

Windows Legal Document Vault succeeds if a small firm can:

- Install the Windows app without an IT consultant.
- Scan or import documents into a matter.
- Convert scanned pages into searchable PDFs.
- Identify document type and filing status.
- Track versions from draft to filed copy.
- Generate a court-ready filing pack.
- Manually upload the pack to e-filing.
- Store receipts, court orders, and notices back into the matter.
- Search across matter files.
- Recover documents after PC failure.
- Keep documents local unless backup is explicitly enabled.

## Core Thesis

Kenyan e-filing digitizes the court-facing submission workflow. Wakili's document platform should digitize the law firm-facing ownership workflow.
