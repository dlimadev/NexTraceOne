# ADR-001: Modular Monolith over Microservices

## Status

Accepted

## Date

2026-01-15

## Context

NexTraceOne is an enterprise platform with 12+ bounded contexts (Identity, Catalog, Contracts, ChangeGovernance, OperationalIntelligence, AuditCompliance, AIKnowledge, Governance, Integrations, Knowledge, Notifications, ProductAnalytics, Configuration).

The team evaluated two architectural approaches:

1. **Microservices**: Each bounded context as an independent service with its own database, deployed independently.
2. **Modular Monolith**: All bounded contexts in a single deployable unit with strict module boundaries, shared database with logical isolation, and cross-module communication via contracts and outbox pattern.

Key constraints:
- Enterprise self-hosted/on-premises deployment requirement
- Small initial team
- Need for strong consistency within modules
- Need for operational simplicity for customers

## Decision

We chose a **modular monolith** architecture with the following characteristics:

- **28 DbContexts** across 12 modules, all sharing a single PostgreSQL database with table-prefix isolation per module (27 at time of initial decision; grew to 28 as product evolved — run `./tools/count-dbcontexts.sh --count` for authoritative current count).
- **20+ cross-module contract interfaces** defining explicit boundaries between modules.
- **19+ outbox processors** for reliable asynchronous cross-module communication.
- **Clean Architecture** within each module: Domain → Application → Infrastructure → API.
- **CQRS with MediatR** for command/query separation.
- **Row-Level Security (RLS)** in PostgreSQL for tenant isolation as defence-in-depth.

## Consequences

### Positive

- Simpler deployment and operations for enterprise self-hosted customers.
- Easier debugging and tracing — single process with full stack traces.
- Strong consistency within transactions (no distributed transactions needed).
- Lower infrastructure costs — single database, single deployment.
- Faster development iteration — no inter-service serialization overhead.

### Negative

- Risk of accidental coupling between modules if boundaries are not enforced.
- Cannot scale modules independently (mitigated by background workers separation).
- Single point of failure (mitigated by health checks and restart policies).

### Mitigations

- Module boundaries enforced by separate DbContexts (one module cannot access another's DbContext).
- Cross-module communication via outbox pattern ensures eventual consistency.
- Prepared for future extraction: each module can be extracted to a microservice by replacing in-process calls with HTTP/gRPC.
