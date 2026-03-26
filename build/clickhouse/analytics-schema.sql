-- ═══════════════════════════════════════════════════════════════════════════════
-- NexTraceOne — ClickHouse Schema for Domain Analytics
--
-- Este schema cria as tabelas analíticas de domínio para os módulos NexTraceOne.
--
-- SEPARAÇÃO DE RESPONSABILIDADES:
--   nextraceone_obs      → dados de observabilidade OpenTelemetry (logs, traces, métricas)
--   nextraceone_analytics → dados analíticos de domínio (eventos de produto, métricas de
--                           runtime, execuções de integrações, analytics de governança)
--
-- PRINCÍPIOS DESTE SCHEMA:
--   - Todas as tabelas são append-only (MergeTree / SummingMergeTree / AggregatingMergeTree)
--   - Não existe estado transacional aqui — tudo isso fica no PostgreSQL
--   - Correlação com PostgreSQL é feita via IDs (sem FK — ClickHouse não suporta)
--   - tenant_id é obrigatório em todas as tabelas (isolamento multi-tenant)
--   - TTL definido por tabela conforme política de retenção aprovada
--   - Particionamento por (tenant_id, toYYYYMM(timestamp)) em tabelas de alto volume
--
-- MÓDULOS COBERTOS (E16 — IMPLEMENT_CLICKHOUSE_NOW):
--   pan_*   → Product Analytics (REQUIRED)
--   ops_*   → Operational Intelligence (RECOMMENDED)
--   int_*   → Integrations (RECOMMENDED)
--   gov_*   → Governance Analytics (RECOMMENDED)
--   chg_*   → Change Intelligence (P5.2 — REQUIRED)
--
-- MÓDULOS PREPARADOS MAS NÃO ATIVOS (E16 — PREPARE_ONLY):
--   aik_*   → AI & Knowledge (comentado — ativar quando volume justificar)
--
-- VERSÃO: E16 — 2026-03-25
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS nextraceone_analytics;

-- ════════════════════════════════════════════════════════════════════════════════
-- PRODUCT ANALYTICS (pan_*)
-- Nível ClickHouse: REQUIRED
-- Módulo: Product Analytics (13)
-- Fonte: eventos de uso de produto (page views, actions, funnels, sessões)
-- ════════════════════════════════════════════════════════════════════════════════

-- ── pan_events — Tabela principal de eventos de produto ──────────────────────
-- Store permanente de todos os eventos de uso da plataforma.
-- Alimentado via: PostgreSQL buffer (pan_analytics_events) → Outbox → ClickHouse writer
CREATE TABLE IF NOT EXISTS nextraceone_analytics.pan_events
(
    id               UUID,
    tenant_id        UUID,
    user_id          UUID,
    persona          LowCardinality(String),
    module           LowCardinality(String),
    event_type       UInt8,
    feature          String                    DEFAULT '',
    entity_type      String                    DEFAULT '',
    outcome          String                    DEFAULT '',
    route            String                    DEFAULT '',
    team_id          Nullable(UUID),
    domain_id        Nullable(UUID),
    session_id       String                    DEFAULT '',
    client_type      LowCardinality(String)    DEFAULT '',
    metadata_json    String                    DEFAULT '',
    occurred_at      DateTime64(3, 'UTC'),
    environment_id   Nullable(UUID),
    duration_ms      Nullable(UInt32),
    parent_event_id  Nullable(UUID),
    source           LowCardinality(String)    DEFAULT 'Frontend'
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(occurred_at))
ORDER BY (tenant_id, occurred_at, module, event_type)
TTL occurred_at + INTERVAL 2 YEAR
SETTINGS index_granularity = 8192;

-- ── pan_daily_module_stats — Agregação diária por módulo ─────────────────────
-- Materialized view que agrega eventos por tenant + data + módulo.
-- Alimenta dashboards de adoção de módulos sem varrer pan_events.
CREATE MATERIALIZED VIEW IF NOT EXISTS nextraceone_analytics.pan_daily_module_stats
ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(date))
ORDER BY (tenant_id, date, module)
TTL date + INTERVAL 1 YEAR
AS SELECT
    tenant_id,
    toDate(occurred_at)                                  AS date,
    module,
    count()                                              AS total_events,
    uniqExact(user_id)                                   AS unique_users,
    uniqExact(session_id)                                AS unique_sessions,
    countIf(event_type IN (4, 21, 22))                   AS friction_events,
    countIf(outcome = 'Success')                         AS success_events,
    countIf(outcome = 'Error')                           AS error_events
