# Validation Research Plan

## Purpose

This document defines how to validate the documentation pack before writing product code.

The immediate question is not whether a document management system can be built. It can. The real question is whether solo advocates and small firms will trust and use a Windows local-first tool enough to replace scattered scanner folders, WhatsApp documents, external drives, and ad hoc cloud folders.

## Validation Goals

Validation should answer:

- Which document pain is most urgent: scanning, retrieval, filing readiness, backup, version control, or AI drafting?
- Who actually handles each step: advocate, clerk, secretary, pupil, or external cyber/support person?
- Where are documents stored today?
- How do firms currently prepare files for e-filing?
- What document mistakes cause real cost or embarrassment?
- What backup method do firms already trust?
- What data are firms willing to send to the cloud, if any?
- What would make the tool feel safe enough to install?
- What should Windows Legal Document Vault solve before Local Matter RAG Connector and Wakili-Mkononi Matter AI Integration are introduced?

## Target Interviewees

### Primary

- Solo advocates.
- Advocates in firms with 1 to 10 advocates.
- Clerks and legal assistants who scan, file, upload, or retrieve matter documents.

### Secondary

- Office administrators.
- Litigation associates.
- Conveyancing assistants.
- IT support providers serving small law firms.

### Not Targeted for First Validation

- Large firms with existing enterprise document management.
- Judiciary staff.
- Corporate legal departments.
- Self-represented litigants.

## Recommended Sample

For the first validation cycle:

- 6 solo advocates.
- 4 small-firm partners or senior advocates.
- 6 clerks/legal assistants.
- 2 people who regularly troubleshoot scanners or filing operations.

This is enough to identify workflow patterns without pretending to produce statistical certainty.

## Interview Format

Use a 45 to 60 minute structured interview.

Ask for permission to discuss workflows without collecting confidential client facts. Do not ask interviewees to disclose live client data, privileged documents, passwords, or portal credentials.

Preferred flow:

1. Describe current filing and document workflow.
2. Walk through a recent matter from instructions to filing.
3. Identify where files were scanned, edited, uploaded, stored, and backed up.
4. Discuss failures or near-misses.
5. Show a simple concept of the Windows local-first DMS.
6. Test willingness to pay, install, and trust.

## Interview Questions

### Firm Profile

- How many advocates and support staff are in the firm?
- What type of matters do you handle most often?
- How many active matters do you typically manage?
- What Windows devices, scanners, and printers do you use?
- Do staff share one machine, separate machines, or a shared folder?

### Current Document Storage

- Where are active matter documents stored today?
- Where are old closed matter documents stored?
- Do you use OneDrive, Google Drive, Dropbox, external drives, or an office server?
- Which folders are most trusted?
- Which storage location causes the most confusion?
- What happens when an advocate or clerk is out of office and another person needs a file?

### Scanning and OCR

- Who scans documents?
- Which scanner software do you use?
- Where do scanned files land by default?
- Are scanned documents searchable today?
- How often do you need to re-scan because of poor quality?
- What happens to original paper files after scanning?

### E-Filing Workflow

- Who logs into the Judiciary e-filing portal?
- What documents do you usually prepare before logging in?
- What causes upload delays?
- Do you check file size, PDF format, scan quality, signatures, and document naming before upload?
- Where do you save receipts, notices, payment confirmations, and court-generated documents?
- Have you ever lost track of whether a document was only prepared, actually filed, or served?

### Version Control

- How do you know which draft is final?
- How do you distinguish editable drafts, signed copies, scanned signed copies, filed copies, and served copies?
- Have you ever filed, served, or sent the wrong version?
- Do you preserve rejected or corrected versions?
- Who is allowed to mark a document as filed or served?

### Backup and Recovery

- What is your backup method today?
- When was the last time you restored a document from backup?
- What would happen if the main office PC failed today?
- Would you trust encrypted cloud backup if files are encrypted before upload?
- Would you prefer external drive backup, office NAS backup, cloud backup, or all three?

### Data Protection and Trust

- What types of documents would you never want automatically uploaded?
- Would you install a tool that works locally and keeps documents on the PC by default?
- What consent screen would make cloud backup acceptable?
- Who in the office should access sensitive matters?
- What audit trail would you want to see?

### AI and RAG Readiness

- Would you first want search and retrieval, or automated drafting?
- Which matter questions would you ask an AI assistant?
- Would you allow AI to use drafts, filed documents only, or selected documents only?
- Do you need every AI answer to cite source documents and page references?
- Would Wakili be more useful if it could search your local matter record without uploading the whole archive?

### Buying and Adoption

- Who would approve purchase?
- What monthly price would be acceptable for a solo advocate?
- What would be acceptable for a small firm?
- Would you prefer one-time license, monthly subscription, or subscription plus backup add-on?
- What would stop you from installing it?
- What would make you recommend it to another advocate?

## Workflow Observation Checklist

Where possible, observe a non-confidential sample workflow:

- Scanner output folder.
- Current matter folder structure.
- Naming conventions.
- How PDFs are split or combined.
- How receipts are saved.
- How staff find old documents.
- How files are transferred when someone is away.
- How backups are performed.

Record only process notes. Do not copy client documents.

## Prototype Validation Tasks

When a clickable prototype exists, ask users to perform:

1. Create a new matter.
2. Import a scanned PDF.
3. Classify it as affidavit, annexure, receipt, order, or pleading.
4. Search for a phrase inside the document.
5. Mark a signed version.
6. Prepare a filing pack.
7. Attach a receipt after filing.
8. Check backup status.
9. Export a matter folder.

Success means the user can complete these without explanation beyond ordinary labels and controls.

## Evidence to Capture

Capture:

- Quotes about current pain.
- Current folder patterns.
- Common document categories.
- Scanner types and constraints.
- Filing-pack mistakes.
- Backup fears.
- Trust objections.
- Price sensitivity.
- Must-have MVP features.
- Features to defer.

Avoid capturing:

- Client names.
- Case numbers.
- Privileged facts.
- Login credentials.
- Copies of real pleadings or evidence.

## Decision Gates

### Gate 1: Problem Intensity

Proceed if at least half of interviewees report repeated pain in scanning, retrieval, version control, filing readiness, or backup.

### Gate 2: Local-First Trust

Proceed with local-first if users clearly prefer local storage or express concern about uncontrolled cloud upload.

### Gate 3: MVP Scope

Proceed to build only the smallest workflow that users rank as urgent:

- Matter vault.
- Scan/import.
- OCR/search.
- Version status.
- Filing pack.
- Receipt capture.
- Backup/restore.

### Gate 4: RAG Timing

Do not build Local Matter RAG Connector first unless users already have organized document sources and mainly need retrieval. For most small firms, Windows Legal Document Vault should come first.

## Validation Output

After interviews, produce:

- Findings summary.
- Persona updates.
- Top 10 document pains.
- MVP feature ranking.
- Pricing assumptions.
- Trust and compliance objections.
- Updated architecture decisions.
- Go/no-go recommendation for Windows Legal Document Vault MVP.

## Research Ethics

The validation process must respect legal confidentiality.

Use process examples, redacted samples, or synthetic documents. Never ask an advocate or clerk to expose live client documents just to validate product assumptions.

