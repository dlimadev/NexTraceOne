# Grafana Dashboards e Provisioning

## Dashboards Criados

### 1. NexTraceOne — Runtime & Environment Comparison

**Ficheiro:** `build/observability/grafana/dashboards/runtime-environment-comparison.json`

**UID:** `nextraceone-runtime-comparison`

**Painéis:**
- Drift Findings — Critical/High (stat)
- Drift Findings — Medium (stat)
- DriftDetectionJob — Ciclos (stat)
- DriftDetectionJob — Falhas (stat)
- Drift Detection Logs (logs)
- Runtime Intelligence Traces (traces via Tempo)

**Variável:** `environment` (dev, test, qa, uat, staging, production)

### 2. NexTraceOne — Platform Health

**Ficheiro:** `build/observability/grafana/dashboards/platform-health.json`

**UID:** `nextraceone-platform-health`

**Painéis:**
- API Success Rate (stat)
- API P95 Latency (stat)
- API Error Rate (stat)
- Request Rate (stat)
- OutboxProcessorJob Logs (logs)
- DriftDetectionJob Logs (logs)
- Spans Received/s (stat)
- Spans Refused/s (stat)
- Collector Queue Size (stat)
- Error & Warning Logs (logs)

### 3. NexTraceOne — Business Observability

**Ficheiro:** `build/observability/grafana/dashboards/business-observability.json`

**UID:** `nextraceone-business-observability`

**Painéis:**
- Snapshots Ingeridos (24h)
- Drift Findings Detectados (24h)
- Releases Comparadas (24h)
- Incidentes Correlacionados (24h)
- Runtime Intelligence Activity Logs
- Ingestion API Logs

## Provisioning

**Datasources:** `build/observability/grafana/provisioning/datasources/datasources.yaml`

Configura automaticamente:
- Tempo (traces) — uid: `nextraceone-tempo`
- Loki (logs) — uid: `nextraceone-loki`  
- Prometheus (OTel self-monitoring) — uid: `nextraceone-prometheus`

**Dashboards:** `build/observability/grafana/provisioning/dashboards/dashboards.yaml`

Carrega automaticamente todos os dashboards de `/var/lib/grafana/dashboards/nextraceone`.

## Como Iniciar Localmente

O Grafana está configurado no `build/otel-collector/docker-compose.telemetry.yaml`:

```bash
cd build/otel-collector
docker-compose -f docker-compose.telemetry.yaml up -d
```

O Grafana estará disponível em `http://localhost:3000` com acesso anónimo como Admin.

Os dashboards serão provisionados automaticamente ao iniciar.

## Fontes de Dados

Todos os painéis usam dados reais das seguintes fontes:
- **Loki:** logs estruturados do Serilog (`{application="NexTraceOne"}`)
- **Tempo:** traces OTel exportados via OTel Collector
- **Prometheus:** métricas self-monitoring do OTel Collector
