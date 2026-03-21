# ADR-003: Event Bus and Outbox Limitations

**Status:** Accepted  
**Date:** 2026-03-21  
**Context:** NexTraceOne uses an Outbox pattern for guaranteed event delivery.

## Decision

### Current State

- **InMemoryEventBus** is the sole IEventBus implementation
- Events published within a domain transaction are saved to the `OutboxMessages` table
- `OutboxProcessorJob` reads unprocessed messages and publishes them via `IEventBus`

### Known Limitations

1. **Single-context processing:** Only `IdentityDbContext` outbox is processed.
   The remaining 15 DbContexts have `OutboxMessages` tables but **no active processor**.
   Events written to these tables are never delivered.

2. **InMemoryEventBus:** Events are not durable after the process dies.
   There is no RabbitMQ, Kafka, or other distributed broker.

3. **No cross-module event consumption:** 4 integration events are defined
   (`UserCreatedIntegrationEvent`, `UserRoleChangedIntegrationEvent`,
   `RiskReportGenerated`, `ComplianceGapsDetected`) but none have consumers.

### Idempotency Key

The `OutboxMessage.IdempotencyKey` is **deterministic** — computed as:
```
{EventType}:{SHA256(payload)[0..16]}:{CreatedAt:O}
```
Same logical event always produces the same key, enabling deduplication by downstream handlers.

### AtomicPer-Message Processing

Each message is saved atomically after processing (per-message `SaveChangesAsync`),
preventing duplicate delivery on crash mid-batch.

## Evolution Path

1. Add outbox processors for all 16 DbContexts (or a generic multi-context processor)
2. Replace InMemoryEventBus with a durable broker (RabbitMQ or Kafka)
3. Implement integration event consumers in target modules
4. Add dead-letter handling for exhausted messages (RetryCount >= MaxRetry)

## Consequences

- Events from modules other than Identity are silently dropped
- Cross-module integration is effectively disabled until processors are added
- The system is honest about this: `IMPLEMENTATION-STATUS.md` marks Outbox as PARTIAL
