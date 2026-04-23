# NexTraceOne — Plano de Evolução de Infraestrutura

> **Data:** Abril 2026
> **Motivação:** Preparar a plataforma para biliões de eventos, ambientes on-premise
> distribuídos e visibilidade completa de infraestrutura (serviços + hosts).

---

## Contexto

O NexTraceOne usa actualmente:
- **PostgreSQL 16** com pgvector — base de dados única para todos os 28 DbContexts
- **Elasticsearch 8.17** — provider de observabilidade (logs, métricas, traces)
- **OpenTelemetry Collector** — pipeline de ingestão de telemetria

À medida que o volume cresce para biliões de eventos e o produto é adoptado em
ambientes on-premise com múltiplos servidores, a arquitectura actual tem limites
conhecidos que este plano endereça.

---

## As 4 Fases do Plano

| Fase | Documento | Foco | Estimativa |
|------|-----------|------|-----------|
| **Fase 1** | [INFRA-PHASE-1-POSTGRES-HARDENING.md](./INFRA-PHASE-1-POSTGRES-HARDENING.md) | PostgreSQL: PgBouncer, particionamento, read replica, Redis | 2–3 semanas |
| **Fase 2** | [INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md](./INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md) | ClickHouse como provider principal de observabilidade | 3–4 semanas |
| **Fase 3** | [INFRA-PHASE-3-HOST-INFRASTRUCTURE.md](./INFRA-PHASE-3-HOST-INFRASTRUCTURE.md) | Camada de infraestrutura de hosts (novo módulo) | 2–3 semanas |
| **Fase 4** | [INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md](./INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md) | Completar topologia: time-travel UI, alertas real-time, discovery contínuo | 2–3 semanas |

**Duração total estimada:** 9–13 semanas (as fases 1 e 2 podem correr em paralelo)

---

## Problema por Problema — Para Onde Vai a Solução

### Problemas de Base de Dados

| Problema Identificado | Fase | Solução |
|-----------------------|------|---------|
| `Maximum Pool Size=20` demasiado baixo | F1 | PgBouncer transaction pooling |
| Tabelas de alto volume sem particionamento | F1 | Partições por tempo (EF Core migrations) |
| Sem read replica para queries pesadas | F1 | Réplica de leitura + routing no DbContext |
| Cache hot data ausente | F1 | Redis (IDistributedCache) |
| Elasticsearch caro e lento para analytics | F2 | ClickHouse como provider principal |
| TelemetryStore snapshots em PostgreSQL | F2 | Migrar para ClickHouse |
| ProductAnalytics events em PostgreSQL | F2 | Migrar para ClickHouse |

### Problemas de Visibilidade

| Problema Identificado | Fase | Solução |
|-----------------------|------|---------|
| Sem camada de hosts na topologia | F3 | HostAsset + ServiceDeployment entities |
| Sem métricas de host (CPU, RAM, disco) | F3 | HostMetricsSnapshot via OTEL hostmetrics |
| Sem correlação serviço → host | F3 | DeployedTo edges no grafo (já existe EdgeType) |
| Time-travel no grafo sem UI | F4 | Slider temporal sobre GraphSnapshot |
| PropagationRisk é query on-demand | F4 | Push via SignalR/WebSocket |
| Discovery de serviços é manual | F4 | Quartz job contínuo + background polling |
| NodeHealthRecord sem pipeline real | F4 | Pipeline de actualização automático |

---

## Dependências Entre Fases

```
Fase 1 (PostgreSQL Hardening)
  │── pode começar imediatamente
  └── sem dependências externas

Fase 2 (ClickHouse Migration)
  │── pode correr em paralelo com Fase 1
  │── ClickHouseAnalyticsWriter já existe (Wave Z.3 ✅)
  └── schema em build/clickhouse/ já existe ✅

Fase 3 (Host Infrastructure)
  │── requer Fase 1 completa (PgBouncer configurado)
  │── requer OTEL Collector operacional (já existe ✅)
  └── usa DeployedTo EdgeType já existente ✅

Fase 4 (Topology Completions)
  │── requer Fase 3 (HostAsset para overlay de hosts)
  │── GraphSnapshot já existe ✅
  └── SignalR a adicionar em Fase 4
```

---

## O que NÃO está no plano (decisão deliberada)

- **Profiling de processos individuais** (PIDs, threads) — complexidade sem ROI claro
- **Topologia de rede a nível IP/porta** — domínio de ferramentas de segurança
- **Auto-descoberta de hosts por scan de rede** — sensível e fora do âmbito
- **Migração de dados históricos do Elasticsearch** — clean slate no ClickHouse; ES mantém dados antigos até expirar TTL

---

## Arquitectura Final Alvo

```
Dados Relacionais / Transaccionais
  └── PostgreSQL 16 + pgvector
        ├── PgBouncer (connection pooling)
        ├── Primary (escritas)
        └── Read Replica (queries pesadas)

Observabilidade de Alto Volume
  └── ClickHouse (logs, traces, métricas OTEL)
        ├── otel_logs (TTL 30 dias)
        ├── otel_traces (TTL 30 dias)
        └── otel_metrics_* (TTL 90 dias)

Cache e Performance
  └── Redis
        ├── Hot data (catálogo, perfis de serviço)
        ├── Sessões e rate limiting
        └── Pub/Sub para alertas real-time

Infraestrutura de Hosts (novo)
  └── PostgreSQL (HostAsset, ServiceDeployment)
  └── ClickHouse (HostMetricsSnapshot — alta frequência)

Topology Intelligence
  └── GraphSnapshot (PostgreSQL — time-travel)
  └── SignalR Hub (alertas de propagação real-time)
  └── Quartz Job (discovery contínuo — 5 min)
```
