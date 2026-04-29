# 07 — Templates de Schema ClickHouse

> Scripts SQL prontos a usar para criar todas as tabelas do `nextraceone_analytics` database.
> Estes scripts complementam o `build/clickhouse/analytics-schema.sql` já existente.
> Para Elasticsearch, usar os index templates do ficheiro [03-ELASTICSEARCH-MIGRATE.md](./03-ELASTICSEARCH-MIGRATE.md).

---

## Ficheiro de inicialização completo

**Localização sugerida:** `build/clickhouse/domain-analytics-schema.sql`

```sql
-- ============================================================
-- NexTraceOne Analytics — ClickHouse Domain Schema
-- Database: nextraceone_analytics
-- Criado para complementar: build/clickhouse/analytics-schema.sql
-- ============================================================

CREATE DATABASE IF NOT EXISTS nextraceone_analytics;
USE nextraceone_analytics;

-- ============================================================
-- MÓDULO: OBSERVABILITY
-- ============================================================

CREATE TABLE IF NOT EXISTS service_metrics_snapshots
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
    metadata         String DEFAULT '{}'
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, service_id, ts)
TTL ts + INTERVAL 2 YEAR DELETE
SETTINGS index_granularity = 8192;

-- -------

CREATE TABLE IF NOT EXISTS runtime_snapshots
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

-- -------

CREATE TABLE IF NOT EXISTS reliability_snapshots
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

-- -------

CREATE TABLE IF NOT EXISTS alert_firing_records
(
    tenant_id       UUID,
    alert_id        UUID,
    alert_name      String,
    severity        LowCardinality(String),
    fired_at        DateTime64(3, 'UTC'),
    resolved_at     Nullable(DateTime64(3, 'UTC')),
    duration_sec    Nullable(UInt32),
    labels          String DEFAULT '{}',
    annotations     String DEFAULT '{}'
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(fired_at))
ORDER BY (tenant_id, fired_at, alert_id)
TTL fired_at + INTERVAL 1 YEAR DELETE;

-- ============================================================
-- MÓDULO: AI KNOWLEDGE / GOVERNANCE
-- ============================================================

CREATE TABLE IF NOT EXISTS token_usage_ledger
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
ENGINE = SummingMergeTree((prompt_tokens, completion_tokens, total_tokens))
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, agent_id, model_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS external_inference_records
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

-- -------

CREATE TABLE IF NOT EXISTS model_prediction_samples
(
    tenant_id       UUID,
    model_id        UUID,
    model_version   String,
    ts              DateTime64(3, 'UTC'),
    input_hash      String,
    confidence      Float32,
    prediction_ms   UInt32,
    label           Nullable(String),
    ground_truth    Nullable(String),
    correct         Nullable(UInt8)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, model_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS benchmark_snapshots
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

-- -------

CREATE TABLE IF NOT EXISTS knowledge_document_content
(
    document_id   UUID,
    tenant_id     UUID,
    title         String,
    content       String,
    summary       String,
    tags          Array(String),
    source_type   LowCardinality(String),
    created_at    DateTime64(3, 'UTC'),
    updated_at    DateTime64(3, 'UTC'),
    -- Full-text index via bloom filter
    INDEX idx_content_title title TYPE tokenbf_v1(32768, 3, 0) GRANULARITY 1,
    INDEX idx_content_body  content TYPE tokenbf_v1(65536, 3, 0) GRANULARITY 1
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(created_at))
ORDER BY (tenant_id, document_id)
TTL updated_at + INTERVAL 5 YEAR DELETE;

-- ============================================================
-- MÓDULO: COST MANAGEMENT
-- ============================================================

CREATE TABLE IF NOT EXISTS cost_records
(
    tenant_id       UUID,
    cost_center_id  UUID,
    ts              DateTime64(3, 'UTC'),
    category        LowCardinality(String),
    sub_category    String,
    resource_id     Nullable(UUID),
    resource_name   String,
    quantity        Float64,
    unit            LowCardinality(String),
    unit_cost_usd   Decimal(12, 6),
    total_cost_usd  Decimal(12, 6),
    currency        LowCardinality(String),
    tags            String DEFAULT '{}'
)
ENGINE = SummingMergeTree((quantity, total_cost_usd))
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, cost_center_id, category, ts)
TTL ts + INTERVAL 5 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS burn_rate_snapshots
(
    tenant_id         UUID,
    budget_id         UUID,
    ts                DateTime64(3, 'UTC'),
    allocated_usd     Decimal(14, 2),
    spent_usd         Decimal(14, 2),
    forecasted_usd    Decimal(14, 2),
    burn_rate_pct     Float32,
    days_remaining    UInt16,
    status            LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, budget_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS cost_allocation_events
(
    tenant_id       UUID,
    ts              DateTime64(3, 'UTC'),
    source_id       UUID,
    target_id       UUID,
    allocation_type LowCardinality(String),
    amount_usd      Decimal(12, 6),
    rule_id         UUID,
    notes           String DEFAULT ''
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, source_id)
TTL ts + INTERVAL 7 YEAR DELETE;

-- ============================================================
-- MÓDULO: SLO / RELIABILITY
-- ============================================================

CREATE TABLE IF NOT EXISTS error_budget_snapshots
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
    status             LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, slo_id, ts)
TTL ts + INTERVAL 2 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS sli_measurements
(
    tenant_id    UUID,
    slo_id       UUID,
    sli_type     LowCardinality(String),
    ts           DateTime64(3, 'UTC'),
    value        Float64,
    good_events  UInt64,
    bad_events   UInt64,
    total_events UInt64
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, slo_id, sli_type, ts)
TTL ts + INTERVAL 2 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS slo_compliance_daily
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

CREATE TABLE IF NOT EXISTS slo_compliance_weekly
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

CREATE TABLE IF NOT EXISTS slo_compliance_monthly
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

-- ============================================================
-- MÓDULO: ANALYTICS / PRODUCT
-- ============================================================

CREATE TABLE IF NOT EXISTS analytics_events
(
    tenant_id   UUID,
    event_id    UUID,
    ts          DateTime64(3, 'UTC'),
    user_id     Nullable(UUID),
    session_id  UUID,
    event_type  LowCardinality(String),
    entity_type LowCardinality(String),
    entity_id   Nullable(UUID),
    properties  String DEFAULT '{}',
    ip_hash     String,
    user_agent  String,
    country     LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, event_type)
TTL ts + INTERVAL 2 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS dashboard_usage_events
(
    tenant_id       UUID,
    user_id         UUID,
    dashboard_id    UUID,
    ts              DateTime64(3, 'UTC'),
    action          LowCardinality(String),
    duration_sec    Nullable(UInt32),
    filters_json    String DEFAULT '{}'
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, dashboard_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS productivity_snapshots
(
    tenant_id          UUID,
    team_id            UUID,
    ts                 DateTime64(3, 'UTC'),
    period_type        LowCardinality(String),
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

-- ============================================================
-- MÓDULO: SECURITY
-- ============================================================

CREATE TABLE IF NOT EXISTS security_events
(
    tenant_id     UUID,
    event_id      UUID,
    ts            DateTime64(3, 'UTC'),
    event_type    LowCardinality(String),
    severity      LowCardinality(String),
    actor_id      Nullable(UUID),
    actor_type    LowCardinality(String),
    resource_type LowCardinality(String),
    resource_id   Nullable(UUID),
    action        String,
    outcome       LowCardinality(String),
    ip_address    String,
    country       LowCardinality(String),
    details       String DEFAULT '{}'
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, event_type, severity)
TTL ts + INTERVAL 7 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS threat_signals
(
    tenant_id     UUID,
    ts            DateTime64(3, 'UTC'),
    signal_type   LowCardinality(String),
    source_ip     String,
    actor_id      Nullable(UUID),
    risk_score    Float32,
    indicators    String DEFAULT '[]',
    correlated_to Nullable(UUID)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, ts, signal_type)
TTL ts + INTERVAL 2 YEAR DELETE;

-- ============================================================
-- MÓDULO: DEVELOPER PRODUCTIVITY
-- ============================================================

CREATE TABLE IF NOT EXISTS agent_query_records
(
    tenant_id       UUID,
    user_id         UUID,
    agent_id        UUID,
    ts              DateTime64(3, 'UTC'),
    query_type      LowCardinality(String),
    response_ms     UInt32,
    tokens_used     UInt32,
    satisfied       Nullable(UInt8),
    ide_type        LowCardinality(String),
    language        LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, agent_id, ts)
TTL ts + INTERVAL 1 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS code_review_cycles
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

-- -------

CREATE TABLE IF NOT EXISTS deployment_records
(
    tenant_id        UUID,
    service_id       UUID,
    ts               DateTime64(3, 'UTC'),
    environment      LowCardinality(String),
    version          String,
    strategy         LowCardinality(String),
    duration_sec     UInt32,
    success          UInt8,
    triggered_by     LowCardinality(String)
)
ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(ts))
ORDER BY (tenant_id, service_id, ts)
TTL ts + INTERVAL 3 YEAR DELETE;

-- -------

CREATE TABLE IF NOT EXISTS pipeline_run_records
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

-- ============================================================
-- VIEWS MATERIALIZADAS — Agregações pré-calculadas
-- ============================================================

-- Token usage diário por agente
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_token_usage_daily
ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(day))
ORDER BY (tenant_id, agent_id, model_provider, day)
AS SELECT
    tenant_id,
    agent_id,
    model_provider,
    toDate(ts)        AS day,
    sum(total_tokens) AS total_tokens,
    sum(cost_usd)     AS total_cost_usd,
    count()           AS request_count
FROM token_usage_ledger
GROUP BY tenant_id, agent_id, model_provider, day;

-- -------

-- Cost diário por categoria
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_cost_daily
ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(day))
ORDER BY (tenant_id, cost_center_id, category, day)
AS SELECT
    tenant_id,
    cost_center_id,
    category,
    toDate(ts)           AS day,
    sum(total_cost_usd)  AS total_cost_usd,
    count()              AS record_count
FROM cost_records
GROUP BY tenant_id, cost_center_id, category, day;

-- -------

-- SLI compliance horária
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_sli_hourly
ENGINE = AggregatingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(hour))
ORDER BY (tenant_id, slo_id, sli_type, hour)
AS SELECT
    tenant_id,
    slo_id,
    sli_type,
    toStartOfHour(ts)           AS hour,
    avgState(value)             AS avg_value,
    quantileState(0.95)(value)  AS p95_value,
    sumState(good_events)       AS total_good,
    sumState(total_events)      AS total_events
FROM sli_measurements
GROUP BY tenant_id, slo_id, sli_type, hour;

-- -------

-- Security events por severidade (diário)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_security_events_daily
ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(day))
ORDER BY (tenant_id, event_type, severity, day)
AS SELECT
    tenant_id,
    event_type,
    severity,
    toDate(ts) AS day,
    count()    AS event_count
FROM security_events
GROUP BY tenant_id, event_type, severity, day;
```

---

## Guia de engines por padrão de uso

| Engine | Quando usar | Exemplo |
|--------|-------------|---------|
| `MergeTree` | Append-only simples, queries ad-hoc | service_metrics_snapshots |
| `SummingMergeTree` | Pre-agregação de somas (evita full scans) | token_usage_ledger, cost_records |
| `AggregatingMergeTree` | Pre-agregação de funções complexas (avg, quantile) | mv_sli_hourly |
| `ReplacingMergeTree` | Deduplicação por chave (snapshot mais recente) | Não usado — domínio não tem updates |

## Convenções de nomenclatura

| Elemento | Convenção | Exemplo |
|----------|-----------|---------|
| Database | `nextraceone_analytics` | — |
| Tabela principal | `snake_case` plural | `cost_records` |
| View materializada | `mv_` + tabela + `_granularidade` | `mv_cost_daily` |
| Partição temporal | `toYYYYMM(ts_column)` | `toYYYYMM(ts)` |
| Ordem primária | `(tenant_id, entity_id, ts)` | — |
| TTL mínimo | 1 ano | alert_firing_records |
| TTL financeiro | 5-7 anos | cost_records, cost_allocation_events |
| TTL auditoria | 7 anos | security_events |
