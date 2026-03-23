# OBSERVABILITY-STRATEGY.md — NexTraceOne

## Purpose

Observability in NexTraceOne serves as the **technical data layer** for internal analysis,
AI-assisted operations, and governance. It is not an end in itself — it is input for
**correlation, decision-making, and operational intelligence**.

## Principles

1. **Provider-agnostic**: ClickHouse (default) or Elastic (enterprise integration).
2. **Collection per environment**: OpenTelemetry Collector (Kubernetes) or CLR Profiler (IIS/Windows).
3. **Source-aware**: IIS, Kubernetes, and Kafka are first-class signal sources.
4. **Product-oriented**: Data is consumed by NexTraceOne and its internal AI — not by external dashboards.
5. **PostgreSQL for domain only**: Observability data lives in ClickHouse or Elastic, never in PostgreSQL.

## Signals

- **Logs**: Structured logs from services, IIS, Kubernetes workloads, Kafka brokers/consumers.
- **Traces**: Distributed traces with correlation IDs, environment context, and tenant awareness.
- **Metrics**: CPU, memory, request duration, error rate, throughput — per service, host, container.

## Use Cases

- Change validation and confidence scoring.
- Incident correlation and root cause analysis.
- Post-release impact analysis.
- Environment comparison (homologation vs production).
- Kafka flow failure analysis.
- Release risk evidence generation.
- AI-assisted investigation and recommendation.

## Rule

Observability supports governance, not the opposite.
All telemetry data must serve the product's analytical and operational goals.
