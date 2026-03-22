# NexTraceOne — Arquitetura de Telemetria

## Visão Geral

A arquitetura de telemetria do NexTraceOne segue três princípios fundamentais:

1. **OpenTelemetry-native na ingestão** — todo dado de telemetria entra via OTLP
2. **Correlation-first no produto** — métricas, topologia e anomalias são correlacionados com releases e deploys
3. **Storage-aware na persistência** — dados agregados ficam no PostgreSQL (Product Store), dados crus em provider analítico configurável (ClickHouse ou Elastic)

### Por que Tempo/Loki/Grafana deixaram de ser a stack principal

O NexTraceOne não é apenas uma interface para ferramentas externas de observabilidade. O objetivo do produto é:

- coletar sinais técnicos dos ambientes
- armazenar logs, traces e métricas técnicas relevantes
- comparar comportamento entre ambientes produtivos e não produtivos
- alimentar mecanismos de análise interna
- alimentar a IA interna do NexTraceOne
- transformar telemetria em inteligência operacional, governança e análise de risco de release

Para suportar estes objetivos com autonomia e flexibilidade, a plataforma agora usa **provider de observabilidade configurável** (ClickHouse ou Elastic) em vez de uma stack fixa.

### Separação de preocupações

A plataforma trata **coleta**, **transporte**, **storage** e **análise** como preocupações separadas:

| Eixo | Exemplos |
|------|----------|
| **A. Coleta** | CLR Profiler (IIS), OpenTelemetry Collector (Kubernetes) |
| **B. Storage** | ClickHouse, Elastic |
| **C. Consumo** | Backend NexTraceOne, análise de release, IA interna |

Combinações suportadas:
- CLR Profiler + ClickHouse
- CLR Profiler + Elastic
- OpenTelemetry Collector + ClickHouse
- OpenTelemetry Collector + Elastic

---

## Provider de Observabilidade

### Escolha do Provider

A escolha é feita por configuração (`Telemetry:ObservabilityProvider:Provider`):

| Provider | Quando usar | Características |
|----------|------------|-----------------|
| **ClickHouse** | Default para desenvolvimento e deploy novo | Analítico colunar, alto throughput, TTL automático |
| **Elastic** | Empresa já possui stack Elastic | Integração com infra existente, ILM, full-text search |

### ClickHouse

- Volume persistente obrigatório (ClickHouse é stateful)
- Database dedicada `nextraceone_obs` com tabelas: `otel_logs`, `otel_traces`, `otel_metrics`
- TTL automático por tabela para retenção
- Schema: `build/clickhouse/init-schema.sql`

### Elastic

- Integra com stack Elastic já existente na empresa
- Configuração via endpoint + API key
- Índices com prefixo configurável (`nextraceone-logs-*`, `nextraceone-traces-*`)
- Retenção via ILM (Index Lifecycle Management)

---

## Modo de Coleta

| Modo | Quando usar | Características |
|------|------------|-----------------|
| **OpenTelemetry Collector** | Ambientes Kubernetes | Pipeline de ingestão, normalização, roteamento |
| **CLR Profiler** | Ambientes IIS/Windows | Auto-instrumentação .NET, menor intrusão |

---

## Product Store (PostgreSQL)

O PostgreSQL é o **store operacional** do produto — **exclusivo para dados transacionais e de domínio**.

### Tabelas (Schema `telemetry`)

| Tabela | Descrição | Retenção |
|--------|-----------|----------|
| `service_metrics_1m` | Métricas por minuto | 7 dias |
| `service_metrics_1h` | Métricas por hora | 90d hot + 365d warm |
| `observed_topology` | Topologia observada | 90 dias |
| `anomaly_snapshots` | Anomalias detectadas | 90d hot + 365d warm |
| `release_runtime_correlation` | Correlação deploy/runtime | 90d hot + 365d warm |
| `investigation_context` | Contextos investigativos | 90d hot + 365d warm |
| `telemetry_references` | Ponteiros para dados crus | Igual ao referenciado |

---

## Configuração (appsettings.json)

```json
{
  "Telemetry": {
    "ProductStore": {
      "ConnectionStringName": "NexTraceOne",
      "Schema": "telemetry"
    },
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": { "Enabled": true, "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs" },
      "Elastic": { "Enabled": false, "Endpoint": "", "ApiKey": "" }
    },
    "CollectionMode": {
      "ActiveMode": "OpenTelemetryCollector",
      "OpenTelemetryCollector": { "Enabled": true, "OtlpGrpcEndpoint": "http://otel-collector:4317" },
      "ClrProfiler": { "Enabled": false, "Mode": "IIS", "ProfilerType": "AutoInstrumentation" }
    },
    "Collector": { "OtlpGrpcEndpoint": "http://otel-collector:4317", "BatchSize": 8192 },
    "Retention": {
      "RawTraces": { "HotDays": 7, "WarmDays": 30 },
      "RawLogs": { "HotDays": 7, "WarmDays": 30 },
      "HourlyAggregates": { "HotDays": 90, "WarmDays": 365 },
      "AuditCompliance": { "HotDays": 365, "ColdDays": 2555 }
    }
  }
}
```

---

## Deploy Local

```bash
docker compose up -d
```

Stack: PostgreSQL + ClickHouse + OTel Collector + serviços NexTraceOne.

| Serviço | Porta |
|---------|-------|
| PostgreSQL | 5432 |
| ClickHouse HTTP | 8123 |
| OTel Collector gRPC | 4317 |
| OTel Collector HTTP | 4318 |
| ApiHost | 8080 |
| Frontend | 3000 |

---

## Abstração Interna

| Interface | Responsabilidade |
|-----------|-----------------|
| `IObservabilityProvider` | Consulta unificada de logs, traces e métricas |
| `ITelemetryQueryService` | Queries orientadas ao produto |
| `ICollectionModeStrategy` | Estratégia de coleta por ambiente |

---

## Testes

- **96 testes** em `NexTraceOne.BuildingBlocks.Observability.Tests`
- Configuração, providers, collection modes, retenção, modelos, correlação, OpenTelemetry readiness
