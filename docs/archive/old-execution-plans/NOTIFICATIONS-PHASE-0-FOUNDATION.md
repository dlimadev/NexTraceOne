# NexTraceOne — Notifications Phase 0: Foundation & Official Platform Model

> **Status:** COMPLETE  
> **Date:** 2026-03-23  
> **Author:** Principal Staff Engineer / Enterprise Notification Architect  
> **Phase:** 0 — Foundation & Official Model Definition

---

## 1. Executive Summary

This document formalizes Phase 0 of the NexTraceOne Notification Platform — the foundational blueprint that defines the official model for events, categories, severities, recipients, routing, channels, entities, and architecture.

**Phase 0 is not an implementation phase.** It is the design and formalization of the notification capability, ensuring all subsequent phases can be implemented without ambiguity.

### Key Decisions

1. **Event-driven architecture**: Modules publish domain/integration events; the Notification Orchestrator decides if, who, and how to notify.
2. **Central inbox is a product capability**: Not an accessory — it is the primary notification experience.
3. **External channels are extensions**: Email and Microsoft Teams are delivery channels, not core domain logic.
4. **Context-rich notifications**: Every notification carries full business context (service, contract, incident, environment, action URL).
5. **Multi-tenant by design**: All entities include `TenantId` for tenant isolation.
6. **Preferences and routing modeled from day one**: Even before full implementation, the model supports user/team preferences and routing rules.

---

## 2. Scope of the Notification Platform

### What the Platform Covers

| Capability | Description |
|---|---|
| **Internal Inbox** | Central notification center within the product UI |
| **Email Channel** | SMTP-based email delivery for critical and actionable notifications |
| **Microsoft Teams Channel** | Teams webhook/Adaptive Card delivery for operational alerts |
| **Event Catalog** | Official catalog of product events that generate notifications |
| **Routing Engine** | Rules-based routing by severity, category, role, team, and preferences |
| **Preference Management** | User-level opt-in/opt-out per category and channel |
| **Delivery Tracking** | Per-channel delivery status tracking with retry support |

### What the Platform Does NOT Cover (Phase 0)

- Full UI implementation of inbox
- Complete email/Teams integration
- Advanced automation (escalation, digest, quiet hours)
- Deduplication algorithms
- Integration with all product modules
- Notification analytics and dashboards

---

## 3. Architecture Principles

### Principle 1 — Business Context-Oriented Notifications
Every notification carries: service, contract, release, incident, environment, tenant, owner, severity, and recommended action.

### Principle 2 — Event-Driven
Modules publish business events. The notification platform decides routing, channels, and templates.

### Principle 3 — Internal Inbox is Core
The inbox is the foundational notification experience. All notifications appear in the inbox. External channels are supplementary.

### Principle 4 — External Channels are Extensions
Email and Teams are delivery channels orchestrated by the core, not independent notification systems.

### Principle 5 — Preferences and Routing in the Model
The domain model includes preferences, routing rules, and targeting from Phase 0, enabling phased implementation.

### Principle 6 — No Spam by Design
Architecture prevents notification spam through: categorization, severity-based eligibility, user preferences, and future deduplication/digest/quiet hours support.

### Principle 7 — Executable Blueprint
Phase 1+ can be implemented directly from this foundation without inventing rules mid-implementation.

---

## 4. Official Taxonomy

### 4.1 Notification Categories

