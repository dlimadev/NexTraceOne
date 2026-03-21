# ADR-002: In-Process Event Bus Limitation

**Status:** Accepted  
**Date:** 2026-03-21  
**Context:** Architectural Analysis (ANALISE-CRITICA-ARQUITETURAL-2026-03.md)

## Context

NexTraceOne uses an `InProcessEventBus` as the sole `IEventBus` implementation. The `OutboxEventBus` persists events to the outbox table but delivers them via the same in-process bus through the `OutboxProcessorJob`.

## Decision

Accept the in-process event bus as sufficient for the current modular monolith architecture.

## Rationale

- The modular monolith runs as a single process — in-process pub/sub is appropriate
- The Outbox pattern provides durability: events survive process restarts
- Adding RabbitMQ/Kafka introduces operational complexity not justified by current scale
- The `IEventBus` abstraction allows future replacement without code changes

## Known Limitations

1. **No horizontal scaling:** Multiple ApiHost instances will process outbox messages concurrently without coordination (requires distributed lock)
2. **No ordering guarantees:** Events are processed in batch order, not strict publish order
3. **Retry without idempotency (mitigated):** `IdempotencyKey` added to `OutboxMessage` — handlers should check this key before executing side-effects
4. **Throughput:** OutboxProcessorJob processes 50 messages every 5 seconds. At sustained 600+ events/minute, processing will fall behind

## When to Revisit

- When deploying multiple ApiHost instances (requires distributed event bus)
- When event throughput exceeds 500 events/minute sustained
- When inter-service communication is needed (microservices extraction)

## Mitigation (Applied)

- `OutboxMessage.IdempotencyKey` added for handler-level deduplication
- `Maximum Pool Size=10` on connection strings prevents connection exhaustion
- Warning log added when auto-migrations run in non-Development environments
