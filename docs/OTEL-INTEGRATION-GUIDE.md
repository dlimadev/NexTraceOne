# NexTraceOne — OTel Collector Integration Guide

## Prerequisites

- [OpenTelemetry Collector Contrib](https://github.com/open-telemetry/opentelemetry-collector-contrib) >= 0.100.0
- NexTraceOne running and accessible (Docker Compose or standalone)
- Your application instrumented with any OTel SDK (.NET, Java, Go, Node.js, Python, etc.)

## Required Environment Variables

| Variable | Description | Example |
|---|---|---|
| `NEXTRACEONE_INGESTION_URL` | Base URL of the NexTraceOne Ingestion API | `https://nextraceone.company.com/api/v1/ingest` |
| `NEXTRACEONE_API_KEY` | API key for authentication | `nxt_live_xxxxxxxxxxxx` |
| `NEXTRACEONE_TENANT` | Tenant identifier | `acme-corp` |
| `NEXTRACEONE_ENVIRONMENT` | Target environment name | `production` |

## Running the Recipe

### Option 1 — Docker (recommended for testing)

```bash
export NEXTRACEONE_INGESTION_URL=https://your-nextraceone/api/v1/ingest
export NEXTRACEONE_API_KEY=your-api-key
export NEXTRACEONE_TENANT=your-tenant
export NEXTRACEONE_ENVIRONMENT=production

docker run --rm \
  -v $(pwd)/docs/otel-collector-recipe.yaml:/etc/otelcol/config.yaml \
  -e NEXTRACEONE_INGESTION_URL \
  -e NEXTRACEONE_API_KEY \
  -e NEXTRACEONE_TENANT \
  -e NEXTRACEONE_ENVIRONMENT \
  -p 4317:4317 \
  -p 4318:4318 \
  -p 8888:8888 \
  -p 13133:13133 \
  otel/opentelemetry-collector-contrib:latest
```

### Option 2 — Docker Compose (add to existing compose file)

```yaml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    volumes:
      - ./docs/otel-collector-recipe.yaml:/etc/otelcol/config.yaml
    environment:
      - NEXTRACEONE_INGESTION_URL=${NEXTRACEONE_INGESTION_URL}
      - NEXTRACEONE_API_KEY=${NEXTRACEONE_API_KEY}
      - NEXTRACEONE_TENANT=${NEXTRACEONE_TENANT}
      - NEXTRACEONE_ENVIRONMENT=${NEXTRACEONE_ENVIRONMENT}
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "8888:8888"   # Collector metrics
      - "13133:13133" # Health check
```

### Option 3 — Binary

```bash
otelcol-contrib --config ./docs/otel-collector-recipe.yaml
```

## Configuring Your Application

Set the OTel SDK to export to the collector:

**.NET**
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317")))
    .WithMetrics(m => m.AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317")));
```

**Environment variables (any SDK)**
```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=my-service
OTEL_RESOURCE_ATTRIBUTES=service.team=platform-team,service.version=1.2.0
```

## Verifying Data in NexTraceOne

1. Check collector health: `curl http://localhost:13133`
2. Check collector metrics: `curl http://localhost:8888/metrics`
3. In NexTraceOne UI → **Service Catalog** → select your service → **Observability** tab
4. In NexTraceOne UI → **Change Intelligence** → check trace-to-release correlation

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No data in NexTraceOne | Wrong `NEXTRACEONE_INGESTION_URL` | Verify the URL includes `/api/v1/ingest` |
| 401 errors in collector logs | Invalid API key | Regenerate API key in NexTraceOne → Settings → Integrations |
| High memory usage | Batch size too large | Reduce `send_batch_size` in `batch` processor |
| Traces missing tenant context | `NEXTRACEONE_TENANT` not set | Ensure the env var is exported before starting the collector |
| Collector fails to start | Config syntax error | Run `otelcol-contrib --config ./docs/otel-collector-recipe.yaml validate` |

## Production Recommendations

- Disable the `debug` exporter in production (comment it out in the pipeline)
- Use TLS (`tls.insecure: false`) and valid certificates for the ingestion endpoint
- Run the collector as a sidecar or DaemonSet, not embedded in your application
- Monitor `otelcol_processor_dropped_spans` metric — spikes indicate backpressure
