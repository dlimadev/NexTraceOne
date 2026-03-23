# NexTraceOne — Notifications Architecture

> **Status:** COMPLETE  
> **Date:** 2026-03-23  
> **Phase:** 0 — Foundation

---

## 1. Architecture Overview

The NexTraceOne Notification Platform follows an event-driven architecture where product modules act as **event producers**, and the notification system acts as an **event consumer** that orchestrates delivery across multiple channels.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        EVENT PRODUCERS (Modules)                        │
│                                                                         │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │
│  │  Operational  │ │   Change     │ │   Catalog    │ │   Identity   │  │
│  │ Intelligence  │ │  Governance  │ │  /Contracts  │ │   & Access   │  │
│  └──────┬───────┘ └──────┬───────┘ └──────┬───────┘ └──────┬───────┘  │
│         │                │                │                │           │
│  ┌──────┴───────┐ ┌──────┴───────┐ ┌──────┴───────┐ ┌──────┴───────┐  │
│  │  Governance   │ │    FinOps    │ │  AI Knowledge│ │ Integrations │  │
│  │  /Compliance  │ │              │ │  /AI Gov     │ │  /Ingestion  │  │
│  └──────┬───────┘ └──────┬───────┘ └──────┬───────┘ └──────┬───────┘  │
│         │                │                │                │           │
└─────────┼────────────────┼────────────────┼────────────────┼───────────┘
          │                │                │                │
          ▼                ▼                ▼                ▼
    ┌─────────────────────────────────────────────────────────────┐
    │                  INotificationModule.SubmitAsync()           │
    │                  (Notification Request Contract)             │
    └─────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
    ┌─────────────────────────────────────────────────────────────┐
    │                 NOTIFICATION ORCHESTRATOR                    │
    │                                                             │
    │  1. Validate request                                        │
    │  2. Resolve recipients (users, roles, teams → user IDs)     │
    │  3. Apply routing rules (severity × category × preferences) │
    │  4. Create Notification entities (one per recipient)         │
    │  5. Persist to Notification Store (central inbox)            │
    │  6. Determine eligible channels per recipient                │
    │  7. Dispatch to channel dispatchers                          │
    │  8. Track delivery status                                    │
    └───┬──────────────┬──────────────┬───────────────────────────┘
        │              │              │
        ▼              ▼              ▼
    ┌────────┐   ┌────────┐   ┌──────────┐
    │ InApp  │   │ Email  │   │  Teams   │
    │ Store  │   │Dispatch│   │ Dispatch │
    └────────┘   └────────┘   └──────────┘
```

---

## 2. Components

### 2.1 Event Producers

Any NexTraceOne module can produce notifications by calling `INotificationModule.SubmitAsync()`. The producer provides:
- Event type and business context
- Category and severity
- Target recipients (explicit users, roles, or teams)
- Action URL for deep linking
- Optional JSON payload for template rendering

**Existing domain events** (e.g., `CostAnomalyDetectedEvent`, `WorkflowRejectedEvent`) can be connected to notification production through integration event handlers in each module's Infrastructure layer.

### 2.2 Notification Orchestrator (`INotificationOrchestrator`)

The central decision-making component:

1. **Validates** the notification request
2. **Resolves recipients**: Converts roles/teams to individual user IDs
3. **Applies routing rules**: Uses `INotificationRoutingEngine` to determine eligible channels per recipient, based on severity, category, and user preferences
4. **Creates entities**: One `Notification` per recipient (persisted in the inbox store)
5. **Dispatches**: Creates `NotificationDelivery` records and invokes `INotificationChannelDispatcher` for each eligible external channel
6. **Tracks delivery**: Updates delivery status based on channel responses

### 2.3 Notification Store (`INotificationStore`)

Persistence layer for the central inbox:
- PostgreSQL via EF Core (following existing NexTraceOne pattern)
- Dedicated `NotificationsDbContext` (module isolation)
- Supports: add, get by ID, list with filters, count unread, mark all as read

### 2.4 Routing Engine (`INotificationRoutingEngine`)

Determines which channels should receive a notification:

```
Input:  (recipientUserId, category, severity)
Output: List<DeliveryChannel>

