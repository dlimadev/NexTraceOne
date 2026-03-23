# NexTraceOne — Notifications Implementation Roadmap

> **Status:** COMPLETE  
> **Date:** 2026-03-23  
> **Phase:** 0 — Foundation

---

## Overview

This document defines the official implementation roadmap for the NexTraceOne Notification Platform, organized in 7 phases from foundational infrastructure to advanced intelligence and governance.

Each phase builds on the previous one. Dependencies are explicit. Criteria of acceptance are defined per phase.

---

## Phase 0 — Foundation & Official Model ✅ COMPLETE

### Objective
Define the official notification model, events, categories, severities, routing, entities, and architecture.

### Scope
- Discovery of notification points across all product modules
- Official event catalog (56 events across 8 families)
- Taxonomy of categories (11) and severities (4)
- Domain entities: Notification, NotificationDelivery, NotificationPreference
- Application abstractions: Orchestrator, Store, ChannelDispatcher, RoutingEngine, PreferenceService
- Public contracts: INotificationModule, NotificationRequest, NotificationResult
- Integration events: NotificationCreated, NotificationDelivered, NotificationDeliveryFailed
- 54 unit tests (all passing)
- Official documentation (5 documents)

### Dependencies
None.

### Deliverables
- [x] Event catalog document
- [x] Architecture document
- [x] Domain entities and enums
- [x] Application abstractions
- [x] Contracts and integration events
- [x] Unit tests
- [x] Roadmap document
- [x] Phase 0 audit report

### Criteria of Acceptance
- [x] Model is clear, complete, and unambiguous
- [x] Events are identified and categorized
- [x] Architecture is formally documented
- [x] Phase 1 can begin without ambiguity

---

## Phase 1 — Internal Notification Center

### Objective
Implement the central internal inbox as a functional product capability.

### Scope
- `NotificationsDbContext` with EF Core (PostgreSQL)
- `NotificationStore` implementation (repository pattern)
- API endpoints for the inbox:
  - `GET /api/notifications` — list with filters and pagination
  - `GET /api/notifications/unread-count` — unread count
  - `PUT /api/notifications/{id}/read` — mark as read
  - `PUT /api/notifications/{id}/unread` — mark as unread
  - `PUT /api/notifications/read-all` — mark all as read
  - `PUT /api/notifications/{id}/acknowledge` — acknowledge
  - `PUT /api/notifications/{id}/archive` — archive
  - `PUT /api/notifications/{id}/dismiss` — dismiss
- Infrastructure DI registration
- Database migration
- API module registration in ApiHost
- Basic authorization (tenant-scoped, user-scoped)

### Dependencies
- Phase 0 (complete)

### Deliverables
- [ ] NotificationsDbContext
- [ ] NotificationStore implementation
- [ ] API endpoints (8 endpoints)
- [ ] Database migration
- [ ] DependencyInjection setup
- [ ] Integration with ApiHost
- [ ] Unit tests for store
- [ ] Integration tests for API

### Criteria of Acceptance
- [ ] Users can list, filter, and manage notifications via API
- [ ] Unread count is accurate
- [ ] State transitions work correctly
- [ ] Multi-tenant isolation is enforced
- [ ] All existing tests continue to pass

---

## Phase 2 — Notification Engine & Event Catalog Wiring

### Objective
Implement the Notification Orchestrator and wire existing domain events to notification production.

### Scope
- `NotificationOrchestrator` implementation
- Basic recipient resolution (explicit user IDs only, roles/teams in Phase 4)
- Basic routing engine (severity-based defaults, no user preferences yet)
- Wire high-priority domain events from existing modules:
  - `CostAnomalyDetectedEvent` → FinOps notification
  - `WorkflowApprovedEvent` / `WorkflowRejectedEvent` → Approval notifications
  - `ReleasePublishedEvent` → Change notification
  - `ComplianceGapsDetected` → Compliance notification
- Integration event handlers in module Infrastructure layers

