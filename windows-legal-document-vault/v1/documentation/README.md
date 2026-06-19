# Windows Legal Document Vault V1 Build Handbook

This folder is the implementation handbook for V1 of the Windows Legal Document Vault.

The broad product, research, compliance, and Kenyan Judiciary context lives in `documentation/`. This folder is intentionally narrower: it breaks the Windows V1 product into buildable modules, feature slices, architecture decisions, data/storage rules, UI workflows, and test expectations.

## V1 Objective

Build a Windows-first, local-first legal document vault for Kenyan solo advocates and small firms.

V1 must let a user:

- Install and run the app on Windows.
- Create an encrypted local vault.
- Configure a watched scanner folder.
- Create and manage matters.
- Import PDFs/images/documents into a matter.
- Classify documents and track versions.
- Prepare filing packs for manual upload to the Judiciary e-filing portal.
- Attach receipts and court outputs after filing.
- Back up and restore the vault.

## Product Boundaries

V1 is not:

- A direct e-filing automation bot.
- A cloud-first document store.
- A billing or practice management suite.
- A legal advice engine.
- A RAG or AI drafting system.

The Local Matter RAG Connector and Wakili-Mkononi Matter AI Integration remain future layers that consume the document vault after the vault itself is reliable.

## Locked Defaults

- Platform: Windows 10 and Windows 11, 64-bit.
- App stack: .NET 10 LTS + WPF.
- Metadata store: SQLite.
- Search: SQLite FTS for V1.
- Document storage: encrypted local vault objects.
- Scanner support: watched folders first.
- Cloud backup: optional add-on, client-side encrypted, never primary storage.
- Admin/licensing dashboard: owner-only control plane for install tracking, enable/disable status, and monetization controls without access to client documents.
- Filing: prepare filing pack only; user manually uploads to `efiling.court.go.ke`.

## Documentation Map

- [01-overview-and-scope.md](01-overview-and-scope.md)
- [02-technical-architecture.md](02-technical-architecture.md)
- [03-module-breakdown.md](03-module-breakdown.md)
- [04-data-model-and-storage.md](04-data-model-and-storage.md)
- [05-security-encryption-backup.md](05-security-encryption-backup.md)
- [06-ui-workflows.md](06-ui-workflows.md)
- [07-feature-slices-and-acceptance-tests.md](07-feature-slices-and-acceptance-tests.md)
- [08-testing-installation-strategy.md](08-testing-installation-strategy.md)
- [09-iteration-log.md](09-iteration-log.md)
- [10-code-implementation-plan.md](10-code-implementation-plan.md)
- [11-local-windows-installation.md](11-local-windows-installation.md)
- [12-end-to-end-document-testing.md](12-end-to-end-document-testing.md)
- [13-cloud-backup-option.md](13-cloud-backup-option.md)
- [14-admin-dashboard-and-licensing.md](14-admin-dashboard-and-licensing.md)
- [15-ci-cd-and-security-gates.md](15-ci-cd-and-security-gates.md)

## Build Rule

Each feature slice must be documented, implemented, tested, and manually verified before the next slice starts. The project should stay usable after each slice, even if the feature set is still small.
