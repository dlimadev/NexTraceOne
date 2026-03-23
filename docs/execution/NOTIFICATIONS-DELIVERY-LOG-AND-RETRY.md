# Notifications — Delivery Log and Retry Policy

## Overview

Every external delivery attempt (Email, Teams) is persisted as a `NotificationDelivery` record, providing complete auditability and observability of the notification pipeline.

## Delivery Model

### Entity: `NotificationDelivery`

| Field | Type | Description |
|---|---|---|
| `Id` | `NotificationDeliveryId` | Unique identifier |
| `NotificationId` | `NotificationId` | Reference to the originating notification |
| `Channel` | `DeliveryChannel` | Channel used (Email, MicrosoftTeams) |
| `RecipientAddress` | `string?` | Destination address (email, webhook URL) |
| `Status` | `DeliveryStatus` | Current delivery status |
| `CreatedAt` | `DateTimeOffset` | When the delivery record was created |
| `DeliveredAt` | `DateTimeOffset?` | When delivery succeeded |
| `FailedAt` | `DateTimeOffset?` | When the last failure occurred |
| `ErrorMessage` | `string?` | Error message from the last failure |
| `RetryCount` | `int` | Number of delivery attempts made |

### Persistence

- Table: `ntf_deliveries`
- Indexes on: `NotificationId`, `Status`, `Channel`, `(Status, RetryCount)`

## Delivery Status

| Status | Description |
|---|---|
| `Pending` | Delivery queued, awaiting processing |
| `Delivered` | Successfully delivered to the external channel |
| `Failed` | All retry attempts exhausted, permanently failed |
| `Skipped` | Delivery skipped (channel disabled, no recipient, opt-out) |

## Retry Policy

### Configuration

```json
{
  "Notifications": {
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelaySeconds": 30
    }
  }
}
```

### Behavior

| Parameter | Default | Description |
|---|---|---|
| `MaxAttempts` | 3 | Maximum delivery attempts (including first) |
| `BaseDelaySeconds` | 30 | Base delay between retries (multiplied by attempt number) |

### Retry Flow

```
Attempt 1 → Success → Mark Delivered
Attempt 1 → Fail → Wait 30s → Attempt 2
Attempt 2 → Fail → Wait 60s → Attempt 3
Attempt 3 → Fail → Mark Failed (permanent)
```

### Error Classification

- **Transient errors** (retried): Network timeouts, HTTP 5xx, SMTP connection failures
- **Permanent errors** (not retried): Channel disabled, missing config, invalid recipient
- **Skipped**: Dispatcher returns `false` (channel disabled, no recipient address)

## Delivery Lifecycle

```
┌──────────┐     ┌──────────┐     ┌───────────┐
│ Pending   │────▶│ Delivered │     │ Skipped   │
│           │     └──────────┘     └───────────┘
│           │                           ▲
│           │──────────────────────────┘
│           │     (dispatcher returns false)
│           │
│           │     ┌──────────┐
│           │────▶│ Failed   │  (after MaxAttempts)
└──────────┘     └──────────┘
```

## Observability of Failures

Every failed delivery records:
- `ErrorMessage`: The exception message from the last attempt
- `FailedAt`: Timestamp of the last failure
- `RetryCount`: Total attempts made

Logs are emitted at:
- `Information` level for successful deliveries
- `Warning` level for retried failures
- `Error` level for permanent failures

### Log Patterns

```
External delivery succeeded: channel=Email, notification={Id}, attempt=1
External delivery attempt 1/3 failed: channel=Email, notification={Id}
External delivery permanently failed after 3 attempts: channel=Email, notification={Id}
```

## Querying Delivery History

The `INotificationDeliveryStore` abstraction provides:

```csharp
// Get all deliveries for a notification
Task<IReadOnlyList<NotificationDelivery>> ListByNotificationIdAsync(
    NotificationId notificationId, CancellationToken ct);

// Get pending deliveries eligible for retry
Task<IReadOnlyList<NotificationDelivery>> ListPendingForRetryAsync(
    int maxRetryCount, int batchSize, CancellationToken ct);
```
