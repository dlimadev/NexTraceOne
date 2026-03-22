# Phase 7 — OTLP / Loki / Tempo / Grafana Verification

## Pipeline Architecture

```
Application → OTLP Collector → Tempo (traces)
                             → Loki (logs)
                             → Product Store (aggregated metrics)
Grafana ← Tempo / Loki / Prometheus datasources
```

## What Was Verified

### OpenTelemetry Collector
- **Config**: `build/otel-collector/otel-collector.yaml`
- Receivers: OTLP gRPC (4317) + HTTP (4318), Prometheus scrape, HostMetrics
- Processors: memory limiter, batch, tail sampling, redaction (PII), resource detection
- Exporters: Tempo (traces), Loki (logs), OTLP (metrics to Product Store)
- SpanMetrics connector: derives request count, latency histogram, error count from traces

### Grafana Tempo
- **Config**: `build/otel-collector/tempo.yaml`
- OTLP gRPC receiver on port 4317, HTTP API on 3200
- Local storage with 720h (30-day) block retention
- Metrics generator enabled

### Grafana Loki
- **Config**: `build/otel-collector/loki.yaml`
- HTTP API on port 3100
- Filesystem storage with 720h retention
- Schema v13 with TSDB, compaction and deletion enabled

### Grafana Provisioning
- **Datasources** (`build/observability/grafana/provisioning/datasources/datasources.yaml`):
  - Tempo (uid: nextraceone-tempo) with Loki trace-to-logs correlation
  - Loki (uid: nextraceone-loki) with TraceID extraction
  - Prometheus (uid: nextraceone-prometheus) from OTel Collector metrics
- **Dashboards** (`build/observability/grafana/provisioning/dashboards/dashboards.yaml`):
  - platform-health.json
  - business-observability.json
  - runtime-environment-comparison.json

### Serilog → Loki Integration
- **Config**: `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Logging/SerilogConfiguration.cs`
- Loki sink configured from `Observability:Serilog:Loki:Endpoint`
- Labels: application, environment, module
- Minimum level: Information

### Metrics Aggregation (Product Store)
- **Config**: `TelemetryStoreOptions` in Observability building block
- Time partitioning: minute metrics (1-day partitions), hourly metrics (1-month partitions)
- Retention: raw 7d hot/30d warm, minute 7d, hourly 90d hot/365d warm

## Verification Script

```bash
scripts/observability/verify-pipeline.sh [--otel-url URL] [--tempo-url URL] [--loki-url URL] [--grafana-url URL]
```

The script performs:
1. OTel Collector health check
2. Tempo readiness check
3. Loki readiness check
4. Grafana health and datasource provisioning check
5. Test trace ingestion via OTLP/HTTP → verify in Tempo
6. Test log ingestion via OTLP/HTTP → verify in Loki
7. Dashboard provisioning verification

## Corrections Applied
- None required — configuration was already correct and complete

## Limitations
- Full end-to-end verification requires running services (docker-compose up)
- Grafana API checks may need authentication in non-default configurations
- Trace propagation to Tempo has a small delay (2-5 seconds)
