# Source Notes and Assumptions

This file records the evidence base, reliability notes, and assumptions used across the documentation pack.

All web sources were reviewed on or before June 19, 2026.

## Primary Sources

### Judiciary E-Filing Portal

Source: [https://efiling.court.go.ke/](https://efiling.court.go.ke/)

Observed public features:

- Login and sign-up access.
- Public Information Kiosk.
- Utilities for validating court orders and receipts.
- Deposit tracking.
- Complaint submission.
- Urithi/probate tracking links.
- Support contact details displayed on the public portal.
- Messaging that users can file, track, and manage court cases online.

Product relevance:

- The Windows product should not duplicate the court portal.
- It should prepare documents, preserve firm-owned records, and store portal outputs such as receipts, orders, and notices.

### Judiciary Nationwide Digital Rollout

Source: [All Courts Nationwide Go Digital](https://judiciary.go.ke/judiciary-launches-e-filing-in-all-courts-data-tracking-dashboard-and-causelist-portal-portal/)

Relevant points:

- The Judiciary announced e-filing in all courts nationwide.
- The Chief Justice directed that no court should print pleadings and documents from July 1, 2024.
- The Judiciary also launched a Causelist Portal and a Data Tracking Dashboard.

Product relevance:

- Firms need stronger internal document preparation because electronic documents now sit at the center of court workflow.
- The product should preserve local copies and filing evidence even when the court system handles submission.

### High Court Registry Page

Source: [High Court Registry](https://highcourt.judiciary.go.ke/court-registry/)

Relevant points:

- The High Court page links to the eFiling System and Causelist System.
- It states that the eFiling System enables litigants to file new cases, check case status, and file additional documents in cases.
- It notes use of virtual courts.

Product relevance:

- Windows Legal Document Vault should treat new filings, additional filings, case status outputs, virtual court notices, and cause-list references as matter lifecycle artifacts.

### Judiciary E-Filing Survey Report

Source: [e-Filing and Commercial Justice Sector Reforms Survey Report](https://www.judiciary.go.ke/wp-content/uploads/2023/07/E-Filing-Survey-April-2023-FINAL-2.pdf)

Relevant points:

- E-filing was launched in 2020.
- It is described as a one-stop portal for case registration, filing case documents, retrieving service documents, searching case files and information, and schedules.
- The Judiciary survey mentions remote access for e-filing, case status confirmation, document upload, service to parties, and payments.
- It records challenges such as slow system response and system breakdown.

Product relevance:

- The DMS should maintain local readiness, filing evidence, downtime attempt notes, receipts, and retry history.
- It should not assume the e-filing portal is always available.

### Supreme Court Filing Guidance

Source: [Supreme Court Guidelines for Filing of Documents](https://supremecourt.judiciary.go.ke/guidelines-for-filing-of-documents/)

Relevant points:

- Pleadings and other documents filed in the Court are to be in printed and electronic form.
- The electronic and printed versions should be consistent.
- The page points to the Electronic Case Management Practice Directions, 2020.

Product relevance:

- Windows Legal Document Vault should distinguish draft, signed, printed, scanned, filed, and court-returned versions.
- The filing-pack builder should check document consistency and preserve source-to-output lineage.

### Kenya Law Cause Lists

Source: [Kenya Law Cause Lists](https://new.kenyalaw.org/causelists/)

Relevant points:

- Kenya Law publishes cause lists received from court stations.
- The cause-list database supports archive and search by parties, court divisions, dates, judicial officers, and more.

Product relevance:

- Cause-list references should be stored as matter events or calendar-linked artifacts.
- The DMS should support importing or attaching cause-list PDFs/screenshots/links to matters.

### eCitizen Judiciary Services

Source: [eCitizen Judiciary Services](https://judiciary.ecitizen.go.ke/)

Relevant points:

- Public services include filing a case, checking case status, checking succession cases, verifying court orders, searching advocates, and verifying advocate credentials.

Product relevance:

- The local system should organize outputs from multiple judiciary-facing services, not only the e-filing submission step.

### E-Judiciary App Listing

Source: [E-Judiciary App on Google Play](https://play.google.com/store/apps/details?hl=en_US&id=com.judiciaryofkenya.ejudiciary)

Relevant points:

- The app listing describes access to historical case activities, outcomes, judgments/rulings, payment status, court order verification, and cause lists.
- It states data is encrypted in transit and users can request data deletion.

Product relevance:

- Mobile judiciary services create additional records that firms may need to capture into their own matter vault.
- The DMS should store case activity snapshots, rulings/orders, and payment status evidence.

### ODPC Regulatory Framework

Source: [ODPC Data Protection Laws Kenya](https://www.odpc.go.ke/data-protection-laws-kenya/)

Relevant points:

- ODPC lists the Data Protection Act, 2019 and 2021 regulations as the regulatory framework.
- The Act gives effect to Article 31(c) and (d) of the Constitution and regulates processing of personal data.

Product relevance:

- The system should be designed as privacy-by-default and security-by-default.

### Kenya Data Protection Act

Source: [Kenya Law Data Protection Act No. 24 of 2019](https://new.kenyalaw.org/akn/ke/act/2019/24/eng%402022-12-31)

Relevant points:

- Registration requirements for data controllers and processors depend on thresholds and processing context.
- Personal data must be processed lawfully, fairly, transparently, for explicit purposes, and with minimization.
- Personal data should not be kept longer than necessary.
- Personal data should not be transferred outside Kenya unless adequate safeguards or consent exist.
- Technical and organizational measures should include safeguards such as encryption and the ability to restore access after incidents.
- Breach notification obligations include notice to the Data Commissioner within 72 hours for certain breaches and processor notice to the controller within 48 hours where reasonably practicable.

Product relevance:

- The DMS should support encryption, access controls, audit logs, retention policy, restore tests, and breach evidence logs.

### ODPC Personal Data Protection Handbook

Source: [ODPC Personal Data Protection Handbook](https://www.odpc.go.ke/wp-content/uploads/2024/02/PERSONAL-DATA-PROTECTION-HANDBOOK.pdf)

Relevant points:

- Explains personal data, sensitive personal data, data controller and processor roles, data subject rights, registration, breach reporting, localization/serving-copy guidance, and safeguards.

Product relevance:

- Legal documents frequently include personal and sensitive data.
- Local-first processing should be a deliberate product posture, not only a technical preference.

## Secondary or Supporting Sources

Secondary sources may be used only for market context or practice commentary, not for definitive court requirements.

Examples:

- Legal practitioner articles or guides explaining filing steps.
- Public videos or help pages explaining e-filing workflows.
- Non-official commentary on KRA PIN or account setup.

When secondary sources differ from official sources, official sources control.

## Important Qualification: KRA PIN

Some public guides say e-filing account creation may involve KRA PIN or ID details. The public registration HTML reviewed during research did not expose a KRA PIN field in the static content available without interaction.

Documentation should therefore say:

"Registration may require identity, contact, account-type, and other details shown by the live portal. Users should confirm current required fields on the official portal."

Avoid claiming KRA PIN is always required unless confirmed by the live portal or an official Judiciary guide.

## Important Qualification: Causelist Integration

The Judiciary provides a Causelist Portal and links to cause-list services. Public sources support related digital services, but do not prove a deep technical API integration between the e-filing portal and cause-list system.

Use:

"Related causelist access is available through Judiciary/Kenya Law cause-list services."

Avoid:

"The e-filing portal is technically integrated with the cause-list portal."

## Important Qualification: Case Tracking to Conclusion

The Judiciary announcement says the Data Tracking Dashboard helps Judiciary leaders monitor case processing from filing to conclusion. Public users can check status and case information, but the extent of public tracking can vary by case type, court, and access rights.

Use:

"Users can check exposed case status, schedules, case information, and available case activity."

Avoid:

"All users can fully track every case from filing to conclusion."

## Assumptions

- First commercial target: solo advocates and law firms with 1 to 10 advocates.
- First product: Windows local-first document management and backup.
- Primary buyer concern: preserving confidential legal files locally while still benefiting from OCR, organization, backup, and future AI retrieval.
- Cloud backup is opt-in and encrypted.
- Direct e-filing portal automation is out of scope for version 1.
- The product prepares filing packs for manual upload to `efiling.court.go.ke`.
- This documentation is not legal advice.

## Product Documentation Rules

- Cite official sources for factual claims about Judiciary systems, court filing, or data protection.
- Mark assumptions clearly.
- Separate court-side e-filing from firm-side document management.
- Keep Windows Legal Document Vault, Local Matter RAG Connector, and Wakili-Mkononi Matter AI Integration separate, with explicit integration boundaries.
