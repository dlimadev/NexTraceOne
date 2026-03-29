# Onda 7 — Batch Intelligence

> **Duração estimada:** 4-5 semanas
> **Dependências:** Ondas 1 e 2
> **Risco:** Médio — volumes de dados batch podem ser altos
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)
> **Nota:** Pode ser executada em **paralelo com Onda 8** (Messaging Intelligence)

---

## Objetivo

Intelligence completa sobre batch jobs — definições, chains, execuções, SLA, baselines e detecção de regressão. Batch é central em bancos e seguradoras, e é uma das capabilities mais procuradas por operações enterprise.

---

## Entregáveis

- [ ] `BatchJobDefinition` + `BatchChain` entities e CRUD
- [ ] `BatchExecution` ingestão e persistência em ClickHouse
- [ ] `BatchBaseline` calculation (duração normal, janela normal)
- [ ] `BatchSlaPolicy` e SLA evaluation automática
- [ ] Duration regression detection
- [ ] Batch chain dependency tracking e visualização
- [ ] Batch Intelligence Dashboard
- [ ] Batch Job Detail page com execution history
- [ ] Batch Chain View
- [ ] Background jobs: poller e SLA evaluator

---

## Impacto Backend

### Novas Entidades de Domínio

Localização: `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/BatchIntelligence/`

| Entidade | Tipo | Descrição |
|---|---|---|
| `BatchJobDefinition` | Aggregate Root | Definição de job batch (nome, JCL, owner, schedule, SLA) |
| `BatchChain` | Entity | Chain de jobs com sequência e dependências |
| `BatchChainStep` | Entity | Step individual dentro de uma chain |
| `BatchBaseline` | Entity | Baseline de execução normal (duração, janela) |
| `BatchSlaPolicy` | Entity | Política de SLA (horário esperado, tolerância) |

| Enum | Valores |
|---|---|
| `BatchJobStatus` | Scheduled, Running, Completed, Failed, Abended, Cancelled, Waiting |
| `BatchJobCriticality` | BusinessCritical, Operational, Informational |
| `BatchSlaStatus` | OnTime, AtRisk, Breached, Unknown |

### Commands e Queries

| Feature | Tipo | Descrição |
|---|---|---|
| `RegisterBatchJobDefinition` | Command | Regista novo job no catálogo |
| `RegisterBatchChain` | Command | Regista chain com steps |
| `IngestBatchExecution` | Command | Recebe execução (via Ingestion API ou event) |
| `RecordBatchBaseline` | Command | Calcula/atualiza baseline de duração |
| `EvaluateBatchSla` | Command | Avalia SLA para execução |
| `DetectBatchDurationRegression` | Query | Compara execução com baseline |
| `GetBatchExecutionHistory` | Query | Histórico de execuções (ClickHouse) |
| `GetBatchChainStatus` | Query | Estado atual de chain |
| `GetBatchDashboard` | Query | Dados para dashboard |
| `GetBatchJobDetail` | Query | Detalhe completo de um job |

### Background Jobs

| Job | Intervalo | Descrição |
|---|---|---|
| `BatchSlaEvaluatorJob` | 5 min | Avalia SLA de jobs completados recentemente |
| `BatchBaselineCalculatorJob` | Diário | Recalcula baselines com dados dos últimos N dias |

### Batch Baseline Calculation

```
Baseline = {
    MedianDuration: P50 das últimas N execuções com sucesso
    P95Duration: P95 das últimas N execuções com sucesso
    NormalWindow: [min_start, max_end] das últimas N execuções
    CalculatedAt: timestamp
    SampleSize: N
}

Regression = Duration atual > P95Duration × 1.2 (configurável)
```

### SLA Evaluation

```
SLA Policy = {
    ExpectedCompletionTime: "06:00" (hora UTC)
    ToleranceMinutes: 30
    BusinessCriticality: Critical
}

SLA Status:
  - OnTime: completou antes de ExpectedCompletionTime
  - AtRisk: em execução e dentro de janela de tolerância
  - Breached: não completou até ExpectedCompletionTime + Tolerance
  - Unknown: sem dados suficientes
```

---

## Impacto Base de Dados

### Novas Tabelas PostgreSQL (prefixo `ops_`)

| Tabela | Descrição |
|---|---|
| `ops_batch_job_definitions` | Catálogo de batch jobs |
| `ops_batch_chains` | Chains de jobs |
| `ops_batch_chain_steps` | Steps dentro de chains |
| `ops_batch_baselines` | Baselines de execução |
| `ops_batch_sla_policies` | Políticas de SLA |

### Novas Tabelas ClickHouse (schema `nextraceone_analytics`)

| Tabela | Descrição |
|---|---|
| `ops_batch_executions` | Execuções individuais (alto volume) |
| `ops_batch_step_executions` | Execuções por step (opcional) |
| `ops_batch_daily_stats` | Agregação diária por job/chain |
| `ops_batch_sla_evaluations` | Histórico de avaliações SLA |

#### `ops_batch_executions` Schema

