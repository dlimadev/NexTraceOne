# NexTraceOne — Arquitetura de Telemetria

## Visão Geral

A arquitetura de telemetria do NexTraceOne segue três princípios fundamentais:

1. **OpenTelemetry-native na ingestão** — todo dado de telemetria entra via OTLP
2. **Correlation-first no produto** — métricas, topologia e anomalias são correlacionados com releases e deploys
3. **Storage-aware na persistência** — dados agregados ficam no PostgreSQL (Product Store), dados crus em backends especializados (Telemetry Store)

---

## Arquitetura de Alto Nível

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FONTES DE TELEMETRIA                        │
│                                                                     │
│  .NET  │  Java  │  Node.js  │  IIS/Windows  │  Kubernetes  │  Ext. │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ OTLP (gRPC :4317 / HTTP :4318)
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│              OPENTELEMETRY COLLECTOR (Pipeline Central)              │
│                                                                     │
│  Receivers    │  Processors              │  Exporters               │
│  ─────────── │  ──────────────────────── │  ──────────────────────  │
│  otlp        │  memory_limiter           │  otlp/tempo (traces)     │
│  prometheus  │  batch                    │  loki (logs)             │
│  hostmetrics │  resourcedetection        │  otlp (métricas)         │
│              │  attributes/normalize     │                          │
│              │  filter/drop_noise        │  Connectors              │
│              │  tail_sampling            │  ──────────────────────  │
│              │  redaction (PII/LGPD)     │  spanmetrics             │
│              │  transform/correlation    │  (traces → métricas)     │
└──────────────┴──────────────┬────────────┴──────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
┌──────────────────┐ ┌──────────────┐ ┌──────────────────┐
│  PRODUCT STORE   │ │ TEMPO        │ │ LOKI             │
│  (PostgreSQL)    │ │ (Traces)     │ │ (Logs)           │
│                  │ │              │ │                  │
│ ▪ Métricas 1m   │ │ Traces crus  │ │ Logs crus        │
│ ▪ Métricas 1h   │ │ Retenção:    │ │ Retenção:        │
│ ▪ Topologia     │ │  hot: 7d     │ │  hot: 7d         │
│ ▪ Anomalias     │ │  warm: 30d   │ │  warm: 30d       │
│ ▪ Correlações   │ │              │ │                  │
│ ▪ Investigação  │ └──────────────┘ └──────────────────┘
│ ▪ Referências   │     ▲                    ▲
│   (ponteiros)───┼─────┘────────────────────┘
└──────────────────┘
       ▲
       │ Consumido por módulos:
       │ ▪ Módulo 3  — Graph / Topology
       │ ▪ Módulo 10 — Runtime Intelligence
       │ ▪ Módulo 11 — Cost Intelligence
       │ ▪ Módulo 12 — AI Orchestration
       │ ▪ Módulo 14 — Audit / Traceability
