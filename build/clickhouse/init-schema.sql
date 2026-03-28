-- ═══════════════════════════════════════════════════════════════════════════════
-- NexTraceOne — ClickHouse Schema for Observability Data
--
-- Este schema cria as tabelas dedicadas para armazenamento analítico de
-- logs, traces e métricas crus no ClickHouse.
--
-- O ClickHouse é o provider padrão de observabilidade do NexTraceOne.
-- PostgreSQL permanece exclusivo para dados transacionais e de domínio.
--
-- IMPORTANTE:
-- - O ClickHouse é stateful — requer volume persistente
-- - Não usar filesystem efêmero do container
-- - Tabelas usam MergeTree engine com TTL para retenção automática
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS nextraceone_obs;

-- ── Logs ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_logs
(
    Timestamp           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate       Date DEFAULT toDate(Timestamp),
    TraceId             String CODEC(ZSTD(1)),
    SpanId              String CODEC(ZSTD(1)),
    TraceFlags          UInt32,
    SeverityText        LowCardinality(String) CODEC(ZSTD(1)),
    SeverityNumber      Int32,
    ServiceName         LowCardinality(String) CODEC(ZSTD(1)),
    Body                String CODEC(ZSTD(1)),
    ResourceSchemaUrl   String CODEC(ZSTD(1)),
    ResourceAttributes  Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeSchemaUrl      String CODEC(ZSTD(1)),
    ScopeName           String CODEC(ZSTD(1)),
    ScopeVersion        String CODEC(ZSTD(1)),
    ScopeAttributes     Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    LogAttributes       Map(LowCardinality(String), String) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(TimestampDate)
ORDER BY (ServiceName, SeverityText, Timestamp)
TTL TimestampDate + INTERVAL 30 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

-- ── Traces (Spans) ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_traces
(
    Timestamp           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate       Date DEFAULT toDate(Timestamp),
    TraceId             String CODEC(ZSTD(1)),
    SpanId              String CODEC(ZSTD(1)),
    ParentSpanId        String CODEC(ZSTD(1)),
    TraceState          String CODEC(ZSTD(1)),
    SpanName            LowCardinality(String) CODEC(ZSTD(1)),
    SpanKind            LowCardinality(String) CODEC(ZSTD(1)),
    ServiceName         LowCardinality(String) CODEC(ZSTD(1)),
    ResourceAttributes  Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeSchemaUrl      String CODEC(ZSTD(1)),
    ScopeName           String CODEC(ZSTD(1)),
    ScopeVersion        String CODEC(ZSTD(1)),
    ScopeAttributes     Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    SpanAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    Duration            Int64 CODEC(ZSTD(1)),
    StatusCode          LowCardinality(String) CODEC(ZSTD(1)),
    StatusMessage       String CODEC(ZSTD(1)),
    Events              Nested (
        Timestamp DateTime64(9),
        Name LowCardinality(String),
        Attributes Map(LowCardinality(String), String)
    ) CODEC(ZSTD(1)),
    Links               Nested (
        TraceId String,
        SpanId String,
        TraceState String,
        Attributes Map(LowCardinality(String), String)
    ) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(TimestampDate)
ORDER BY (ServiceName, SpanName, Timestamp)
TTL TimestampDate + INTERVAL 30 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

-- ── Métricas ─────────────────────────────────────────────────────────────────
-- O clickhouseexporter (otel-collector-contrib >= v0.100) usa tabelas separadas
-- por tipo de métrica (gauge, sum, histogram, exponential_histogram, summary).
-- O prefixo é definido por metrics_table_name no otel-collector.yaml ("otel_metrics").
-- Estas tabelas são também criadas automaticamente pelo exporter (create_schema: true),
-- mas definidas aqui para explicitação e controlo de TTL.

CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_metrics_gauge
(
    ResourceAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ResourceSchemaUrl       String CODEC(ZSTD(1)),
    ScopeName               String CODEC(ZSTD(1)),
    ScopeVersion            String CODEC(ZSTD(1)),
    ScopeAttributes         Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeDroppedAttrCount   UInt32 CODEC(ZSTD(1)),
    ScopeSchemaUrl          String CODEC(ZSTD(1)),
    ServiceName             LowCardinality(String) CODEC(ZSTD(1)),
    MetricName              String CODEC(ZSTD(1)),
    MetricDescription       String CODEC(ZSTD(1)),
    MetricUnit              String CODEC(ZSTD(1)),
    Attributes              Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    StartTimeUnix           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimeUnix                DateTime64(9) CODEC(Delta, ZSTD(1)),
    Value                   Float64 CODEC(ZSTD(1)),
    Flags                   UInt32 CODEC(ZSTD(1)),
    Exemplars Nested (
        FilteredAttributes  Map(LowCardinality(String), String),
        TimeUnix            DateTime64(9),
        Value               Float64,
        SpanId              String,
        TraceId             String
    ) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toDate(TimeUnix)
ORDER BY (ServiceName, MetricName, Attributes, toUnixTimestamp64Nano(TimeUnix))
TTL toDateTime(TimeUnix) + INTERVAL 90 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_metrics_sum
(
    ResourceAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ResourceSchemaUrl       String CODEC(ZSTD(1)),
    ScopeName               String CODEC(ZSTD(1)),
    ScopeVersion            String CODEC(ZSTD(1)),
    ScopeAttributes         Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeDroppedAttrCount   UInt32 CODEC(ZSTD(1)),
    ScopeSchemaUrl          String CODEC(ZSTD(1)),
    ServiceName             LowCardinality(String) CODEC(ZSTD(1)),
    MetricName              String CODEC(ZSTD(1)),
    MetricDescription       String CODEC(ZSTD(1)),
    MetricUnit              String CODEC(ZSTD(1)),
    Attributes              Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    StartTimeUnix           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimeUnix                DateTime64(9) CODEC(Delta, ZSTD(1)),
    Value                   Float64 CODEC(ZSTD(1)),
    Flags                   UInt32 CODEC(ZSTD(1)),
    Exemplars Nested (
        FilteredAttributes  Map(LowCardinality(String), String),
        TimeUnix            DateTime64(9),
        Value               Float64,
        SpanId              String,
        TraceId             String
    ) CODEC(ZSTD(1)),
    AggregationTemporality  Int32 CODEC(ZSTD(1)),
    IsMonotonic             Bool CODEC(Delta, ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toDate(TimeUnix)
ORDER BY (ServiceName, MetricName, Attributes, toUnixTimestamp64Nano(TimeUnix))
TTL toDateTime(TimeUnix) + INTERVAL 90 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_metrics_histogram
(
    ResourceAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ResourceSchemaUrl       String CODEC(ZSTD(1)),
    ScopeName               String CODEC(ZSTD(1)),
    ScopeVersion            String CODEC(ZSTD(1)),
    ScopeAttributes         Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeDroppedAttrCount   UInt32 CODEC(ZSTD(1)),
    ScopeSchemaUrl          String CODEC(ZSTD(1)),
    ServiceName             LowCardinality(String) CODEC(ZSTD(1)),
    MetricName              String CODEC(ZSTD(1)),
    MetricDescription       String CODEC(ZSTD(1)),
    MetricUnit              String CODEC(ZSTD(1)),
    Attributes              Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    StartTimeUnix           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimeUnix                DateTime64(9) CODEC(Delta, ZSTD(1)),
    Count                   UInt64 CODEC(Delta, ZSTD(1)),
    Sum                     Float64 CODEC(ZSTD(1)),
    BucketCounts            Array(UInt64) CODEC(ZSTD(1)),
    ExplicitBounds          Array(Float64) CODEC(ZSTD(1)),
    Exemplars Nested (
        FilteredAttributes  Map(LowCardinality(String), String),
        TimeUnix            DateTime64(9),
        Value               Float64,
        SpanId              String,
        TraceId             String
    ) CODEC(ZSTD(1)),
    Flags                   UInt32 CODEC(ZSTD(1)),
    Min                     Float64 CODEC(ZSTD(1)),
    Max                     Float64 CODEC(ZSTD(1)),
    AggregationTemporality  Int32 CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toDate(TimeUnix)
ORDER BY (ServiceName, MetricName, Attributes, toUnixTimestamp64Nano(TimeUnix))
TTL toDateTime(TimeUnix) + INTERVAL 90 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_metrics_exponential_histogram
(
    ResourceAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ResourceSchemaUrl       String CODEC(ZSTD(1)),
    ScopeName               String CODEC(ZSTD(1)),
    ScopeVersion            String CODEC(ZSTD(1)),
    ScopeAttributes         Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeDroppedAttrCount   UInt32 CODEC(ZSTD(1)),
    ScopeSchemaUrl          String CODEC(ZSTD(1)),
    ServiceName             LowCardinality(String) CODEC(ZSTD(1)),
    MetricName              String CODEC(ZSTD(1)),
    MetricDescription       String CODEC(ZSTD(1)),
    MetricUnit              String CODEC(ZSTD(1)),
    Attributes              Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    StartTimeUnix           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimeUnix                DateTime64(9) CODEC(Delta, ZSTD(1)),
    Count                   UInt64 CODEC(Delta, ZSTD(1)),
    Sum                     Float64 CODEC(ZSTD(1)),
    Scale                   Int32 CODEC(ZSTD(1)),
    ZeroCount               UInt64 CODEC(ZSTD(1)),
    PositiveOffset          Int32 CODEC(ZSTD(1)),
    PositiveBucketCounts    Array(UInt64) CODEC(ZSTD(1)),
    NegativeOffset          Int32 CODEC(ZSTD(1)),
    NegativeBucketCounts    Array(UInt64) CODEC(ZSTD(1)),
    Exemplars Nested (
        FilteredAttributes  Map(LowCardinality(String), String),
        TimeUnix            DateTime64(9),
        Value               Float64,
        SpanId              String,
        TraceId             String
    ) CODEC(ZSTD(1)),
    Flags                   UInt32 CODEC(ZSTD(1)),
    Min                     Float64 CODEC(ZSTD(1)),
    Max                     Float64 CODEC(ZSTD(1)),
    AggregationTemporality  Int32 CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toDate(TimeUnix)
ORDER BY (ServiceName, MetricName, Attributes, toUnixTimestamp64Nano(TimeUnix))
TTL toDateTime(TimeUnix) + INTERVAL 90 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_metrics_summary
(
    ResourceAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ResourceSchemaUrl       String CODEC(ZSTD(1)),
    ScopeName               String CODEC(ZSTD(1)),
    ScopeVersion            String CODEC(ZSTD(1)),
    ScopeAttributes         Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeDroppedAttrCount   UInt32 CODEC(ZSTD(1)),
    ScopeSchemaUrl          String CODEC(ZSTD(1)),
    ServiceName             LowCardinality(String) CODEC(ZSTD(1)),
    MetricName              String CODEC(ZSTD(1)),
    MetricDescription       String CODEC(ZSTD(1)),
    MetricUnit              String CODEC(ZSTD(1)),
    Attributes              Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    StartTimeUnix           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimeUnix                DateTime64(9) CODEC(Delta, ZSTD(1)),
    Count                   UInt64 CODEC(Delta, ZSTD(1)),
    Sum                     Float64 CODEC(ZSTD(1)),
    ValueAtQuantiles Nested (
        Quantile            Float64,
        Value               Float64
    ) CODEC(ZSTD(1)),
    Flags                   UInt32 CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toDate(TimeUnix)
ORDER BY (ServiceName, MetricName, Attributes, toUnixTimestamp64Nano(TimeUnix))
TTL toDateTime(TimeUnix) + INTERVAL 90 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;