FROM nextraceone_analytics.pan_events
GROUP BY tenant_id, date, module;

-- ── pan_daily_persona_stats — Agregação diária por persona ───────────────────
-- Materialized view que agrega eventos por tenant + data + persona.
-- Alimenta dashboards de uso por persona.
CREATE MATERIALIZED VIEW IF NOT EXISTS nextraceone_analytics.pan_daily_persona_stats
ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(date))
ORDER BY (tenant_id, date, persona)
TTL date + INTERVAL 1 YEAR
AS SELECT
    tenant_id,
    toDate(occurred_at)                                  AS date,
    persona,
    count()                                              AS total_events,
    uniqExact(user_id)                                   AS unique_users,
    uniqExact(module)                                    AS modules_used,
    avg(coalesce(duration_ms, 0))                        AS avg_duration_ms
FROM nextraceone_analytics.pan_events
GROUP BY tenant_id, date, persona;

-- ── pan_daily_friction_stats — Agregação diária de indicadores de fricção ────
-- Materialized view que agrega eventos de fricção (zero results, abandonos, erros).
-- Alimenta indicadores de UX e friction analysis.
CREATE MATERIALIZED VIEW IF NOT EXISTS nextraceone_analytics.pan_daily_friction_stats
ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(date))
ORDER BY (tenant_id, date, module)
TTL date + INTERVAL 1 YEAR
AS SELECT
    tenant_id,
    toDate(occurred_at)                                  AS date,
    module,
    feature,
    countIf(event_type = 4)                              AS zero_result_searches,
    countIf(event_type = 21)                             AS journey_abandonments,
    countIf(event_type = 22)                             AS empty_state_encounters,
    countIf(outcome = 'Error')                           AS error_outcomes
FROM nextraceone_analytics.pan_events
GROUP BY tenant_id, date, module, feature;

-- ── pan_session_summaries — Resumos de sessão para funnels ───────────────────
-- Materialized view AggregatingMergeTree para resumos de sessão.
-- Alimenta funnel analysis e cohort retention dashboards.
CREATE MATERIALIZED VIEW IF NOT EXISTS nextraceone_analytics.pan_session_summaries
ENGINE = AggregatingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(session_date))
ORDER BY (tenant_id, session_date, session_id)
TTL session_date + INTERVAL 90 DAY
AS SELECT
    tenant_id,
    toDate(occurred_at)                                  AS session_date,
    session_id,
    anyState(user_id)                                    AS user_id,
    anyState(persona)                                    AS persona,
    countState()                                         AS event_count,
    uniqExactState(module)                               AS modules_visited,
    minState(occurred_at)                                AS session_start,
    maxState(occurred_at)                                AS session_end
FROM nextraceone_analytics.pan_events
WHERE session_id != ''
GROUP BY tenant_id, session_date, session_id;

-- ════════════════════════════════════════════════════════════════════════════════
-- OPERATIONAL INTELLIGENCE (ops_*)
-- Nível ClickHouse: RECOMMENDED
-- Módulo: Operational Intelligence (06)
-- Fonte: métricas de runtime, snapshots de custo, tendências de incidentes
-- ════════════════════════════════════════════════════════════════════════════════

-- ── ops_runtime_metrics — Métricas de runtime por serviço/ambiente ────────────
-- Time-series de métricas de latência, throughput, error rate e recursos.
-- Alimentado via: projeção de ops_runtime_snapshots → Outbox → ClickHouse writer
CREATE TABLE IF NOT EXISTS nextraceone_analytics.ops_runtime_metrics
(
    id                    UUID,
    tenant_id             UUID,
    service_name          LowCardinality(String),
    service_id            Nullable(UUID),
    environment           LowCardinality(String),
    environment_id        Nullable(UUID),
    source                LowCardinality(String)    DEFAULT '',
    avg_latency_ms        Decimal64(3),
    p99_latency_ms        Decimal64(3),
    error_rate            Decimal64(3),
    requests_per_second   Decimal64(3),
    cpu_usage_percent     Decimal64(3),
    memory_usage_mb       Decimal64(3),
    active_instances      UInt32                    DEFAULT 0,
    health_status         LowCardinality(String)    DEFAULT '',
    captured_at           DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(captured_at))
