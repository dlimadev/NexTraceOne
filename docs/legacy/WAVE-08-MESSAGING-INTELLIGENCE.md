# Onda 8 — Messaging Intelligence

> **Duração estimada:** 3-4 semanas
> **Dependências:** Ondas 1 e 2
> **Risco:** Médio — topologia MQ pode ser complexa
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)
> **Nota:** Pode ser executada em **paralelo com Onda 7** (Batch Intelligence)

---

## Objetivo

Intelligence completa sobre IBM MQ — topology, performance, anomalias. MQ é o backbone de integração em ambientes enterprise e precisa de visibilidade operacional dedicada.

---

## Entregáveis

- [ ] `QueueManagerDefinition`, `QueueDefinition`, `ChannelDefinition` entities e CRUD
- [ ] MQ statistics ingestão e persistência em ClickHouse
- [ ] MQ topology snapshot
- [ ] Queue depth monitoring e trending
- [ ] DLQ (Dead Letter Queue) monitoring
- [ ] Stuck message detection
- [ ] MQ anomaly detection
- [ ] MQ Intelligence Dashboard
- [ ] MQ Topology Viewer

---

## Impacto Backend

### Novas Entidades de Domínio

Localização: `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/MessagingIntelligence/`

| Entidade | Tipo | Descrição |
|---|---|---|
| `QueueManagerDefinition` | Aggregate Root | Queue Manager (nome, plataforma, host, port, versão) |
| `QueueDefinition` | Entity | Queue (nome, tipo, max depth, manager) |
| `ChannelDefinition` | Entity | Channel (nome, tipo, conexão) |
| `MqContract` | Entity | Contrato de mensagem MQ (descriptor, payload format) |
| `MqTopologySnapshot` | Entity | Snapshot da topologia num momento |

| Enum | Valores |
|---|---|
| `QueueType` | Local, Remote, Alias, Model, Cluster, DeadLetter, Transmission |
| `ChannelType` | Sender, Receiver, Server, Requester, ClusterSender, ClusterReceiver |
| `MqPlatform` | zOS, Distributed, Appliance |
| `MqHealthStatus` | Healthy, Degraded, Unhealthy, Critical |

### Commands e Queries

| Feature | Tipo | Descrição |
|---|---|---|
| `RegisterQueueManager` | Command | Regista queue manager |
| `RegisterQueue` | Command | Regista queue com metadata |
| `RegisterChannel` | Command | Regista channel |
| `IngestMqStatistics` | Command | Recebe statistics via Ingestion API |
| `SnapshotMqTopology` | Command | Cria snapshot da topologia |
| `DetectMqAnomalies` | Query | Detecta anomalias (depth, DLQ, stuck) |
| `GetMqDashboard` | Query | Dados para dashboard |
| `GetQueueHealthHistory` | Query | Histórico de saúde de queue (ClickHouse) |
| `GetMqTopology` | Query | Topologia completa ou filtrada |

### Background Jobs

| Job | Intervalo | Descrição |
|---|---|---|
| `MqAnomalyDetectorJob` | 1 min | Avalia anomalias com base em statistics recentes |

### Anomaly Detection Rules

| Anomalia | Condição | Severidade |
|---|---|---|
| **Queue Depth Warning** | depth > maxDepth × 0.80 | Warning |
| **Queue Depth Critical** | depth > maxDepth × 0.95 | Critical |
| **DLQ Messages** | DLQ depth > 0 | Warning |
| **DLQ Growth** | DLQ depth crescente por N intervalos | Critical |
| **Stuck Messages** | Queue depth sem variação por N intervalos com consumers activos | Warning |
| **Channel Down** | Channel status não "Running" | Critical |
| **Throughput Drop** | Enqueue/dequeue rate < baseline × 0.5 | Warning |

---

## Impacto Base de Dados

### Novas Tabelas PostgreSQL (prefixo `ops_`)

| Tabela | Descrição |
|---|---|
| `ops_queue_managers` | Queue Manager definitions |
| `ops_queue_definitions` | Queue definitions |
| `ops_channel_definitions` | Channel definitions |
| `ops_mq_contracts` | MQ message contracts |
| `ops_mq_topology_snapshots` | Topology snapshots |

### Novas Tabelas ClickHouse (schema `nextraceone_analytics`)

| Tabela | Descrição |
|---|---|
| `ops_mq_statistics` | MQ statistics/accounting (alto volume) |
| `ops_mq_queue_depth_history` | Queue depth time series |
| `ops_mq_channel_stats` | Channel statistics |
| `ops_mq_anomaly_events` | Anomalias detectadas |

#### `ops_mq_statistics` Schema

```sql
CREATE TABLE nextraceone_analytics.ops_mq_statistics (
    stat_id String,
    tenant_id String,
    queue_manager LowCardinality(String),
    queue_name LowCardinality(String),
    channel_name LowCardinality(String),
    queue_depth UInt32,
    max_depth UInt32,
    enqueue_count UInt64,
    dequeue_count UInt64,
    bytes_in UInt64,
    bytes_out UInt64,
    oldest_message_age_seconds UInt32,
    consumer_count UInt16,
    producer_count UInt16,
    dlq_depth UInt32,
    platform LowCardinality(String),
    environment LowCardinality(String),
    collected_at DateTime64(3) CODEC(Delta, ZSTD(1)),
    collected_date Date DEFAULT toDate(collected_at)
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(collected_date))
ORDER BY (tenant_id, queue_manager, queue_name, collected_at)
TTL collected_date + INTERVAL 90 DAY;
```

