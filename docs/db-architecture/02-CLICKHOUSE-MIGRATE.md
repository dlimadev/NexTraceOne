# 02 — Tabelas a migrar → ClickHouse (Store Primário)

> ClickHouse é o analytics store **primário**. Todos os schemas aqui definidos são os schemas
> de referência. O ficheiro [03-ELASTICSEARCH-MIGRATE.md](./03-ELASTICSEARCH-MIGRATE.md) define
> os índices Elasticsearch **equivalentes** para clientes que escolhem o store alternativo.

---

## Resumo executivo

| Módulo | Tabelas PG actuais | Tabelas ClickHouse |
|--------|-------------------|--------------------|
| Observability | 4 | 4 |
| AI Knowledge | 4 | 4 |
| Cost Management | 3 | 3 |
| SLO / Reliability | 5 | 5 |
| Analytics | 3 | 3 |
| Security | 2 | 2 |
| Developer Productivity | 4 | 4 |
| **Total** | **25** | **25** |

---

## 1. Módulo Observability

### 1.1 `obs_service_metrics_snapshots` → `service_metrics_snapshots`

**Justificação:** Ingestão contínua de métricas (CPU, memória, latência) por serviço. Nunca há UPDATE.
O padrão de query é sempre `WHERE service_id = ? AND ts BETWEEN ? AND ?` + agregações.

```sql
CREATE TABLE nextraceone_analytics.service_metrics_snapshots
(
    tenant_id        UUID,
    service_id       UUID,
    service_name     String,
    ts               DateTime64(3, 'UTC'),
    cpu_usage_pct    Float32,
    memory_usage_pct Float32,
    request_rate     Float64,
    error_rate       Float64,
    p50_latency_ms   Float32,
    p95_latency_ms   Float32,
    p99_latency_ms   Float32,
    metadata         String  -- JSON extra fields
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, service_id, ts)
TTL ts + INTERVAL 2 YEAR DELETE
SETTINGS index_granularity = 8192;
```

**Engine:** MergeTree — append-only, ordenado por tempo, TTL automático de 2 anos.
**Migration:** dual-write PG+CH durante 4 semanas, depois remover tabela PG.

---

### 1.2 `obs_runtime_snapshots` → `runtime_snapshots`

**Justificação:** Snapshots periódicos de runtime (heap, threads, GC). Alto volume por ambiente.

```sql
CREATE TABLE nextraceone_analytics.runtime_snapshots
(
    tenant_id         UUID,
    environment_id    UUID,
    service_id        UUID,
    ts                DateTime64(3, 'UTC'),
    heap_used_bytes   UInt64,
    heap_total_bytes  UInt64,
    thread_count      UInt32,
    gc_pause_ms       Float32,
    open_fds          UInt32,
    uptime_seconds    UInt64
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, environment_id, service_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;
```

---

### 1.3 `obs_reliability_snapshots` → `reliability_snapshots`

**Justificação:** Ponto-em-tempo de availability e MTTR por serviço. Analytics de confiabilidade.