### Dependencies
- Phase 1 (inbox functional)

### Deliverables
- [ ] NotificationOrchestrator implementation
- [ ] BasicNotificationRoutingEngine (severity defaults)
- [ ] Event handlers for 5+ high-priority events
- [ ] Unit tests for orchestrator
- [ ] Integration tests for event-to-notification flow

### Criteria of Acceptance
- [ ] Domain events automatically generate notifications in the inbox
- [ ] Recipients receive notifications for events they are targeted for
- [ ] Severity-based channel routing works

---

## Phase 3 — External Channels: Email & Microsoft Teams

### Objective
Implement email and Teams delivery channels.

### Scope
- `EmailNotificationDispatcher` implementation
  - HTML templates with severity-based styling
  - Deep link integration
  - SMTP configuration (building on existing `EmailAlertChannel` pattern)
- `TeamsNotificationDispatcher` implementation
  - Adaptive Card rendering
  - Incoming webhook integration
  - Deep link action buttons
- `NotificationDelivery` persistence and tracking
- Delivery status updates (Delivered, Failed, Skipped)
- Basic retry logic (configurable max retries)

### Dependencies
- Phase 2 (orchestrator functional)
- SMTP configuration available
- Teams webhook URL configured

### Deliverables
- [ ] EmailNotificationDispatcher
- [ ] TeamsNotificationDispatcher
- [ ] NotificationDelivery repository
- [ ] Email HTML templates
- [ ] Teams Adaptive Card templates
- [ ] Delivery tracking and retry
- [ ] Configuration options (appsettings)
- [ ] Unit tests for dispatchers
- [ ] Integration tests for delivery flow

### Criteria of Acceptance
- [ ] Critical notifications are delivered via email and Teams
- [ ] Delivery status is tracked per channel
- [ ] Failed deliveries are retried
- [ ] Templates are professional and include deep links

---

## Phase 4 — Preferences & Advanced Routing

### Objective
Implement user preferences and advanced recipient resolution.

### Scope
- `NotificationPreferenceService` implementation
- Preferences API endpoints:
  - `GET /api/notifications/preferences` — get user preferences
  - `PUT /api/notifications/preferences` — update preferences
- Advanced recipient resolution:
  - Role-based resolution (via Identity module)
  - Team-based resolution (via Identity module)
  - Service owner resolution (via Catalog module)
- Routing engine enhanced with user preferences
- Default preference fallbacks
- Critical severity override (cannot opt out of safety-critical notifications)

### Dependencies
- Phase 3 (channels functional)
- Identity module APIs for role/team resolution

### Deliverables
- [ ] NotificationPreferenceService implementation
- [ ] Preferences API endpoints
- [ ] Advanced recipient resolution
- [ ] Enhanced routing engine
- [ ] Preference-aware channel selection
- [ ] Critical severity override logic
- [ ] Unit tests for preferences and routing
- [ ] Integration tests for preference API

### Criteria of Acceptance
- [ ] Users can configure notification preferences per category and channel
- [ ] Routing respects user preferences
- [ ] Critical notifications bypass opt-out settings
- [ ] Role/team-based targeting resolves correctly

---

## Phase 5 — High-Value Domain Events

### Objective
Wire all high-value events from the event catalog to notification production.

### Scope
- Incident events (IncidentCreated, IncidentEscalated, ServiceDegraded, etc.)
- Security events (BreakGlassActivated, SecretExpiring, OIDCProviderUnavailable)
- FinOps events (BudgetThreshold80/90/100, WasteDetected)
- AI events (AIProviderUnavailable, TokenBudgetExceeded)
- Integration events (SyncFailed, ConnectorAuthFailed)
- Platform events (BackupFailed, PipelineFailed)
- Contract events (BreakingChangeDetected, ContractExpiring)
- Approval events (ApprovalPending, ApprovalExpiring)

### Dependencies
- Phase 4 (preferences and routing functional)
- All producing modules have domain events or can emit them