Rules (default):
  - InApp: ALWAYS included
  - Email: included for ActionRequired, Warning, Critical (unless user opted out)
  - Teams: included for Warning, Critical (unless user opted out)
  - User preferences override defaults
  - Critical severity ignores opt-out for safety channels
```

### 2.5 Channel Dispatchers (`INotificationChannelDispatcher`)

Each external channel implements the dispatcher interface:

| Channel | Implementation | Format |
|---|---|---|
| **Email** | SMTP (builds on existing `EmailAlertChannel` pattern) | HTML email with deep links |
| **Teams** | Incoming Webhook / Adaptive Cards | Structured card with action buttons |

### 2.6 Preference Service (`INotificationPreferenceService`)

Manages user notification preferences:
- Get preferences per user
- Check if a channel is enabled for a category
- Update preferences
- Fallback to platform defaults when no explicit preference exists

---

## 3. Data Flow

### 3.1 Notification Creation Flow

```
Module Event → INotificationModule.SubmitAsync(request)
  → Orchestrator validates request
  → Orchestrator resolves recipients
  → For each recipient:
    → Notification.Create(...) → persisted to store
    → RoutingEngine.ResolveChannels(user, category, severity)
    → For each eligible channel:
      → NotificationDelivery.Create(notificationId, channel)
      → ChannelDispatcher.DispatchAsync(notification, address)
      → Delivery.MarkDelivered() or Delivery.MarkFailed(error)
```

### 3.2 Notification Read Flow

```
User opens notification in UI
  → GET /api/notifications/{id}
  → Notification.MarkAsRead()
  → Domain event: NotificationReadEvent
  → Persisted to store
```

### 3.3 Notification Lifecycle

```
Created (Unread)
  ├── User views → Read
  │    ├── User acknowledges → Acknowledged
  │    │    └── User archives → Archived
  │    ├── User archives → Archived
  │    └── User dismisses → Dismissed
  └── User dismisses → Dismissed
```

---

## 4. Integration with Existing Architecture

### 4.1 Outbox Pattern

Notification creation can leverage the existing outbox pattern:
- When a module publishes an integration event, the outbox processor picks it up
- A notification-specific handler consumes the event and calls `INotificationModule.SubmitAsync()`
- This ensures at-least-once delivery and transactional consistency

### 4.2 Alerting Gateway (Complementary)

The existing `AlertGateway` handles **operational alerts** (Webhook, Email channels for infrastructure monitoring). The notification platform handles **user-facing notifications** (inbox, email, Teams for business events). They are complementary:

| Aspect | AlertGateway | Notification Platform |
|---|---|---|
| Purpose | Infrastructure monitoring | User-facing business notifications |
| Recipients | Ops channels (webhook endpoints) | Individual users by role/team/ownership |
| Persistence | None (fire-and-forget) | Full persistence with lifecycle states |
| User preferences | None | Per-user, per-category, per-channel |
| UI | None | Inbox, notification center, badge counter |

### 4.3 Multi-Tenancy

All notification entities include `TenantId`. Queries are tenant-scoped via the existing `TenantRlsInterceptor` pattern.

### 4.4 Audit Trail

Notification events (`NotificationCreatedIntegrationEvent`, `NotificationDeliveredIntegrationEvent`) can be consumed by the AuditCompliance module for full audit trail.

---

## 5. Database Schema (Planned)

### Tables

```sql
-- Central inbox
notifications (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    recipient_user_id UUID NOT NULL,
    event_type VARCHAR(200) NOT NULL,
    category INTEGER NOT NULL,
    severity INTEGER NOT NULL,
    title VARCHAR(500) NOT NULL,
    message TEXT NOT NULL,
    source_module VARCHAR(100) NOT NULL,
    source_entity_type VARCHAR(100),
    source_entity_id VARCHAR(200),
    environment_id UUID,
    action_url VARCHAR(2000),
    requires_action BOOLEAN NOT NULL DEFAULT FALSE,
    status INTEGER NOT NULL DEFAULT 0,
    payload_json JSONB,
    created_at TIMESTAMPTZ NOT NULL,
    read_at TIMESTAMPTZ,
    acknowledged_at TIMESTAMPTZ,
    archived_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ
);

