# Product Roadmap

## Roadmap Principle

Build trust before intelligence.

The first product must solve document ownership, scanning, filing-pack preparation, and backup before asking firms to rely on AI retrieval or drafting.

## Phase 0: Documentation and Validation

Goal: confirm product scope and buyer problem before coding.

Deliverables:

- Documentation pack.
- Source-backed problem analysis.
- Windows Legal Document Vault workflow spec.
- Architecture options.
- Compliance posture.
- Interview guide for advocates/clerks.
- MVP acceptance criteria.

Validation questions:

- Do small firms want local-first over cloud-first?
- What scanner workflows do they use?
- Which folders currently hold matter files?
- How do they name filings?
- Who files on the portal?
- What documents are most often misplaced?
- How often do they need old filed copies?
- What backup method do they trust?

## Phase 1: Windows Legal Document Vault MVP

Goal: usable Windows document vault and filing-pack tool.

Core features:

- Windows installer.
- Local vault.
- Matter creation.
- File import.
- Watched scan folder.
- Basic OCR.
- Document classification.
- Full-text search.
- Version statuses.
- Filing-pack builder.
- Local/external encrypted backup.
- Receipt/court-output attachment.
- Audit log.

Acceptance criteria:

- A solo advocate can create a matter, import scans, OCR, prepare a filing pack, manually file, attach receipt, search later, and restore from backup.

## Phase 2: Windows Legal Document Vault Beta for Small Firms

Goal: make MVP reliable for daily office use.

Add:

- Better scanner integrations.
- Office shared folder/NAS mode.
- Basic user roles.
- Improved duplicate detection.
- Better document-type classification.
- Restore drill wizard.
- Matter export.
- Filing-pack templates by filing type.
- Better court-output capture.

Acceptance criteria:

- A small firm can use the app across advocate and clerk workflows without losing version clarity.

## Phase 3: Compliance and Operational Hardening

Goal: improve trust, auditability, and resilience.

Add:

- Stronger encryption key recovery workflow.
- Sensitive matter flag.
- Retention policy configuration.
- Breach incident report.
- Backup integrity dashboard.
- Audit export.
- Filing-pack cleanup reminders.
- Installer update mechanism.

Acceptance criteria:

- Firm owner can explain where documents live, who accessed them, when they were backed up, and how they can be restored.

## Phase 4: Local Matter RAG Connector

Goal: add local retrieval without changing document ownership.

Add:

- Local indexer.
- Matter-scoped retrieval.
- Draft include/exclude.
- Citation returns.
- Local API.
- Hybrid keyword/vector search.
- Sensitive matter controls.

Acceptance criteria:

- User can ask a matter question and receive cited answer snippets from local documents.

## Phase 5: Wakili-Mkononi Matter AI Integration

Goal: connect Wakili-Mkononi to local matter context.

Add:

- Local connector bridge.
- Matter pairing.
- Wakili retrieval tools.
- Cited drafting context.
- Draft save-back.
- Audit log for Wakili retrieval.

Acceptance criteria:

- Wakili can draft from matter-specific, cited local context without broad cloud document custody.

## Phase 6: Advanced Filing Operations

Goal: improve filing support while keeping human control.

Possible features:

- Browser assistant for upload checklist.
- Portal receipt watcher.
- Better downtime attempt logs.
- Court-specific filing-pack profiles.
- Calendar/cause-list capture.
- Registry correction workflow.

Direct portal automation should remain out of scope unless there is clear legal, technical, and user authorization.

## Commercial Packaging

### Solo Plan

- One Windows device.
- Local vault.
- External backup.
- Filing-pack builder.
- Basic OCR.

### Small Firm Plan

- Multiple users.
- Shared vault path.
- Role-based access.
- Audit exports.
- Backup dashboard.

### Add-On: Encrypted Cloud Backup

- Opt-in.
- Client-side encrypted.
- Restore support.

### Add-On: Local RAG

- Matter-scoped retrieval.
- Citations.
- Wakili integration later.

## Major Risks

### Adoption Risk

Small firms may resist changing folder habits.

Mitigation:

- Support import from existing folders.
- Preserve exportable folder structure.
- Keep UI simple.

### Trust Risk

Users may fear cloud leakage.

Mitigation:

- Local-first default.
- Cloud backup opt-in only.
- Clear encryption language.

### OCR Quality Risk

Old scans may be hard to read.

Mitigation:

- Show OCR confidence.
- Preserve original scans.
- Allow manual correction notes.

### Filing Rule Risk

Court requirements can vary or change.

Mitigation:

- Make filing-pack rules configurable.
- Cite official sources.
- Include "confirm current portal requirements" reminders.

### Backup Misunderstanding Risk

Users may assume backup means filed.

Mitigation:

- Clear status separation:
  - Draft.
  - Prepared.
  - Filed.
  - Backed up.

## Build Sequence Recommendation

1. Documentation review.
2. Advocate/clerk interviews.
3. Clickable prototype.
4. Local vault proof of concept.
5. OCR/import proof of concept.
6. Filing-pack export proof of concept.
7. Backup/restore proof of concept.
8. Private beta with 2 to 5 firms.
9. Harden based on real filing workflows.
10. Add RAG only after document trust is established.