### Deliverables
- [ ] Event handlers for 40+ events from the catalog
- [ ] Notification templates per event type
- [ ] Deep links per event type
- [ ] Comprehensive routing rules per event
- [ ] Unit tests for all event handlers

### Criteria of Acceptance
- [ ] All high-value events from the catalog generate notifications
- [ ] Each event produces contextually appropriate notifications
- [ ] Deep links navigate to the correct source entity

---

## Phase 6 — Intelligence & Automation

### Objective
Add intelligent notification features to reduce noise and improve response times.

### Scope
- **Digest/Batching**: Aggregate low-priority notifications into periodic summaries
- **Quiet Hours**: Suppress non-critical notifications during configured periods
- **Deduplication**: Prevent duplicate notifications for the same event
- **Escalation**: Auto-escalate unacknowledged critical notifications after timeout
- **Snooze**: Allow users to snooze notifications
- **Grouping/Correlation**: Group related notifications (e.g., multiple incidents for same service)

### Dependencies
- Phase 5 (comprehensive event coverage)

### Deliverables
- [ ] Digest engine (daily/weekly summaries)
- [ ] Quiet hours configuration and enforcement
- [ ] Deduplication logic (idempotency by event type + entity + time window)
- [ ] Escalation rules and automation
- [ ] Snooze functionality
- [ ] Notification grouping/correlation
- [ ] Unit and integration tests

### Criteria of Acceptance
- [ ] Users can configure quiet hours
- [ ] Duplicate notifications are suppressed
- [ ] Unacknowledged critical notifications escalate
- [ ] Low-priority notifications can be digested

---

## Phase 7 — Metrics, Audit & Governance

### Objective
Add observability, audit trail, and governance capabilities to the notification platform.

### Scope
- **Notification Analytics**: Delivery rates, read rates, response times
- **Audit Trail**: Full audit of notification lifecycle (via AuditCompliance module)
- **Governance Dashboard**: Admin view of notification health and configuration
- **Notification Templates Management**: Admin CRUD for notification templates
- **Channel Health Monitoring**: Monitor email/Teams delivery success rates
- **SLA Tracking**: Time-to-read and time-to-acknowledge metrics

### Dependencies
- Phase 6 (intelligence features)

### Deliverables
- [ ] Notification analytics endpoints
- [ ] Audit trail integration
- [ ] Admin governance dashboard
- [ ] Template management
- [ ] Channel health monitoring
- [ ] SLA metrics and alerts

### Criteria of Acceptance
- [ ] Admins can view notification delivery metrics
- [ ] Full audit trail is available for compliance
- [ ] Channel health is monitored and alerted
- [ ] SLA metrics inform operational decisions

---

## Summary Timeline

| Phase | Name | Status | Key Output |
|---|---|---|---|
| **0** | Foundation & Official Model | ✅ Complete | Model, entities, events, docs |
| **1** | Internal Notification Center | 🔲 Next | Inbox API, persistence |
| **2** | Engine & Event Wiring | 🔲 Planned | Orchestrator, 5+ events wired |
| **3** | External Channels | 🔲 Planned | Email + Teams dispatchers |
| **4** | Preferences & Routing | 🔲 Planned | User preferences, advanced routing |
| **5** | High-Value Domain Events | 🔲 Planned | 40+ events wired |
| **6** | Intelligence & Automation | 🔲 Planned | Digest, quiet hours, escalation |
| **7** | Metrics, Audit & Governance | 🔲 Planned | Analytics, audit, governance |

---

## Risk Register

| Risk | Mitigation |
|---|---|
| Notification spam | Severity-based routing, preferences, future deduplication |
| Channel failures (SMTP, Teams) | Retry with backoff, delivery status tracking, fallback to inbox |
| Performance at scale | Indexes, pagination, background processing via existing outbox/workers |
| Multi-tenant data leakage | TenantId in all entities, RLS interceptors |
| User preference complexity | Sensible defaults with opt-out, critical override for safety |