| Category | Description | Example Events |
|---|---|---|
| `Incident` | Operational incidents, degradations, anomalies | Incident created, severity changed, service degraded |
| `Approval` | Pending, rejected, expired approvals | Approval pending, approval expired, waiver pending |
| `Change` | Releases, promotions, deployments | Release published, promotion registered, deployment received |
| `Contract` | API contracts, breaking changes, publications | Contract published, breaking change detected, validation failed |
| `Security` | Access, break-glass, JIT, secrets | Break-glass activated, JIT granted, secret expiring |
| `Compliance` | Evidence, policies, compliance checks | Evidence expired, policy violated, compliance check failed |
| `FinOps` | Budgets, costs, anomalies, waste | Budget 80/90/100%, cost anomaly, waste detected |
| `AI` | AI providers, tokens, policies, drafts | Provider unavailable, token budget exceeded, draft completed |
| `Integration` | Connectors, ingestion, sync, webhooks | Sync failed, connector auth failed, webhook rejected |
| `Platform` | Health, backups, pipelines, workers | Job failed, backup failed, health critical |
| `Informational` | General information, no action required | Contract published (info), release notes available |

### 4.2 Notification Severities

| Severity | Meaning | Visual Priority | Default Channels | Acknowledge |
|---|---|---|---|---|
| `Info` | Informational, no action needed | Neutral / Gray | InApp only | Not required |
| `ActionRequired` | Action recommended within reasonable timeframe | Blue / Highlight | InApp + Email (by preference) | May be required |
| `Warning` | Attention needed, may escalate | Yellow / Orange | InApp + Email + Teams (by preference) | Recommended |
| `Critical` | Immediate action required, production impact | Red | All configured channels | Required |

---

## 5. Domain Model

### 5.1 Entities

#### `Notification` (Aggregate Root)
The central entity representing a notification delivered to a specific recipient.

| Field | Type | Description |
|---|---|---|
| `Id` | `NotificationId` | Strongly-typed unique identifier |
| `TenantId` | `Guid` | Tenant isolation |
| `RecipientUserId` | `Guid` | Target user |
| `EventType` | `string` | Source event type (e.g., "IncidentCreated") |
| `Category` | `NotificationCategory` | Functional category |
| `Severity` | `NotificationSeverity` | Notification severity |
| `Title` | `string` | Short title for list/push display |
| `Message` | `string` | Full message with business context |
| `SourceModule` | `string` | Originating module |
| `SourceEntityType` | `string?` | Entity type for deep linking |
| `SourceEntityId` | `string?` | Entity ID for deep linking |
| `EnvironmentId` | `Guid?` | Environment context |
| `ActionUrl` | `string?` | Deep link URL |
| `RequiresAction` | `bool` | Whether acknowledge is expected |
| `Status` | `NotificationStatus` | Lifecycle status |
| `PayloadJson` | `string?` | Additional JSON payload for templates |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp (UTC) |
| `ReadAt` | `DateTimeOffset?` | Read timestamp |
| `AcknowledgedAt` | `DateTimeOffset?` | Acknowledge timestamp |
| `ArchivedAt` | `DateTimeOffset?` | Archive timestamp |
| `ExpiresAt` | `DateTimeOffset?` | Expiration timestamp |

#### `NotificationDelivery`
Tracks delivery per external channel with retry support.

| Field | Type | Description |
|---|---|---|
| `Id` | `NotificationDeliveryId` | Strongly-typed unique identifier |
| `NotificationId` | `NotificationId` | Parent notification |
| `Channel` | `DeliveryChannel` | Delivery channel (Email, Teams) |
| `RecipientAddress` | `string?` | Channel-specific address |
| `Status` | `DeliveryStatus` | Delivery status |
| `CreatedAt` | `DateTimeOffset` | Creation timestamp |
| `DeliveredAt` | `DateTimeOffset?` | Successful delivery timestamp |
| `FailedAt` | `DateTimeOffset?` | Last failure timestamp |
| `ErrorMessage` | `string?` | Last error message |
| `RetryCount` | `int` | Retry attempt count |

#### `NotificationPreference`
User preferences per category and channel.

| Field | Type | Description |
|---|---|---|
| `Id` | `NotificationPreferenceId` | Strongly-typed unique identifier |
| `TenantId` | `Guid` | Tenant isolation |
| `UserId` | `Guid` | User who set the preference |
| `Category` | `NotificationCategory` | Notification category |
| `Channel` | `DeliveryChannel` | Delivery channel |
| `Enabled` | `bool` | Whether this category+channel is enabled |
| `UpdatedAt` | `DateTimeOffset` | Last update timestamp |

