# ADR-003: Elasticsearch as Observability Provider

## Status

Accepted

## Date

2026-02-01

## Context

NexTraceOne provides contextualized observability — not generic dashboards, but telemetry correlated with services, contracts, changes, and incidents. We needed an analytics-capable backend for:

- Log storage and full-text search
- Metrics aggregation by service, team, environment
- Trace correlation with change events
- Audit trail search and compliance evidence export
- Historical pattern analysis for change intelligence

Options considered:

1. **Loki/Tempo/Prometheus stack**: Cloud-native, but limited analytical query capabilities.
2. **ClickHouse**: Excellent analytics performance, but operational complexity for self-hosted.
3. **Elasticsearch**: Strong analytical capabilities, full-text search, mature ecosystem.
4. **PostgreSQL only**: Use PostgreSQL FTS for MVP, defer external analytics.

## Decision

We chose **Elasticsearch** as the primary observability and analytics provider, with PostgreSQL as the authoritative source of truth for domain data:

- **Elasticsearch** handles: log storage, metrics aggregation, trace search, analytics queries.
- **PostgreSQL** handles: domain entities, audit trails, compliance data, configuration.
- **OpenTelemetry Collector** as the ingestion gateway for telemetry data.
- **Production security**: xpack.security enabled with TLS, authentication, and API keys.

## Consequences

### Positive

- Powerful full-text search for logs, audit trails, and knowledge documents.
- Efficient time-series aggregation for dashboards and analytics.
- Rich query DSL for complex analytical queries (change correlation, pattern detection).
- Mature ecosystem with good .NET client support.

### Negative

- Additional infrastructure component to manage.
- JVM-based — requires memory tuning for production.
- Index management and retention policies need attention.

### Mitigations

- Docker Compose profiles for development (ES without security) and production (ES with xpack).
- Resource limits configured in production overlay.
- PostgreSQL FTS used as fallback for basic search when Elasticsearch is unavailable.