```sql
CREATE TABLE nextraceone_analytics.reliability_snapshots
(
    tenant_id          UUID,
    service_id         UUID,
    ts                 DateTime64(3, 'UTC'),
    availability_pct   Float64,
    mttr_minutes       Float32,
    incident_count     UInt32,
    error_budget_pct   Float64,
    deployment_count   UInt32
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, service_id, ts)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

### 1.4 `obs_alert_firing_records` → `alert_firing_records`

**Justificação:** Histórico de alertas disparados. Nunca há UPDATE; é auditoria de eventos.
Query pattern: `WHERE tenant_id = ? AND fired_at BETWEEN ? AND ?`.

```sql
CREATE TABLE nextraceone_analytics.alert_firing_records
(
    tenant_id       UUID,
    alert_id        UUID,
    alert_name      String,
    severity        LowCardinality(String),  -- critical/warning/info
    fired_at        DateTime64(3, 'UTC'),
    resolved_at     Nullable(DateTime64(3, 'UTC')),
    duration_sec    Nullable(UInt32),
    labels          String,  -- JSON
    annotations     String   -- JSON
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(fired_at))
ORDER BY (tenant_id, fired_at, alert_id)
TTL fired_at + INTERVAL 1 YEAR DELETE;
```

---

## 2. Módulo AI Knowledge / Governance

### 2.1 `aig_token_usage_ledger` → `token_usage_ledger`

**Justificação:** Cada request de LLM gera uma linha. Volume proporcional ao uso de IA.
Queries: somas por período, por agente, por modelo. Nunca há UPDATE.

```sql
CREATE TABLE nextraceone_analytics.token_usage_ledger
(
    tenant_id          UUID,
    agent_id           UUID,
    agent_name         String,
    model_id           String,
    model_provider     LowCardinality(String),
    ts                 DateTime64(3, 'UTC'),
    prompt_tokens      UInt32,
    completion_tokens  UInt32,
    total_tokens       UInt32,
    cost_usd           Decimal(12, 6),
    latency_ms         UInt32,
    request_id         UUID,
    skill_id           Nullable(UUID)
)
ENGINE = SummingMergeTree((prompt_tokens, completion_tokens, total_tokens, cost_usd))
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, agent_id, model_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;
```

**Engine:** SummingMergeTree — permite `SELECT SUM(cost_usd)` eficiente via pre-agregação.

---

### 2.2 `aig_external_inference_records` → `external_inference_records`

**Justificação:** Registo de cada chamada a API externas de inferência (OpenAI, Anthropic, etc.).
Auditoria de custo + latência por provider.

```sql
CREATE TABLE nextraceone_analytics.external_inference_records
(
    tenant_id       UUID,
    ts              DateTime64(3, 'UTC'),
    provider        LowCardinality(String),
    model           String,
    endpoint        String,
    status_code     UInt16,
    prompt_tokens   UInt32,
    output_tokens   UInt32,
    cost_usd        Decimal(12, 6),
    latency_ms      UInt32,
    error_code      Nullable(String),
    request_id      UUID
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, provider, ts)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

### 2.3 `aig_model_prediction_samples` → `model_prediction_samples`

**Justificação:** Amostras de predições para drift detection e A/B testing de modelos. Alto volume.

```sql
CREATE TABLE nextraceone_analytics.model_prediction_samples
(
    tenant_id       UUID,
    model_id        UUID,
    model_version   String,
    ts              DateTime64(3, 'UTC'),
    input_hash      String,   -- hash do input para deduplicação
    confidence      Float32,
    prediction_ms   UInt32,
    label           Nullable(String),
    ground_truth    Nullable(String),
    correct         Nullable(UInt8)  -- 1/0/NULL
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, model_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;
```

---

### 2.4 `aig_benchmark_snapshots` → `benchmark_snapshots`

**Justificação:** Resultados de benchmark de agentes ao longo do tempo. Serie temporal de qualidade.