ORDER BY (tenant_id, service_name, environment, captured_at)
TTL captured_at + INTERVAL 90 DAY
SETTINGS index_granularity = 8192;

-- ── ops_cost_entries — Entradas de custo operacional por serviço ─────────────
-- Séries temporais de custo por serviço/ambiente/período.
-- Alimentado via: projeção de ops_cost_snapshots + ops_cost_records → ClickHouse writer
CREATE TABLE IF NOT EXISTS nextraceone_analytics.ops_cost_entries
(
    id                    UUID,
    tenant_id             UUID,
    service_name          LowCardinality(String),
    service_id            Nullable(UUID),
    environment           LowCardinality(String),
    environment_id        Nullable(UUID),
    currency              LowCardinality(String)    DEFAULT 'USD',
    period                LowCardinality(String)    DEFAULT '',
    source                LowCardinality(String)    DEFAULT '',
    total_cost            Decimal64(4),
    cpu_cost_share        Decimal64(4)              DEFAULT 0,
    memory_cost_share     Decimal64(4)              DEFAULT 0,
    network_cost_share    Decimal64(4)              DEFAULT 0,
    storage_cost_share    Decimal64(4)              DEFAULT 0,
    captured_at           DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(captured_at))
ORDER BY (tenant_id, service_name, environment, captured_at)
TTL captured_at + INTERVAL 1 YEAR
SETTINGS index_granularity = 8192;

-- ── ops_incident_trends — Tendências de incidentes (stream de eventos) ───────
-- Stream de eventos do ciclo de vida de incidentes para análise de tendências.
-- Nota: estado activo dos incidentes fica no PostgreSQL (ops_incidents).
-- Este é um stream de projecção: Created, Correlated, Resolved, Reopened.
CREATE TABLE IF NOT EXISTS nextraceone_analytics.ops_incident_trends
(
    event_id              UUID,
    incident_id           UUID,
    tenant_id             UUID,
    service_name          LowCardinality(String)    DEFAULT '',
    service_id            Nullable(UUID),
    environment           LowCardinality(String)    DEFAULT '',
    environment_id        Nullable(UUID),
    severity              LowCardinality(String),
    incident_type         LowCardinality(String)    DEFAULT '',
    lifecycle_event       LowCardinality(String),
    change_correlated     Bool                      DEFAULT false,
    mttr_minutes          Nullable(UInt32),
    occurred_at           DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(occurred_at))
ORDER BY (tenant_id, occurred_at, severity)
TTL occurred_at + INTERVAL 1 YEAR
SETTINGS index_granularity = 8192;

-- ════════════════════════════════════════════════════════════════════════════════
-- INTEGRATIONS (int_*)
-- Nível ClickHouse: RECOMMENDED
-- Módulo: Integrations (12)
-- Fonte: logs de execução de conectores, histórico de health
-- ════════════════════════════════════════════════════════════════════════════════

-- ── int_execution_logs — Logs históricos de execuções de conectores ───────────
-- Histórico append-only de todas as execuções de ingestão.
-- Estado activo (Running, recente) fica em int_ingestion_executions no PostgreSQL.
CREATE TABLE IF NOT EXISTS nextraceone_analytics.int_execution_logs
(
    id                UUID,
    tenant_id         UUID,
    connector_id      UUID,
    connector_name    String                    DEFAULT '',
    connector_type    LowCardinality(String)    DEFAULT '',
    provider          LowCardinality(String)    DEFAULT '',
    source_id         Nullable(UUID),
    data_domain       LowCardinality(String)    DEFAULT '',
    correlation_id    Nullable(String),
    started_at        DateTime64(3, 'UTC'),
    completed_at      Nullable(DateTime64(3, 'UTC')),
    duration_ms       Nullable(Int64),
    result            LowCardinality(String),
    items_processed   Int32                     DEFAULT 0,
    items_succeeded   Int32                     DEFAULT 0,
    items_failed      Int32                     DEFAULT 0,
    error_code        Nullable(String),
    retry_attempt     Int32                     DEFAULT 0,
    created_at        DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(started_at))
