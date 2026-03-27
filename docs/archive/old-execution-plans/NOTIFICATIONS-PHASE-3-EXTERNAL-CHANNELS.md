# Notifications Phase 3 — External Channels: Email + Microsoft Teams

## Objective

Expand the NexTraceOne notification platform beyond the internal notification center, delivering the first real external notification channels: **Email** and **Microsoft Teams**.

This phase implements the official external delivery layer of the platform, integrating with the engine delivered in Phases 1 and 2.

## Channels Delivered

### Email (SMTP)
- Full HTML email with branded template
- Plain text alternative view
- Configurable SMTP settings per environment
- Subject line includes severity and title
- Deep link to NexTraceOne action page
- Respects enable/disable configuration

### Microsoft Teams (Incoming Webhook)
- Adaptive Card v1.4 format
- Structured card with severity emoji, title, message, context facts
- Action button linking to NexTraceOne
- Configurable webhook URL per environment
- Respects enable/disable configuration

## Integration with Engine

The `NotificationOrchestrator` was expanded to trigger external delivery after creating internal notifications:

1. Notification is created internally (Phase 1/2 behavior preserved)
2. `IExternalDeliveryService` is invoked for each created notification
3. `INotificationRoutingEngine` determines eligible channels based on severity
4. Channel dispatchers send the notification
5. Delivery log is persisted with status tracking

### Routing Rules (Phase 3)

| Severity | InApp | Email | Teams |
|---|---|---|---|
| Info | ✅ | ❌ | ❌ |
| ActionRequired | ✅ | ✅ | ❌ |
| Warning | ✅ | ✅ | ✅ |
| Critical | ✅ | ✅ | ✅ |

### Event Types with External Delivery

| Event | Email | Teams |
|---|---|---|
| IncidentCreated | ✅ | ✅ |
| IncidentEscalated | ✅ | ✅ |
| BreakGlassActivated | ✅ | ✅ |
| ApprovalPending | ✅ | ❌ |
| ApprovalRejected | ✅ | ❌ |
| ComplianceCheckFailed | ✅ | ✅ |
| BudgetExceeded | ✅ | ✅ |
| IntegrationFailed | ✅ | ✅ |
| AiProviderUnavailable | ✅ | ✅ |

## Deliberate Limitations

This phase does NOT include:
- User notification preferences (opt-in/opt-out)
- Quiet hours or digest
- Advanced routing rules
- Slack, SMS, or generic webhook channels
- Escalation or grouping
- Role/team-based recipient resolution for external channels
- Per-user email address resolution (requires Identity integration)

These are deferred to Phase 4 and beyond.

## Architecture Components

| Component | Layer | Purpose |
|---|---|---|
| `NotificationChannelOptions` | Application | Configuration model for Email and Teams |
| `DeliveryRetryOptions` | Application | Retry policy configuration |
| `IExternalDeliveryService` | Application | External delivery coordination abstraction |
| `INotificationDeliveryStore` | Application | Delivery log persistence abstraction |
| `IExternalChannelTemplateResolver` | Application | Template resolution per channel |
| `NotificationRoutingEngine` | Infrastructure | Severity-based channel routing |
| `EmailNotificationDispatcher` | Infrastructure | SMTP email dispatch |
| `TeamsNotificationDispatcher` | Infrastructure | Teams webhook dispatch |
| `ExternalChannelTemplateResolver` | Infrastructure | HTML email + Adaptive Card templates |
| `ExternalDeliveryService` | Infrastructure | Routing + dispatch + retry + logging |
| `NotificationDeliveryStoreRepository` | Infrastructure | EF Core delivery persistence |
| `NotificationDeliveryConfiguration` | Infrastructure | EF Core mapping for ntf_deliveries |

## Test Coverage

73 new tests added covering:
- Routing engine (8 tests)
- Template resolver — email and Teams (22 tests)
- External delivery service (8 tests)
- Email dispatcher (7 tests)
- Teams dispatcher (5 tests)
- Channel options (15 tests)
- Orchestrator integration (4 tests)
- Pre-existing event handler tests fixed (4 tests)