```sql
CREATE TABLE nextraceone_analytics.benchmark_snapshots
(
    tenant_id         UUID,
    agent_id          UUID,
    ts                DateTime64(3, 'UTC'),
    benchmark_score   Float32,
    accuracy          Float32,
    normalized_rating Float32,
    feedback_coverage Float32,
    rl_bonus          Float32,
    tier              LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, agent_id, ts)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

## 3. Módulo Cost Management

### 3.1 `cst_cost_records` → `cost_records`

**Justificação:** Cada evento de custo (infra, licença, token) gera uma linha. Queries de SUM por período.

```sql
CREATE TABLE nextraceone_analytics.cost_records
(
    tenant_id       UUID,
    cost_center_id  UUID,
    ts              DateTime64(3, 'UTC'),
    category        LowCardinality(String),  -- infrastructure/ai/license/people
    sub_category    String,
    resource_id     Nullable(UUID),
    resource_name   String,
    quantity        Float64,
    unit            LowCardinality(String),
    unit_cost_usd   Decimal(12, 6),
    total_cost_usd  Decimal(12, 6),
    currency        LowCardinality(String),
    tags            String  -- JSON
)
ENGINE = SummingMergeTree((quantity, total_cost_usd))
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, cost_center_id, category, ts)
TTL ts + INTERVAL 5 YEAR DELETE;
```

---

### 3.2 `cst_burn_rate_snapshots` → `burn_rate_snapshots`

**Justificação:** Snapshots periódicos de burn rate de budget. Série temporal de gestão financeira.

```sql
CREATE TABLE nextraceone_analytics.burn_rate_snapshots
(
    tenant_id         UUID,
    budget_id         UUID,
    ts                DateTime64(3, 'UTC'),
    allocated_usd     Decimal(14, 2),
    spent_usd         Decimal(14, 2),
    forecasted_usd    Decimal(14, 2),
    burn_rate_pct     Float32,
    days_remaining    UInt16,
    status            LowCardinality(String)  -- on-track/at-risk/over-budget
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, budget_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;
```

---

### 3.3 `cst_cost_allocation_events` → `cost_allocation_events`

**Justificação:** Log de cada evento de alocação de custo (chargeback). Auditoria financeira.

```sql
CREATE TABLE nextraceone_analytics.cost_allocation_events
(
    tenant_id       UUID,
    ts              DateTime64(3, 'UTC'),
    source_id       UUID,
    target_id       UUID,
    allocation_type LowCardinality(String),
    amount_usd      Decimal(12, 6),
    rule_id         UUID,
    notes           String
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, source_id)
TTL ts + INTERVAL 7 YEAR DELETE;
```

---

## 4. Módulo SLO / Reliability

### 4.1 `slo_error_budget_snapshots` → `error_budget_snapshots`

**Justificação:** Snapshots do error budget por SLO. Serie temporal que alimenta dashboards de SRE.

```sql
CREATE TABLE nextraceone_analytics.error_budget_snapshots
(
    tenant_id          UUID,
    slo_id             UUID,
    ts                 DateTime64(3, 'UTC'),
    target_pct         Float64,
    achieved_pct       Float64,
    error_budget_pct   Float64,
    burn_rate_1h       Float32,
    burn_rate_6h       Float32,
    burn_rate_24h      Float32,
    burn_rate_72h      Float32,
    status             LowCardinality(String)  -- healthy/at-risk/exhausted
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, slo_id, ts)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

### 4.2 `slo_sli_measurements` → `sli_measurements`

**Justificação:** Cada medição de SLI (latency, availability, throughput) é uma linha temporal.

```sql
CREATE TABLE nextraceone_analytics.sli_measurements
(
    tenant_id   UUID,
    slo_id      UUID,
    sli_type    LowCardinality(String),  -- latency/availability/throughput/error_rate
    ts          DateTime64(3, 'UTC'),
    value       Float64,
    good_events UInt64,
    bad_events  UInt64,
    total_events UInt64
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, slo_id, sli_type, ts)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

### 4.3–4.5 `slo_compliance_*` → tabelas de compliance de SLO

```sql
CREATE TABLE nextraceone_analytics.slo_compliance_daily
(
    tenant_id       UUID,
    slo_id          UUID,
    date            Date,
    compliance_pct  Float64,
    violations      UInt32,
    downtime_min    Float32
)
ENGINE = SummingMergeTree((violations, downtime_min))
PARTITION BY (tenant_id, toYYYYMM(date))
ORDER BY (tenant_id, slo_id, date)
TTL date + INTERVAL 3 YEAR DELETE;

CREATE TABLE nextraceone_analytics.slo_compliance_weekly
(
    tenant_id       UUID,
    slo_id          UUID,
    week_start      Date,
    compliance_pct  Float64,
    violations      UInt32,
    downtime_min    Float32
)
ENGINE = SummingMergeTree((violations, downtime_min))
PARTITION BY (tenant_id, toYYYYMM(week_start))
ORDER BY (tenant_id, slo_id, week_start)
TTL week_start + INTERVAL 5 YEAR DELETE;

CREATE TABLE nextraceone_analytics.slo_compliance_monthly
(
    tenant_id       UUID,
    slo_id          UUID,
    month_start     Date,
    compliance_pct  Float64,
    violations      UInt32,
    downtime_min    Float32
)
ENGINE = SummingMergeTree((violations, downtime_min))
PARTITION BY (tenant_id, toYear(month_start))
ORDER BY (tenant_id, slo_id, month_start)
TTL month_start + INTERVAL 10 YEAR DELETE;
```

---

## 5. Módulo Analytics / Product

### 5.1 `pan_analytics_events` → `analytics_events`

> Já existe em `build/clickhouse/analytics-schema.sql` como `pan_events`. Renomear para consistência.

**Justificação:** Eventos de produto (page view, feature use, click). Volume proporcional a DAU.

```sql
CREATE TABLE nextraceone_analytics.analytics_events
(
    tenant_id   UUID,
    event_id    UUID,
    ts          DateTime64(3, 'UTC'),
    user_id     Nullable(UUID),
    session_id  UUID,
    event_type  LowCardinality(String),
    entity_type LowCardinality(String),
    entity_id   Nullable(UUID),
    properties  String,  -- JSON
    ip_hash     String,  -- PII-safe hash
    user_agent  String,
    country     LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, event_type)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

### 5.2 `pan_dashboard_usage_events` → `dashboard_usage_events`

**Justificação:** Log de acesso a dashboards. Feed para recomendações e analytics de adopção.

```sql
CREATE TABLE nextraceone_analytics.dashboard_usage_events
(
    tenant_id       UUID,
    user_id         UUID,
    dashboard_id    UUID,
    ts              DateTime64(3, 'UTC'),
    action          LowCardinality(String),  -- view/filter/export/share
    duration_sec    Nullable(UInt32),
    filters_json    String
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, dashboard_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;
```

---

### 5.3 `pan_productivity_snapshots` → `productivity_snapshots`

**Justificação:** Snapshots de produtividade de developer por sprint/semana. DORA metrics.

```sql
CREATE TABLE nextraceone_analytics.productivity_snapshots
(
    tenant_id          UUID,
    team_id            UUID,
    ts                 DateTime64(3, 'UTC'),
    period_type        LowCardinality(String),  -- daily/weekly/sprint
    deploy_frequency   Float32,
    lead_time_hours    Float32,
    change_fail_rate   Float32,
    mttr_hours         Float32,
    pr_cycle_time_h    Float32,
    review_coverage    Float32
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, team_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;
```

---

## 6. Módulo Security

### 6.1 `sec_security_events` → `security_events`

**Justificação:** Stream de eventos de segurança (login, access denied, suspicious activity). SIEM-like.

```sql
CREATE TABLE nextraceone_analytics.security_events
(
    tenant_id     UUID,
    event_id      UUID,
    ts            DateTime64(3, 'UTC'),
    event_type    LowCardinality(String),
    severity      LowCardinality(String),  -- critical/high/medium/low/info
    actor_id      Nullable(UUID),
    actor_type    LowCardinality(String),  -- user/service/system
    resource_type LowCardinality(String),
    resource_id   Nullable(UUID),
    action        String,
    outcome       LowCardinality(String),  -- success/failure/blocked
    ip_address    String,
    country       LowCardinality(String),
    details       String  -- JSON
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, event_type, severity)
TTL ts + INTERVAL 7 YEAR DELETE;
```

---

### 6.2 `sec_threat_signals` → `threat_signals`

**Justificação:** Sinais de ameaça agrupados por sessão/IP. Analytics de anomalias.

```sql
CREATE TABLE nextraceone_analytics.threat_signals
(
    tenant_id     UUID,
    ts            DateTime64(3, 'UTC'),
    signal_type   LowCardinality(String),
    source_ip     String,
    actor_id      Nullable(UUID),
    risk_score    Float32,
    indicators    String,  -- JSON array
    correlated_to Nullable(UUID)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, signal_type)
TTL ts + INTERVAL 2 YEAR DELETE;
```

---

## 7. Módulo Developer Productivity

### 7.1 `dpx_agent_query_records` → `agent_query_records`

**Justificação:** Log de cada query feita a agentes de IA no IDE plugin. Alto volume, sem UPDATE.

```sql
CREATE TABLE nextraceone_analytics.agent_query_records
(
    tenant_id       UUID,
    user_id         UUID,
    agent_id        UUID,
    ts              DateTime64(3, 'UTC'),
    query_type      LowCardinality(String),
    response_ms     UInt32,
    tokens_used     UInt32,
    satisfied       Nullable(UInt8),  -- 1/0/NULL (thumbs up/down)
    ide_type        LowCardinality(String),  -- vscode/jetbrains/vim
    language        LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, agent_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;
```

---

### 7.2–7.4 `dpx_code_review_*` → tabelas de review analytics

```sql
CREATE TABLE nextraceone_analytics.code_review_cycles
(
    tenant_id        UUID,
    repo_id          UUID,
    pr_id            UUID,
    created_at       DateTime64(3, 'UTC'),
    merged_at        Nullable(DateTime64(3, 'UTC')),
    cycle_time_hours Float32,
    review_count     UInt16,
    comment_count    UInt32,
    author_id        UUID,
    size_lines       UInt32
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(created_at))
ORDER BY (tenant_id, repo_id, created_at)
TTL created_at + INTERVAL 2 YEAR DELETE;

CREATE TABLE nextraceone_analytics.deployment_records
(
    tenant_id        UUID,
    service_id       UUID,
    ts               DateTime64(3, 'UTC'),
    environment      LowCardinality(String),
    version          String,
    strategy         LowCardinality(String),  -- rolling/blue-green/canary
    duration_sec     UInt32,
    success          UInt8,
    triggered_by     LowCardinality(String)   -- manual/ci/schedule
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, service_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;

CREATE TABLE nextraceone_analytics.pipeline_run_records
(
    tenant_id     UUID,
    pipeline_id   UUID,
    run_id        UUID,
    ts            DateTime64(3, 'UTC'),
    status        LowCardinality(String),
    duration_sec  UInt32,
    stage         LowCardinality(String),
    triggered_by  LowCardinality(String),
    branch        String,
    commit_sha    String
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, pipeline_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;
```

---

## Estratégia de migração por tabela

| Tabela PG | Tabela ClickHouse | Fase | Estratégia |
|-----------|-------------------|------|------------|
| `obs_service_metrics_snapshots` | `service_metrics_snapshots` | 1 | dual-write 4s → remove PG |
| `obs_runtime_snapshots` | `runtime_snapshots` | 1 | dual-write 4s → remove PG |
| `obs_reliability_snapshots` | `reliability_snapshots` | 1 | dual-write 4s → remove PG |
| `obs_alert_firing_records` | `alert_firing_records` | 1 | dual-write 4s → remove PG |
| `aig_token_usage_ledger` | `token_usage_ledger` | 1 | dual-write 4s → remove PG |
| `aig_external_inference_records` | `external_inference_records` | 1 | dual-write 4s → remove PG |
| `aig_model_prediction_samples` | `model_prediction_samples` | 1 | dual-write 4s → remove PG |
| `aig_benchmark_snapshots` | `benchmark_snapshots` | 1 | dual-write 4s → remove PG |
| `cst_cost_records` | `cost_records` | 1 | dual-write 4s → remove PG |
| `cst_burn_rate_snapshots` | `burn_rate_snapshots` | 1 | dual-write 4s → remove PG |
| `cst_cost_allocation_events` | `cost_allocation_events` | 1 | dual-write 4s → remove PG |
| `slo_error_budget_snapshots` | `error_budget_snapshots` | 1 | dual-write 4s → remove PG |
| `slo_sli_measurements` | `sli_measurements` | 1 | dual-write 4s → remove PG |
| `slo_compliance_*` | `slo_compliance_daily/weekly/monthly` | 1 | dual-write 4s → remove PG |
| `pan_analytics_events` | `analytics_events` | 1 | dual-write 4s → remove PG |
| `pan_dashboard_usage_events` | `dashboard_usage_events` | 1 | dual-write 4s → remove PG |
| `pan_productivity_snapshots` | `productivity_snapshots` | 1 | dual-write 4s → remove PG |
| `sec_security_events` | `security_events` | 1 | dual-write 4s → remove PG |
| `sec_threat_signals` | `threat_signals` | 1 | dual-write 4s → remove PG |
| `dpx_agent_query_records` | `agent_query_records` | 1 | dual-write 4s → remove PG |
| `dpx_code_review_cycles` | `code_review_cycles` | 1 | dual-write 4s → remove PG |
| `dpx_deployment_records` | `deployment_records` | 1 | dual-write 4s → remove PG |
| `dpx_pipeline_run_records` | `pipeline_run_records` | 1 | dual-write 4s → remove PG |
| `aig_knowledge_documents` (content) | `knowledge_document_content` | 2 | dual-write 4s → remove PG index GIN |
| `ctm_contract_versions` (spec) | `contract_version_content` | 2 | dual-write 4s → remove PG index GIN |

**"4s"** = 4 semanas de dual-write com verificação de consistência antes de desligar PG.