```

---

## Product Store (PostgreSQL)

O PostgreSQL é o **store operacional** do produto. Armazena **apenas dados agregados e metadados**, nunca traces/logs crus em volume.

### Tabelas do Product Store (Schema `telemetry`)

| Tabela | Descrição | Particionamento | Retenção |
|--------|-----------|-----------------|----------|
| `service_metrics_1m` | Métricas de serviço por minuto | Diário | 7 dias |
| `service_metrics_1h` | Métricas de serviço por hora | Mensal | 90 dias hot + 365 warm |
| `dependency_metrics_1m` | Métricas de dependência por minuto | Diário | 7 dias |
| `dependency_metrics_1h` | Métricas de dependência por hora | Mensal | 90 dias hot + 365 warm |
| `observed_topology` | Topologia observada agregada | — | 90 dias |
| `anomaly_snapshots` | Anomalias detectadas | — | 90 dias hot + 365 warm |
| `release_runtime_correlation` | Correlação deploy/runtime | — | 90 dias hot + 365 warm |
| `investigation_context` | Contextos investigativos | — | 90 dias hot + 365 warm |
| `telemetry_references` | Ponteiros para dados crus | — | Igual ao dado referenciado |

### Métricas Agregadas Disponíveis

- **Throughput**: req/min, req/hora
- **Error Rate**: % de erros, contagem de erros
- **Latência**: avg, p50, p95, p99, max (em ms)
- **Recursos**: CPU avg %, memória avg MB
- **Dependências**: call count, error rate, latência por dependência
- **Top N**: top serviços, top dependências, top operações

### Particionamento

Tabelas de métricas por minuto são particionadas por dia (DROP PARTITION para cleanup eficiente).
Tabelas de métricas por hora são particionadas por mês.

### Índices

Cada tabela agragada deve ter índices em:
- `(service_id, environment, interval_start)` — busca temporal por serviço
- `(environment, interval_start)` — busca temporal por ambiente
- `(tenant_id, environment, interval_start)` — isolamento multi-tenant

---

## Telemetry Store (Backends Especializados)

Os dados crus de traces e logs ficam em backends especializados, **não em PostgreSQL**.

### Traces → Grafana Tempo
- Armazenamento otimizado para traces distribuídos
- Busca por trace_id, service.name, duration, status
- Retenção: 7 dias hot, 30 dias warm

### Logs → Grafana Loki
- Armazenamento otimizado para logs estruturados
- Busca por labels, LogQL queries
- Retenção: 7 dias hot, 30 dias warm

### Navegação Product Store → Telemetry Store

O Product Store mantém `telemetry_references` com ponteiros para dados crus:
- `signal_type` (traces/logs)
- `external_id` (trace_id ou log stream)
- `backend_type` ("tempo" ou "loki")
- `access_uri` (URL para acesso direto)
- `correlation_id` (link para anomalia/release/investigação)

---

## Política de Retenção (Hot / Warm / Cold)

### Por Tipo de Sinal

| Tipo de Dado | Hot (SSD) | Warm (Disco) | Cold (Object Storage) |
|--------------|-----------|--------------|----------------------|
| Traces crus | 7 dias | 30 dias | — |
| Logs crus | 7 dias | 30 dias | — |
| Métricas 1m | 7 dias | — | — |
| Métricas 1h | 90 dias | 365 dias | — |
| Snapshots/Anomalias | 90 dias | 365 dias | — |
| Topologia | 90 dias | — | — |
| Audit/Compliance | 365 dias | — | 7 anos |

### Jobs de Consolidação

1. **Consolidação minuto → hora**: A cada hora, agrega dados de `service_metrics_1m` para `service_metrics_1h`
2. **Cleanup de minuto**: Após consolidação, remove partições de minuto com mais de 7 dias (DROP PARTITION)
3. **Cleanup de hora**: Remove partições de hora com mais de 455 dias (90 + 365)
4. **Cleanup de anomalias**: Remove anomalias resolvidas com mais de 455 dias
5. **Cleanup de topologia**: Remove arestas não observadas há mais de 90 dias
6. **Cleanup de referências**: Remove referências cujo dado original já expirou

---

## OpenTelemetry Collector

### Configuração de Referência

Arquivo: `build/otel-collector/otel-collector.yaml`

### Receivers
- **OTLP** (gRPC :4317, HTTP :4318) — receptor principal para todas as fontes
- **Prometheus** — scraping de métricas para workloads legados
- **Host Metrics** — CPU, memória, disco, rede do host

### Processors (ordem do pipeline)
1. **memory_limiter** — proteção contra OOM (512 MB limit, 128 MB spike)
2. **resourcedetection** — enriquecimento com metadata do host/cloud/k8s
3. **attributes/normalize** — normalização de nomes de atributos
4. **filter/drop_noise** — remoção de health checks e probes
5. **transform/correlation** — injeção de atributos NexTraceOne
6. **redaction** — sanitização de PII/tokens/passwords (LGPD/GDPR)
7. **tail_sampling** — amostragem inteligente (100% erros, 10% normal)
8. **batch** — agrupamento para eficiência de I/O

### Connectors
- **spanmetrics** — gera métricas automaticamente a partir de traces (RED metrics)

### Pipelines
- **Traces**: OTLP → processors → Tempo + SpanMetrics
- **Metrics**: OTLP + HostMetrics + SpanMetrics → processors → OTLP (Product Store aggregator)
- **Logs**: OTLP → processors → Loki

---

## Fontes Suportadas

| Plataforma | Método de Ingestão | Protocolo |
|------------|-------------------|-----------|
| .NET | OpenTelemetry SDK nativo | OTLP gRPC/HTTP |
| Java | OpenTelemetry Java Agent | OTLP gRPC/HTTP |
| Node.js | @opentelemetry/sdk-node | OTLP gRPC/HTTP |
| IIS / Windows | OTLP HTTP exporter ou Prometheus | OTLP HTTP / Prometheus |
| Kubernetes | kubeletstats + OTLP | OTLP gRPC |
| Sistemas externos | OTLP via API ou webhook | OTLP HTTP |

---

## Correlação

### Release/Deployment Markers

Quando um deploy é notificado via Ingestion API:
1. Cria-se um `ReleaseRuntimeCorrelation` no Product Store
2. Captura métricas pré-deploy (baseline: 30 min antes)
3. Monitora métricas pós-deploy (30 min depois)
4. Calcula `ImpactScore` (0.0 a 1.0) e `ImpactClassification`
5. Cria referências para traces/logs da janela pós-deploy

### Service/Environment Identity

Cada sinal de telemetria carrega:
- `service.name` — nome do serviço no catálogo
- `deployment.environment` — ambiente (production, staging, etc.)
- `service.namespace` — namespace NexTraceOne
- `nextraceone.tenant_id` — isolamento multi-tenant

### Investigation Context

Quando uma anomalia é detectada:
1. Cria-se um `InvestigationContext` com:
   - Anomalias correlacionadas
   - Releases recentes no serviço afetado
   - Serviços no blast radius
   - Referências para traces/logs crus
2. Gera-se `AiSummaryJson` para consumo pelo módulo AI Orchestration

---

## Módulos Consumidores

### Módulo 3 — Graph / Topology
- Consome: `ObservedTopologyEntry` (topologia observada via telemetria)
- Complementa o catálogo declarado com dependências descobertas automaticamente
- Detecta shadow dependencies

### Módulo 10 — Runtime Intelligence
- Consome: `ServiceMetricsSnapshot`, `DependencyMetricsSnapshot`, `AnomalySnapshot`
- Detecta drift de runtime e anomalias
- Correlaciona com releases

### Módulo 11 — Cost Intelligence
- Consome: `ServiceMetricsSnapshot`, `DependencyMetricsSnapshot`
- Calcula custo por serviço, por rota, por release
- Detecta anomalias de custo

### Módulo 12 — AI Orchestration
- Consome: `InvestigationContext`, `AiSummaryJson`
- Recebe bundles investigativos para análise automatizada
- Gera recomendações baseadas em padrões de anomalia

### Módulo 14 — Audit / Traceability
- Consome: `ReleaseRuntimeCorrelation`, `TelemetryReference`
- Evidência de impacto de deploys para evidence pack
- Rastreabilidade completa: quem/quando/o quê → impacto observado

---

## Segurança e Privacidade

### Sanitização (LGPD/GDPR)
- O processor `redaction` no Collector remove: Bearer tokens, passwords, CPF/CNPJ
- Atributos sensíveis são mascarados antes de chegar aos backends

### Tenant Isolation
- Todo dado de telemetria inclui `tenant_id`
- Queries no Product Store são sempre filtradas por tenant
- Telemetry Store usa labels de tenant para isolamento

### Volume Control
- `memory_limiter` previne OOM no Collector
- `tail_sampling` controla volume de traces (10% em produção)
- `filter/drop_noise` remove telemetria de health checks
- Retenção curta para dados crus controla custo de storage

---

## Deploy

### Docker Compose (Desenvolvimento/POC)

```bash
docker compose -f build/otel-collector/docker-compose.telemetry.yaml up -d
```

Stack inclui:
- OpenTelemetry Collector (:4317 gRPC, :4318 HTTP, :13133 health)
- Grafana Tempo (:3200 API)
- Grafana Loki (:3100 API)
- Grafana (:3000 UI — opcional)

### Produção Enterprise

Em produção, cada componente é configurado separadamente:
- Collector pode escalar horizontalmente (múltiplas instâncias com load balancer)
- Tempo e Loki podem usar object storage (S3/MinIO) para cold tier
- PostgreSQL Product Store pode usar replicação e connection pooling

---

## Configuração

Seção `Telemetry` no `appsettings.json`:

```json
{
  "Telemetry": {
    "ProductStore": {
      "ConnectionStringName": "NexTraceOne",
      "Schema": "telemetry",
      "EnableTimePartitioning": true
    },
    "TelemetryStore": {
      "TracesBackend": "tempo",
      "TracesEndpoint": "http://tempo:3200",
      "LogsBackend": "loki",
      "LogsEndpoint": "http://loki:3100"
    },
    "Collector": {
      "OtlpGrpcEndpoint": "http://otel-collector:4317",
      "OtlpHttpEndpoint": "http://otel-collector:4318",
      "MemoryLimitMb": 512,
      "BatchSize": 8192,
      "TracesSamplingRate": 0.1
    },
    "Retention": {
      "RawTraces": { "HotDays": 7, "WarmDays": 30 },
      "RawLogs": { "HotDays": 7, "WarmDays": 30 },
      "MinuteAggregates": { "HotDays": 7 },
      "HourlyAggregates": { "HotDays": 90, "WarmDays": 365 },
      "AuditCompliance": { "HotDays": 365, "ColdDays": 2555 }
    }
  }
}
```

---

## Testes

Os testes da fundação de telemetria cobrem:

- **57 testes** no projeto `NexTraceOne.BuildingBlocks.Observability.Tests`
- Configuração: defaults, Product Store vs Telemetry Store
- Retenção: hot/warm/cold, diferenciação bruto vs agregado
- Modelos: ServiceMetrics, DependencyMetrics, Topology, Anomaly
- Correlação: release markers, investigation context, referências
- OpenTelemetry readiness: activity sources, meters, naming conventions
- Arquitetura: separação de stores, schema dedicado, backends independentes
