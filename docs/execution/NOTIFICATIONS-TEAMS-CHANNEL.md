# Notifications — Microsoft Teams Channel

## Overview

The Teams channel delivers notifications as Adaptive Cards via Incoming Webhooks. It is designed for critical operational events that require team-wide awareness.

## Configuration

Configuration is provided via `appsettings.json` or environment variables under the `Notifications:Channels:Teams` section.

### Settings

| Setting | Type | Default | Description |
|---|---|---|---|
| `Enabled` | bool | `false` | Enable/disable the Teams channel |
| `WebhookUrl` | string | `null` | Incoming Webhook URL for Microsoft Teams |
| `TimeoutSeconds` | int | `30` | HTTP timeout for webhook calls |
| `BaseUrl` | string | `https://app.nextraceone.com` | Base URL for deep links in cards |

### Example Configuration

```json
{
  "Notifications": {
    "Channels": {
      "Teams": {
        "Enabled": true,
        "WebhookUrl": "https://tenant.webhook.office.com/webhookb2/...",
        "TimeoutSeconds": 30,
        "BaseUrl": "https://nextraceone.example.com"
      }
    }
  }
}
```

### Environment Variables

```bash
Notifications__Channels__Teams__Enabled=true
Notifications__Channels__Teams__WebhookUrl=https://tenant.webhook.office.com/webhookb2/...
Notifications__Channels__Teams__TimeoutSeconds=30
```

> **Security**: Never commit webhook URLs to source code. Use secrets manager or environment variables.

## Payload Format — Adaptive Card v1.4

Each Teams notification uses an Adaptive Card with:

### Structure

```json
{
  "type": "message",
  "attachments": [{
    "contentType": "application/vnd.microsoft.card.adaptive",
    "content": {
      "type": "AdaptiveCard",
      "version": "1.4",
      "body": [
        { "type": "TextBlock", "text": "🔴 {Title}", "weight": "Bolder" },
        { "type": "TextBlock", "text": "**Severity:** {Severity} | **Category:** {Category}" },
        { "type": "TextBlock", "text": "{Message}" },
        { "type": "FactSet", "facts": [
          { "title": "Source", "value": "{SourceModule}" },
          { "title": "Time", "value": "{CreatedAt} UTC" },
          { "title": "Entity", "value": "{EntityType} / {EntityId}" }
        ]}
      ],
      "actions": [{
        "type": "Action.OpenUrl",
        "title": "Take Action",
        "url": "{BaseUrl}{ActionUrl}"
      }]
    }
  }]
}
```

### Severity Indicators

| Severity | Emoji |
|---|---|
| Critical | 🔴 |
| Warning | 🟠 |
| ActionRequired | 🔵 |
| Info | 🟢 |

## Priority Event Types

| Event | Severity | Teams |
|---|---|---|
| IncidentCreated | Critical | ✅ |
| IncidentEscalated | Critical | ✅ |
| BreakGlassActivated | Critical | ✅ |
| ComplianceCheckFailed | Warning | ✅ |
| BudgetExceeded | Warning | ✅ |
| IntegrationFailed | Warning | ✅ |
| AiProviderUnavailable | Warning | ✅ |

## Setting Up the Incoming Webhook

1. Open Microsoft Teams
2. Navigate to the target channel
3. Click `...` → `Connectors` → `Incoming Webhook`
4. Name the webhook (e.g., "NexTraceOne Alerts")
5. Copy the generated URL
6. Configure it in `Notifications:Channels:Teams:WebhookUrl`

## Troubleshooting

| Problem | Cause | Solution |
|---|---|---|
| Messages not sent | Channel disabled | Set `Teams:Enabled=true` |
| Webhook URL missing | Not configured | Set `WebhookUrl` |
| HTTP 400 error | Invalid payload | Check logs for response body |
| HTTP 404 error | Wrong webhook URL | Verify the URL in Teams settings |
| Timeout | Network or Teams issue | Increase `TimeoutSeconds` or check network |
| Delivery marked as Failed | Webhook error | Check delivery log for error details |
