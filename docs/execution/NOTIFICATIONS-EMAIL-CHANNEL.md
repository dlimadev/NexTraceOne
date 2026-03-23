# Notifications — Email Channel

## Overview

The Email channel delivers HTML-formatted notifications via SMTP. It is designed for critical and action-required events that need to reach users outside the NexTraceOne UI.

## Configuration

Configuration is provided via `appsettings.json` or environment variables under the `Notifications:Channels:Email` section.

### Settings

| Setting | Type | Default | Description |
|---|---|---|---|
| `Enabled` | bool | `false` | Enable/disable the email channel |
| `SmtpHost` | string | `null` | SMTP server hostname |
| `SmtpPort` | int | `587` | SMTP server port |
| `UseSsl` | bool | `true` | Use SSL/TLS for SMTP connection |
| `Username` | string | `null` | SMTP authentication username |
| `Password` | string | `null` | SMTP authentication password |
| `FromAddress` | string | `null` | Sender email address |
| `FromName` | string | `NexTraceOne` | Sender display name |
| `BaseUrl` | string | `https://app.nextraceone.com` | Base URL for deep links |

### Example Configuration

```json
{
  "Notifications": {
    "Channels": {
      "Email": {
        "Enabled": true,
        "SmtpHost": "smtp.example.com",
        "SmtpPort": 587,
        "UseSsl": true,
        "Username": "notifications@example.com",
        "Password": "STORED_IN_SECRETS",
        "FromAddress": "noreply@example.com",
        "FromName": "NexTraceOne",
        "BaseUrl": "https://nextraceone.example.com"
      }
    }
  }
}
```

### Environment Variables

```bash
Notifications__Channels__Email__Enabled=true
Notifications__Channels__Email__SmtpHost=smtp.example.com
Notifications__Channels__Email__SmtpPort=587
Notifications__Channels__Email__Username=user
Notifications__Channels__Email__Password=STORED_IN_SECRETS
Notifications__Channels__Email__FromAddress=noreply@example.com
```

> **Security**: Never commit SMTP credentials to source code. Use secrets manager or environment variables.

## Template

Each email includes:
- **Subject**: `[NexTraceOne] [{Severity}] {Title}`
- **HTML body**: Branded email with severity-colored header, title, message, context table, action button, and footer
- **Plain text alternative**: Fallback for email clients without HTML support

### Template Structure

```
┌──────────────────────────────────┐
│ [Severity Color Bar]             │
│ NexTraceOne — {Severity}         │
├──────────────────────────────────┤
│ {Title}                          │
│                                  │
│ {Message}                        │
│                                  │
│ Category: {Category}             │
│ Severity: {Severity}             │
│ Source:   {SourceModule}         │
│ Entity:   {Type} / {Id}         │
│ Time:     {CreatedAt} UTC        │
│                                  │
│ [Take Action / View Details]     │
├──────────────────────────────────┤
│ Automated notification. Do not   │
│ reply.                           │
└──────────────────────────────────┘
```

## Priority Event Types

| Event | Severity | Email |
|---|---|---|
| ApprovalPending | ActionRequired | ✅ |
| ApprovalRejected | Warning | ✅ |
| IncidentCreated | Critical | ✅ |
| IncidentEscalated | Critical | ✅ |
| BreakGlassActivated | Critical | ✅ |
| ComplianceCheckFailed | Warning | ✅ |
| BudgetExceeded | Warning | ✅ |
| IntegrationFailed | Warning | ✅ |
| AiProviderUnavailable | Warning | ✅ |

## Troubleshooting

| Problem | Cause | Solution |
|---|---|---|
| Emails not sent | Channel disabled | Set `Email:Enabled=true` |
| Connection refused | Wrong SMTP host/port | Verify `SmtpHost` and `SmtpPort` |
| Authentication failed | Wrong credentials | Verify `Username` and `Password` |
| Missing From address | Not configured | Set `FromAddress` |
| No deep links | Wrong BaseUrl | Set `BaseUrl` to the production URL |
| Delivery marked as Failed | SMTP error | Check delivery log and SMTP server logs |