ORDER BY (tenant_id, connector_id, started_at)
TTL created_at + INTERVAL 1 YEAR
SETTINGS index_granularity = 8192;

-- ── int_health_history — Histórico de transições de health de conectores ──────
-- Registo de cada transição de estado de health de um conector.
-- Permite análise de MTTF, MTTR e tendências de estabilidade.
CREATE TABLE IF NOT EXISTS nextraceone_analytics.int_health_history
(
    tenant_id             UUID,
    connector_id          UUID,
    connector_name        String                DEFAULT '',
    health                LowCardinality(String),
    previous_health       LowCardinality(String) DEFAULT '',
    freshness_lag_minutes Nullable(Int32),
    changed_at            DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(changed_at))
ORDER BY (tenant_id, connector_id, changed_at)
TTL changed_at + INTERVAL 1 YEAR
SETTINGS index_granularity = 8192;

-- ════════════════════════════════════════════════════════════════════════════════
-- GOVERNANCE ANALYTICS (gov_*)
-- Nível ClickHouse: RECOMMENDED
-- Módulo: Governance (08)
-- Fonte: scores de compliance, agregações FinOps, histórico de risco
-- ════════════════════════════════════════════════════════════════════════════════

-- ── gov_compliance_trends — Tendências de compliance score ───────────────────
-- Séries temporais de compliance scores por tenant/serviço/política.
-- Alimenta dashboards de compliance e relatórios de auditoria.
CREATE TABLE IF NOT EXISTS nextraceone_analytics.gov_compliance_trends
(
    tenant_id           UUID,
    service_id          Nullable(UUID),
    service_name        LowCardinality(String)    DEFAULT '',
    policy_id           Nullable(UUID),
    policy_name         LowCardinality(String)    DEFAULT '',
    environment         LowCardinality(String)    DEFAULT '',
    compliance_score    Decimal64(2),
    status              LowCardinality(String)    DEFAULT '',
    violations_count    UInt32                    DEFAULT 0,
    captured_at         DateTime64(3, 'UTC')
) ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(captured_at))
ORDER BY (tenant_id, service_name, policy_name, captured_at)
TTL captured_at + INTERVAL 2 YEAR
SETTINGS index_granularity = 8192;

-- ── gov_finops_aggregates — Agregações FinOps por domínio/equipa ─────────────
-- Agregações de custo por domínio, equipa, serviço e período.
-- Alimenta dashboards FinOps contextuais (custo por serviço, por equipa).
CREATE TABLE IF NOT EXISTS nextraceone_analytics.gov_finops_aggregates
(
    tenant_id           UUID,
    team_id             Nullable(UUID),
    team_name           LowCardinality(String)    DEFAULT '',
    domain_name         LowCardinality(String)    DEFAULT '',
    service_name        LowCardinality(String)    DEFAULT '',
    service_id          Nullable(UUID),
    environment         LowCardinality(String)    DEFAULT '',
    currency            LowCardinality(String)    DEFAULT 'USD',
    period_label        LowCardinality(String)    DEFAULT '',
    total_cost          Decimal64(4),
    compute_cost        Decimal64(4)              DEFAULT 0,
    storage_cost        Decimal64(4)              DEFAULT 0,
    network_cost        Decimal64(4)              DEFAULT 0,
    anomaly_detected    Bool                      DEFAULT false,
    captured_at         DateTime64(3, 'UTC')
) ENGINE = SummingMergeTree()
PARTITION BY (tenant_id, toYYYYMM(captured_at))
ORDER BY (tenant_id, team_name, service_name, captured_at)
TTL captured_at + INTERVAL 2 YEAR
SETTINGS index_granularity = 8192;

-- ════════════════════════════════════════════════════════════════════════════════
-- AI & KNOWLEDGE (aik_*) — PREPARE_ONLY (E16)
-- Nível ClickHouse: OPTIONAL_LATER (promovido de RECOMMENDED no E16)
-- Módulo: AI & Knowledge (07)
-- Estado: Schema definido mas tabelas comentadas — ativar no E17 ou quando
--         o volume de token usage justificar (> 10K registos/dia/tenant)
-- ════════════════════════════════════════════════════════════════════════════════