-- Delivery tracking
notification_deliveries (
    id UUID PRIMARY KEY,
    notification_id UUID NOT NULL REFERENCES notifications(id),
    channel INTEGER NOT NULL,
    recipient_address VARCHAR(500),
    status INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL,
    delivered_at TIMESTAMPTZ,
    failed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0
);

-- User preferences
notification_preferences (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    user_id UUID NOT NULL,
    category INTEGER NOT NULL,
    channel INTEGER NOT NULL,
    enabled BOOLEAN NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    UNIQUE(tenant_id, user_id, category, channel)
);

-- Indexes
CREATE INDEX idx_notifications_recipient_status ON notifications(recipient_user_id, status);
CREATE INDEX idx_notifications_tenant_created ON notifications(tenant_id, created_at DESC);
CREATE INDEX idx_notification_deliveries_notification ON notification_deliveries(notification_id);
CREATE INDEX idx_notification_preferences_user ON notification_preferences(tenant_id, user_id);
```

---

## 6. Email Channel Specification

### Subject Pattern
`[NexTraceOne] {Severity}: {Title}`

Example: `[NexTraceOne] CRITICAL: Incident created — Service Orders degraded in Production`

### Body (HTML)
- Color-coded header by severity (following existing `EmailAlertChannel` pattern)
- Title and message
- Business context: module, entity type, environment
- Deep link button to the source entity
- Footer with NexTraceOne branding

### Priority Events for Email
- Critical incidents
- Pending approvals
- Break-glass activations
- Budget threshold breaches
- Security alerts

---

## 7. Microsoft Teams Channel Specification

### Message Format
Adaptive Card v1.4+ with:
- Color-coded accent by severity
- Title and summary message
- Context fields: module, category, environment, timestamp
- Action button: "View in NexTraceOne" (deep link)

### Priority Events for Teams
- Critical incidents (service health critical)
- Break-glass activations
- Budget 100% exceeded
- AI provider unavailable
- OIDC provider unavailable

### What Should NOT Go to Teams
- Informational notifications
- Routine contract publications
- Low-severity events
- Events where user explicitly opted out

---

## 8. Recipient Resolution Model

### Targeting Types

| Target Type | Resolution |
|---|---|
| Explicit User IDs | Direct — no resolution needed |
| Roles | Resolved via Identity module to user IDs |
| Teams | Resolved via Identity module to team member IDs |
| Service Owner | Resolved via Catalog module to owner user ID |
| Tenant Admins | Resolved via Identity module to admin user IDs |

### Default Routing Rules

| Scenario | Recipients |
|---|---|
| Critical incident | Service owner + ops team + tenant admins |
| Pending approval | Designated approver |
| Break-glass | Tenant admins + security team |
| Budget critical | Service owner + budget owner + manager |
| Compliance fail | Governance team + service owner |
| AI provider down | Platform admins + AI governance team |
| Connector auth failed | Integration owner + platform admins |

---

## 9. Future Architecture Extensions (Post Phase 0)

| Extension | Phase | Description |
|---|---|---|
| Notification Templates | 3 | Template engine for channel-specific rendering |
| Digest/Batching | 6 | Aggregate low-priority notifications into periodic digests |
| Quiet Hours | 6 | Suppress non-critical notifications during configured hours |
| Deduplication | 6 | Prevent duplicate notifications for same event |
| Escalation | 6 | Auto-escalate unacknowledged critical notifications |
| Notification Analytics | 7 | Metrics on delivery, read rates, response times |
| Real-time Push | Future | WebSocket/SSE for real-time inbox updates |
| Mobile Push | Future | Mobile push notifications via Firebase/APNS |
| Slack Channel | Future | Slack delivery channel |
