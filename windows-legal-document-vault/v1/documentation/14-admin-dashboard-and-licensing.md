# Admin Dashboard and Licensing

## Purpose

The owner admin dashboard supports monetization, install tracking, support, and license control.

It is accessible only to the product owner/admin. It must not become a portal for viewing law firm documents.

## Admin Capabilities

Admin can:

- See registered installations.
- See license/subscription status.
- See app version per install.
- See last check-in time.
- See enabled/disabled/suspended status.
- Enable an installation ID.
- Disable an installation ID.
- Suspend a license.
- Reactivate a license.
- See whether cloud backup add-on is enabled.
- See backup-health summary if cloud backup is enabled.
- Add support notes.

Admin cannot:

- View client documents.
- View OCR text.
- View matter names.
- View client names.
- View case numbers.
- View recovery keys.
- Download vault backups in decrypted form.

## Installation Identity

Each installed app gets:

- `installationId`: random GUID generated on first setup.
- `licenseKey`: user-entered or assigned by admin.
- `deviceNickname`: user-friendly label.
- `firmDisplayName`: firm name entered by user.
- `createdAt`.
- `lastCheckInAt`.

Installation identity is not the same as document identity. It must not include matter or document details.

## License Statuses

Statuses:

- Trial.
- Active.
- Payment due.
- Suspended.
- Disabled.
- Revoked.

Local behavior:

- Trial/Active: app works normally.
- Payment due: app warns user but grace period may allow continued use.
- Suspended: paid features such as cloud backup stop.
- Disabled/Revoked: app stops license-gated features.

Important rule:

- License disablement must never delete local vault data.

## Suggested Admin Dashboard Screens

### Login

- Admin email.
- Password.
- MFA.

### Installations

Columns:

- Installation ID.
- Firm display name.
- Device nickname.
- App version.
- License status.
- Cloud backup status.
- Last check-in.
- Enabled/disabled.

Actions:

- View install.
- Enable.
- Disable.
- Suspend.
- Reactivate.

### Installation Detail

Shows:

- Install metadata.
- License history.
- Check-in history.
- Backup health summary.
- App version.
- Support notes.

Does not show:

- Matters.
- Documents.
- Client files.
- Court data.

### License Management

Shows:

- License key.
- Plan.
- Seats/install limit.
- Expiry or renewal date.
- Add-ons.
- Payment status placeholder.

V1 can support manual payment tracking first, before payment integration exists.

## Windows App License Check

The app should periodically check:

- Installation ID.
- License key.
- App version.
- Enabled/disabled status.
- Feature entitlements.

Check-in payload:

```json
{
  "installationId": "guid",
  "licenseKey": "LICENSE-KEY",
  "firmDisplayName": "Example Advocates LLP",
  "deviceNickname": "Office front desk PC",
  "appVersion": "1.0.0",
  "features": {
    "cloudBackup": true
  },
  "health": {
    "lastLocalBackupAt": "2026-06-19T20:00:00Z",
    "lastCloudBackupAt": "2026-06-19T20:10:00Z",
    "backupStatus": "Healthy"
  }
}
```

Forbidden in check-in payload:

- Matter names.
- Party names.
- Client names.
- Case numbers.
- Document filenames.
- OCR text.
- File hashes that could identify a client document across installs.

## Payment Integration Later

Payment is not implemented yet.

V1 admin can still support monetization by:

- Manually creating license keys.
- Manually marking payment status.
- Setting trial expiry.
- Enabling/disabling install IDs.
- Enabling/disabling paid add-ons such as cloud backup.

Future payment integration can add:

- M-Pesa STK push.
- Card payments.
- Invoices.
- Receipt emails.
- Auto-renewal.
- Automatic license status updates.

## Security Requirements

Admin dashboard must have:

- Admin-only authentication.
- MFA.
- Audit log for enable/disable/suspend actions.
- Least-privilege deployment credentials.
- No document access paths.
- Rate limits on license checks.
- Tamper-resistant installation IDs.

## Acceptance Criteria

Admin/licensing is acceptable when:

- Admin can see installs and license state.
- Admin can enable/disable an installation ID.
- App receives license state on check-in.
- Disabled license stops paid/online features.
- Local vault data remains intact.
- Admin dashboard cannot inspect client documents.

