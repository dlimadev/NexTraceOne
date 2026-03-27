# Phase 7 — Alerting Gateway

## Architecture

The NexTraceOne Alerting Gateway provides a simple, extensible mechanism for sending operational alerts through multiple channels.

```
┌──────────────┐     ┌─────────────────┐     ┌──────────────────┐
│ Alert Source  │────▶│  AlertGateway   │────▶│ WebhookChannel   │──▶ HTTP POST
│ (drift, job, │     │  (IAlertGateway) │     └──────────────────┘
│  health, etc) │     │                 │     ┌──────────────────┐
└──────────────┘     │  Fan-out to all  │────▶│ EmailChannel     │──▶ SMTP
                     │  enabled channels│     └──────────────────┘
                     └─────────────────┘
```

### Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `IAlertGateway` | Abstractions/ | Gateway contract |
| `IAlertChannel` | Abstractions/ | Channel contract |
| `AlertGateway` | AlertGateway.cs | Fan-out dispatcher |
| `WebhookAlertChannel` | Channels/ | HTTP POST webhook |
| `EmailAlertChannel` | Channels/ | SMTP email alerts |
| `AlertPayload` | Models/ | Alert data record |
| `AlertSeverity` | Models/ | Info/Warning/Error/Critical |
| `AlertDispatchResult` | Models/ | Per-channel results |
| `AlertingOptions` | Configuration/ | Webhook + Email config |

## Supported Channels

### Webhook Channel

Sends alerts as JSON HTTP POST to a configurable URL.

**Payload format:**
```json
{
  "title": "Drift Detection Alert",
  "description": "Service payment-api has drifted from baseline",
  "severity": "Warning",
  "source": "DriftDetectionJob",
  "correlationId": "abc-123",
  "timestamp": "2026-03-22T21:00:00Z",
  "context": {
    "service": "payment-api",
    "environment": "production"
  }
}
```

### Email Channel

Sends HTML-formatted emails via SMTP with severity color coding:

| Severity | Color |
|----------|-------|
| Info | Blue (#2196F3) |
| Warning | Orange (#FF9800) |
| Error | Red (#F44336) |
| Critical | Dark Red (#B71C1C) |

## Configuration

Add to `appsettings.json`:

```json
{
  "Alerting": {
    "Enabled": true,
    "Webhook": {
      "Enabled": true,
      "Url": "https://hooks.example.com/alerts",
      "Headers": {
        "Authorization": "Bearer <token>"
      },
      "TimeoutSeconds": 30
    },
    "Email": {
      "Enabled": true,
      "SmtpHost": "smtp.example.com",
      "SmtpPort": 587,
      "UseSsl": true,
      "Username": "alerts@nextraceone.local",
      "Password": "<smtp-password>",
      "FromAddress": "alerts@nextraceone.local",
      "FromName": "NexTraceOne Alerts",
      "Recipients": [
        "ops-team@company.com",
        "oncall@company.com"
      ]
    }
  }
}
```

### Registration

```csharp
// In DependencyInjection or Program.cs
services.AddBuildingBlocksAlerting(configuration);
```

## Usage

### Sending Alerts

```csharp
public sealed class DriftAlertHandler(IAlertGateway alertGateway)
{
    public async Task HandleDriftDetected(string serviceName, CancellationToken ct)
    {
        var alert = new AlertPayload
        {
            Title = $"Drift detected: {serviceName}",
            Description = $"Service {serviceName} has drifted from its baseline configuration.",
            Severity = AlertSeverity.Warning,
            Source = "DriftDetection",
            Context = new Dictionary<string, string>
            {
                ["service"] = serviceName,
                ["environment"] = "production"
            }
        };

        var result = await alertGateway.DispatchAsync(alert, ct);
        // result.Succeeded — true if at least one channel succeeded
        // result.ChannelResults — per-channel success/failure details
    }
}
```

### Sending to Specific Channel

```csharp
var result = await alertGateway.DispatchAsync(alert, "webhook", ct);
```

## Troubleshooting

### Webhook channel returns false
- Check `Alerting:Webhook:Url` is configured and reachable
- Check `Alerting:Webhook:TimeoutSeconds` is sufficient
- Check logs for HTTP status codes from the webhook endpoint

### Email channel returns false
- Check `Alerting:Email:SmtpHost` is configured and reachable
- Check SMTP credentials are correct
- Check `Alerting:Email:Recipients` has at least one entry
- Check firewall allows outbound SMTP (port 587/465/25)

### No alerts being sent
- Check `Alerting:Enabled` is `true`
- Check individual channel `Enabled` flags
- Check logs for `AlertGateway` messages

## Tests

Alerting tests are in `tests/building-blocks/NexTraceOne.BuildingBlocks.Observability.Tests/Alerting/`:

```bash
dotnet test tests/building-blocks/NexTraceOne.BuildingBlocks.Observability.Tests/ --filter "FullyQualifiedName~Alerting"

# Expected: 28 tests passing
```