```sql
CREATE TABLE nextraceone_analytics.ops_batch_executions (
    execution_id String,
    tenant_id String,
    job_name LowCardinality(String),
    job_id String,
    chain_name LowCardinality(String),
    system_name LowCardinality(String),
    lpar_name LowCardinality(String),
    status LowCardinality(String),
    return_code String,
    started_at DateTime64(3),
    completed_at Nullable(DateTime64(3)),
    duration_ms UInt64,
    step_count UInt16,
    criticality LowCardinality(String),
    sla_status LowCardinality(String),
    environment LowCardinality(String),
    metadata_json String CODEC(ZSTD(1)),
    occurred_at DateTime64(3) CODEC(Delta, ZSTD(1)),
    occurred_date Date DEFAULT toDate(occurred_at)
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(occurred_date))
ORDER BY (tenant_id, job_name, occurred_at)
TTL occurred_date + INTERVAL 365 DAY;
```

---

## Impacto Frontend

### Nova Página: Batch Intelligence Dashboard

**Rota:** `/operations/batch`
**Persona:** Operations, Tech Lead

**Componentes:**

| Componente | Descrição |
|---|---|
| `BatchDashboardSummary` | Cards: Total Jobs, Running, Failed Today, SLA Breaches |
| `BatchSlaOverview` | Gráfico: % on-time vs at-risk vs breached |
| `BatchDurationTrends` | Gráfico temporal: duração por job (com baseline overlay) |
| `BatchFailedJobsList` | Lista de jobs falhos recentes com return code e link |
| `BatchChainStatusList` | Lista de chains com status por step |
| `BatchCriticalityFilter` | Filtro por criticidade: BusinessCritical, Operational, All |

### Nova Página: Batch Job Detail

**Rota:** `/operations/batch/:jobId`
**Persona:** Operations

**Secções:**
1. **Job Info** — nome, sistema, team, domain, criticality, SLA
2. **Execution History** — timeline com execuções recentes
3. **Duration Chart** — gráfico com baseline overlay (ECharts)
4. **SLA Status** — semáforo SLA com histórico
5. **Dependencies** — jobs upstream/downstream
6. **Chain Context** — posição na chain (se aplicável)
7. **Recent Incidents** — incidentes correlacionados

### Nova Página: Batch Chain View

**Rota:** `/operations/batch/chains/:chainId`
**Persona:** Operations, Architect

**Componentes:**
- `BatchChainDiagram` — diagrama de chain com status por step
- `CriticalPathHighlight` — caminho crítico destacado
- `ChainExecutionTimeline` — timeline de execução da chain

### Extensão do Sidebar

```
Operations
  ├── Incidents             (existente)
  ├── Runbooks              (existente)
  ├── Reliability           (existente)
  ├── Automation            (existente)
  ├── Runtime Intelligence  (existente)
  └── Batch Intelligence    ← NOVO
```

---

## Testes

### Testes Unitários (~50)
- Entities: criação, validação, estado
- Baseline calculation: mediana, P95, janela
- SLA evaluation: OnTime, AtRisk, Breached
- Duration regression: detecção com threshold

### Testes de Integração (~30)
- Ingestão de execução → persistência → correlação
- Baseline calculation com dados históricos
- SLA evaluation automática
- Dashboard query com dados reais

---

## Critérios de Aceite

1. ✅ Dashboard mostra visão consolidada de batch com métricas
2. ✅ SLA breaches visíveis com alertas
3. ✅ Duration regression detectada automaticamente
4. ✅ Chain dependencies visualizadas em diagrama
5. ✅ Drill-down de job para execuções individuais
6. ✅ Baseline calculado com dados históricos
7. ✅ Background jobs funcionais (SLA evaluator, baseline calculator)

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Volume de execuções batch alto | Média | ClickHouse com TTL. Agregação daily |
| Baselines imprecisas com poucos dados | Média | Mínimo de N execuções para baseline válida |
| Chains complexas com centenas de steps | Média | Paginação. Agrupamento. Lazy loading |

---

## Stories

| ID | Story | Prioridade |
|---|---|---|
| W7-S01 | Criar `BatchJobDefinition` entity + CRUD | P0 |
| W7-S02 | Criar `BatchChain` + `BatchChainStep` entities | P1 |
| W7-S03 | Criar `BatchIntelligenceDbContext` com migrações | P0 |
| W7-S04 | Implementar `IngestBatchExecution` command | P1 |
| W7-S05 | Criar tabelas ClickHouse para batch | P1 |
| W7-S06 | Implementar `RecordBatchBaseline` command | P1 |
| W7-S07 | Criar `BatchSlaPolicy` entity + evaluation | P1 |
| W7-S08 | Implementar `DetectBatchDurationRegression` | P2 |
| W7-S09 | Criar `BatchSlaEvaluatorJob` background worker | P1 |
| W7-S10 | Criar `BatchBaselineCalculatorJob` | P2 |
| W7-S11 | Criar Batch Intelligence Dashboard frontend | P1 |
| W7-S12 | Criar Batch Job Detail page | P1 |
| W7-S13 | Criar Batch Chain View page | P2 |
| W7-S14 | Atualizar sidebar com "Batch Intelligence" | P1 |
| W7-S15 | Testes unitários (~50) | P1 |
| W7-S16 | Testes de integração (~30) | P2 |