#### `ops_mq_queue_depth_history` — Materialized View

```sql
CREATE MATERIALIZED VIEW nextraceone_analytics.ops_mq_queue_depth_hourly
ENGINE = AggregatingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(hour))
ORDER BY (tenant_id, queue_manager, queue_name, hour)
AS SELECT
    tenant_id,
    queue_manager,
    queue_name,
    toStartOfHour(collected_at) AS hour,
    avg(queue_depth) AS avg_depth,
    max(queue_depth) AS max_depth,
    min(queue_depth) AS min_depth,
    avg(enqueue_count) AS avg_enqueue,
    avg(dequeue_count) AS avg_dequeue
FROM nextraceone_analytics.ops_mq_statistics
GROUP BY tenant_id, queue_manager, queue_name, hour;
```

---

## Impacto Frontend

### Nova Página: MQ Intelligence Dashboard

**Rota:** `/operations/messaging`
**Persona:** Operations, Architect

**Componentes:**

| Componente | Descrição |
|---|---|
| `MqDashboardSummary` | Cards: Queue Managers, Queues, Channels, Anomalies |
| `MqQueueDepthHeatmap` | Heatmap de queue depth por fila (ECharts) |
| `MqThroughputChart` | Gráfico de enqueue/dequeue rates |
| `MqDlqAlerts` | Lista de DLQ com messages count |
| `MqAnomalyList` | Lista de anomalias recentes |
| `MqChannelStatusGrid` | Grid de channels com status |

### Nova Página: MQ Queue Manager Detail

**Rota:** `/operations/messaging/:managerId`
**Persona:** Operations

**Secções:**
1. **Manager Info** — nome, host, port, versão, plataforma
2. **Queues** — lista de queues com depth e status
3. **Channels** — lista de channels com status
4. **Statistics** — métricas históricas (depth, throughput)
5. **Anomalies** — anomalias recentes
6. **Topology** — sub-grafo de topology para este manager

### Nova Página: MQ Topology Viewer

**Rota:** `/operations/messaging/topology`
**Persona:** Architect

**Componentes:**
- `MqTopologyGraph` — grafo visual (ECharts) com queue managers, queues, channels
- `MqTopologyFilter` — filtros por manager, environment, platform
- `MqTopologyNodeDetail` — panel com detalhe ao clicar num nó
- `MqTopologyStatusOverlay` — overlay de estado (Healthy/Degraded/Unhealthy)

### Extensão do Sidebar

```
Operations
  ├── ...existentes...
  ├── Batch Intelligence       (Onda 7)
  └── Messaging Intelligence   ← NOVO
```

---

## Testes

### Testes Unitários (~40)
- Entities: criação, validação
- Anomaly detection: cada regra
- Statistics aggregation
- Topology snapshot

### Testes de Integração (~20)
- Ingestão de statistics → persistência → query
- Anomaly detection → incident creation
- Dashboard query com dados reais
- Topology snapshot e retrieval

---

## Critérios de Aceite

1. ✅ Topology viewer mostra MQ landscape completo
2. ✅ Queue depth alertas funcionais (warning + critical)
3. ✅ DLQ monitoring ativo com alertas
4. ✅ Stuck message detection funcional
5. ✅ Dashboard consolidado com métricas operacionais
6. ✅ Histórico de queue depth consultável
7. ✅ Anomaly events registados e visíveis

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Topologia MQ complexa (centenas de queues) | Média | Filtros. Agrupamento por cluster/manager. Paginação |
| Volume de statistics alto | Média | ClickHouse com TTL 90 dias. Materialized views |
| False positives em anomaly detection | Média | Thresholds configuráveis. Silence/snooze rules |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W8-S01 | Criar `QueueManagerDefinition` entity + CRUD | P0 |
| W8-S02 | Criar `QueueDefinition` + `ChannelDefinition` entities | P1 |
| W8-S03 | Criar `MessagingIntelligenceDbContext` com migrações | P0 |
| W8-S04 | Implementar `IngestMqStatistics` command | P1 |
| W8-S05 | Criar tabelas ClickHouse para MQ | P1 |
| W8-S06 | Criar materialized view para hourly aggregation | P2 |
| W8-S07 | Implementar anomaly detection rules | P1 |
| W8-S08 | Criar `MqAnomalyDetectorJob` background worker | P1 |
| W8-S09 | Implementar `SnapshotMqTopology` | P2 |
| W8-S10 | Criar MQ Intelligence Dashboard frontend | P1 |
| W8-S11 | Criar MQ Queue Manager Detail page | P1 |
| W8-S12 | Criar MQ Topology Viewer page | P2 |
| W8-S13 | Atualizar sidebar com "Messaging Intelligence" | P1 |
| W8-S14 | Testes unitários (~40) | P1 |
| W8-S15 | Testes de integração (~20) | P2 |