### 5.2 Notification Lifecycle States

```
Unread → Read → Acknowledged → Archived
Unread → Read → Archived
Unread → Dismissed
Read → Dismissed
```

| Status | Description |
|---|---|
| `Unread` | Created but not yet viewed |
| `Read` | Viewed by recipient |
| `Acknowledged` | Explicitly confirmed (for action-required notifications) |
| `Archived` | Archived by user or automatically |
| `Dismissed` | Dismissed without action |

### 5.3 Delivery States

| Status | Description |
|---|---|
| `Pending` | Awaiting processing |
| `Delivered` | Successfully delivered |
| `Failed` | Failed after all attempts |
| `Skipped` | Skipped (opt-out, channel disabled, deduplication) |

---

## 6. API Contracts (Planned)

### 6.1 Notification Module Interface

```
INotificationModule.SubmitAsync(NotificationRequest) → NotificationResult
INotificationModule.GetUnreadCountAsync(recipientUserId) → int
```

### 6.2 Internal Inbox API (Phase 1)

| Endpoint | Method | Description |
|---|---|---|
| `GET /api/notifications` | List | List notifications with filters |
| `GET /api/notifications/unread-count` | Count | Get unread count |
| `PUT /api/notifications/{id}/read` | Update | Mark as read |
| `PUT /api/notifications/{id}/unread` | Update | Mark as unread |
| `PUT /api/notifications/read-all` | Bulk | Mark all as read |
| `PUT /api/notifications/{id}/acknowledge` | Update | Acknowledge notification |
| `PUT /api/notifications/{id}/archive` | Update | Archive notification |
| `PUT /api/notifications/{id}/dismiss` | Update | Dismiss notification |

### 6.3 Preferences API (Phase 4)

| Endpoint | Method | Description |
|---|---|---|
| `GET /api/notifications/preferences` | List | Get user preferences |
| `PUT /api/notifications/preferences` | Update | Update preferences |

---

## 7. Module Structure

```
src/modules/notifications/
├── NexTraceOne.Notifications.Contracts/     # Public contracts (INotificationModule, DTOs, integration events)
├── NexTraceOne.Notifications.Domain/        # Domain entities, enums, events, strongly-typed IDs
├── NexTraceOne.Notifications.Application/   # Orchestrator, store, routing abstractions
├── NexTraceOne.Notifications.Infrastructure/ # (Phase 1+) EF Core, dispatchers, repositories
└── NexTraceOne.Notifications.API/           # (Phase 1+) API endpoints

tests/modules/notifications/
└── NexTraceOne.Notifications.Tests/         # Unit tests (54 tests, all passing)
```

---

## 8. Compatibility with Existing Architecture

| Existing Pattern | Notification Platform Usage |
|---|---|
| Domain Events (DomainEventBase) | NotificationCreatedEvent, NotificationReadEvent, NotificationDeliveryCompletedEvent |
| Integration Events (IntegrationEventBase) | NotificationCreatedIntegrationEvent, NotificationDeliveredIntegrationEvent |
| Outbox Pattern | Notifications will leverage existing outbox for guaranteed delivery |
| Strongly-Typed IDs (TypedIdBase) | NotificationId, NotificationDeliveryId, NotificationPreferenceId |
| AggregateRoot pattern | Notification entity as aggregate root |
| Multi-tenancy | TenantId in all entities |
| Alerting Gateway | Complementary — alerts are operational; notifications are user-facing |

---

## 9. Conclusion

Phase 0 establishes the official notification model for NexTraceOne. The domain entities, enums, contracts, and abstractions are implemented and tested (54 unit tests, all passing). The event catalog, architecture, and roadmap are documented.

**Phase 1 can begin without ambiguity.**