-- NOTA: Estas tabelas estão comentadas intencionalmente.
-- O módulo AI & Knowledge funciona inteiramente em PostgreSQL no MVP1.
-- O schema está definido aqui para referência arquitetural.
-- Para ativar: descomentar e executar no ClickHouse.

-- CREATE TABLE IF NOT EXISTS nextraceone_analytics.aik_token_usage
-- (
--     id               UUID,
--     tenant_id        UUID,
--     user_id          UUID,
--     model_id         UUID,
--     model_name       LowCardinality(String)    DEFAULT '',
--     provider_id      UUID,
--     provider_name    LowCardinality(String)    DEFAULT '',
--     agent_id         Nullable(UUID),
--     conversation_id  Nullable(UUID),
--     routing_path     LowCardinality(String)    DEFAULT '',
--     tokens_input     UInt32                    DEFAULT 0,
--     tokens_output    UInt32                    DEFAULT 0,
--     cost_estimate    Decimal64(6)              DEFAULT 0,
--     response_time_ms UInt32                    DEFAULT 0,
--     usage_result     LowCardinality(String)    DEFAULT '',
--     environment_id   Nullable(UUID),
--     occurred_at      DateTime64(3, 'UTC')
-- ) ENGINE = MergeTree()
-- PARTITION BY (tenant_id, toYYYYMM(occurred_at))
-- ORDER BY (tenant_id, model_id, occurred_at)
-- TTL occurred_at + INTERVAL 1 YEAR
-- SETTINGS index_granularity = 8192;

-- CREATE TABLE IF NOT EXISTS nextraceone_analytics.aik_model_performance
-- (
--     tenant_id          UUID,
--     model_id           UUID,
--     model_name         LowCardinality(String)    DEFAULT '',
--     provider_id        UUID,
--     avg_latency_ms     Decimal64(3),
--     p99_latency_ms     Decimal64(3),
--     success_rate       Decimal64(3),
--     total_requests     UInt64,
--     total_tokens       UInt64,
--     captured_at        DateTime64(3, 'UTC')
-- ) ENGINE = MergeTree()
-- PARTITION BY (tenant_id, toYYYYMM(captured_at))
-- ORDER BY (tenant_id, model_id, captured_at)
-- TTL captured_at + INTERVAL 90 DAY
-- SETTINGS index_granularity = 8192;

-- ════════════════════════════════════════════════════════════════════════════════
-- CHANGE INTELLIGENCE ANALYTICS (chg_*)
-- Nível ClickHouse: REQUIRED (P5.2)
-- Módulo: Change Governance (10)
-- Fonte: correlação automática entre traces OTel e releases do módulo Change Governance
-- ════════════════════════════════════════════════════════════════════════════════

-- ── chg_trace_release_mapping — Mapeamento analítico trace → release ─────────
-- Registo append-only de correlações entre traces distribuídos e releases.
-- Permite responder: "quais traces pertencem a esta release?" e inverso.
--
-- Alimentado por: NotifyDeployment.Handler (via ITraceCorrelationWriter)
--                 e por qualquer pipeline que correlacione traces a deploys.
--
-- Correlação com PostgreSQL:
--   release_id      → chg_releases.Id
--   tenant_id       → iam_tenants.Id
--   service_id      → cat_service_assets.Id (opcional)
--   environment_id  → env_environments.Id (opcional)
--
-- Correlação com nextraceone_obs:
--   trace_id        → otel_traces.TraceId (sem FK — ClickHouse não suporta)
CREATE TABLE IF NOT EXISTS nextraceone_analytics.chg_trace_release_mapping
(
    id                  UUID,
    tenant_id           UUID,
    release_id          UUID,
    trace_id            String,
    service_name        LowCardinality(String),
    service_id          Nullable(UUID),
    environment         LowCardinality(String),
    environment_id      Nullable(UUID),
    correlation_source  LowCardinality(String)   DEFAULT 'deployment_event',
    trace_started_at    Nullable(DateTime64(3, 'UTC')),
    trace_ended_at      Nullable(DateTime64(3, 'UTC')),
    correlated_at       DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(correlated_at))
ORDER BY (tenant_id, release_id, correlated_at, trace_id)
TTL correlated_at + INTERVAL 1 YEAR
SETTINGS index_granularity = 8192;
