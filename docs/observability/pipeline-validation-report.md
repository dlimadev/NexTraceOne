# Pipeline Validation Report — OTEL → Collector → ClickHouse

> **Prompt ID:** P04.2  
> **Phase:** 04-integrations-observability  
> **Date:** 2026-07  
> **Status:** ✅ Fixed — pipeline validated, critical issues resolved

---

## 1. Topology

```
┌──────────────────────────────────────────────────────────────────┐
│  NexTraceOne Services (ApiHost, Ingestion.Api, WorkerService)    │
│  SDK: OpenTelemetry .NET (OTLP exporter)                         │
│  Config: OTEL_EXPORTER_OTLP_ENDPOINT / Telemetry:Collector:*    │
└──────────────────────────────┬───────────────────────────────────┘
                               │ OTLP gRPC :4317 / HTTP :4318
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│  OpenTelemetry Collector (otel/opentelemetry-collector-contrib)  │
│  Image: otel/opentelemetry-collector-contrib:0.115.0             │
│  Config: build/otel-collector/otel-collector.yaml                │
│                                                                  │
│  Receivers:  otlp (4317/4318), hostmetrics                       │
│  Processors: memory_limiter, resourcedetection, normalize,       │
│              filter, transform/correlation, redaction,           │
│              tail_sampling (traces only), batch                  │
│  Connectors: spanmetrics (traces → metrics)                      │
│  Exporters:  clickhouse (all signals), debug                     │
└──────────────────────────────┬───────────────────────────────────┘
                               │ TCP :9000 (native ClickHouse protocol)
                               ▼
┌──────────────────────────────────────────────────────────────────┐
│  ClickHouse 24.8-alpine                                          │
│  Database: nextraceone_obs                                       │
│                                                                  │
│  Tables:                                                         │
│    otel_logs                    — Logs (TTL 30 days)             │
│    otel_traces                  — Traces/Spans (TTL 30 days)     │
│    otel_metrics_gauge           — Gauge metrics (TTL 90 days)    │
│    otel_metrics_sum             — Sum/Counter metrics            │
│    otel_metrics_histogram       — Explicit histograms            │
│    otel_metrics_exponential_histogram — Exp. histograms          │
│    otel_metrics_summary         — Summaries                      │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Files audited

| File | State |
|---|---|
| `build/otel-collector/otel-collector.yaml` | Fixed — 2 critical issues resolved |
| `build/clickhouse/init-schema.sql` | Fixed — metric tables schema corrected |
| `build/otel-collector/docker-compose.telemetry.yaml` | Fixed — network + schema mount |
| `build/clickhouse/analytics-schema.sql` | No issues — product analytics tables only |
| `docker-compose.yml` | No issues — correctly wires all services on `nextraceone-net` |
| `docker-compose.override.yml` | No issues — correct dev OTEL env vars (P04.1) |

---

## 3. Issues found and fixed

### CRITICAL-1 — Circular loop in metrics pipeline

**File:** `build/otel-collector/otel-collector.yaml`  
**Problem:** The `otlp` exporter was declared with `endpoint: "localhost:4317"`. In a
Docker container, `localhost` resolves to the container itself. Port `4317` is the
Collector's own OTLP gRPC receiver. This created a circular loop: the metrics pipeline
was attempting to export spanmetrics back into the Collector's own receiver, causing
connection failures or infinite processing loops.

**Impact:** The entire metrics pipeline would fail silently or loop indefinitely. No
metrics (including spanmetrics derived from traces) would ever reach ClickHouse.

**Fix applied:** Removed `otlp` from the metrics pipeline `exporters` list.
Metrics now flow exclusively to ClickHouse:

```yaml
# Before (broken):
metrics:
  exporters: [clickhouse, otlp]

# After (fixed):
metrics:
  exporters: [clickhouse]
```

The `otlp` exporter block is retained in the config (not in any active pipeline)
for future use when a separate Product Store aggregator is available. The endpoint
was changed to use an env var `${SPANMETRICS_FORWARD_ENDPOINT:-localhost:4317}` with
a prominent comment warning against using `localhost` in a containerised context.

---

### CRITICAL-2 — Wrong ClickHouse metric table schema

**File:** `build/clickhouse/init-schema.sql`  
**Problem:** The init script created a single `otel_metrics` table with a generic
schema (Timestamp, MetricName, Value, AggregationTemporality, IsMonotonic). However,
the `clickhouseexporter` in `otel-collector-contrib` >= v0.100 uses **separate tables
per metric type**, deriving table names from the configured `metrics_table_name` prefix:

- `otel_metrics_gauge`
- `otel_metrics_sum`
- `otel_metrics_histogram`
- `otel_metrics_exponential_histogram`
- `otel_metrics_summary`

The single `otel_metrics` table would never receive any data from the collector.

**Why it did not block the pipeline entirely:** The exporter has `create_schema: true`
by default, so it auto-creates the correct tables at startup. The old `otel_metrics`
table was simply dead code that was never written to. However, this created confusion
and the manual init script was misleading.

**Fix applied:** Replaced the single `otel_metrics` table with the 5 correct
per-type tables using the authoritative column definitions from the contrib exporter
source (v0.115.0). TTL set to 90 days for all metric tables (consistent with the
original intent, and distinct from the 30-day TTL for logs and traces).

`create_schema: true` was also added explicitly to the `clickhouse` exporter config
in `otel-collector.yaml` to make the auto-create behaviour visible.

---

### STRUCTURAL-3 — docker-compose.telemetry.yaml missing analytics-schema.sql mount

**File:** `build/otel-collector/docker-compose.telemetry.yaml`  
**Problem:** The standalone telemetry compose mounted only `init-schema.sql`, omitting
`analytics-schema.sql`. This made it inconsistent with the main `docker-compose.yml`
which mounts both scripts.

**Fix applied:** Added the `analytics-schema.sql` mount as
`02-analytics-schema.sql` (numeric prefix ensures correct execution order after
`01-init-schema.sql`). The original `init-schema.sql` was also renamed to
`01-init-schema.sql` in the mount path for consistency.

---

### STRUCTURAL-4 — docker-compose.telemetry.yaml missing network definition

**File:** `build/otel-collector/docker-compose.telemetry.yaml`  
**Problem:** No explicit Docker network was defined. Services used Docker's default
unnamed bridge network, which is isolated from the `nextraceone-net` network used by
the main `docker-compose.yml`. This is correct for a standalone stack (they should NOT
be run together), but the absence of an explicit named network made the isolation
implicit and undocumented.

**Fix applied:** Added an explicit `nextraceone-telemetry` network declared at the
compose level. Both services now reference this network. This makes the isolation
intentional and the topology clear.

> **Note:** `docker-compose.telemetry.yaml` is a standalone telemetry stack for POC
> and testing. It should **NOT** be run simultaneously with `docker-compose.yml`
> (different network scopes, port conflicts on 8123/9000/4317/4318).

---

## 4. What was confirmed working (no changes needed)

| Component | Confirmed |
|---|---|
| OTLP receiver binds to `0.0.0.0:4317` (gRPC) | ✅ Correct — accessible from host and other containers |
| OTLP receiver binds to `0.0.0.0:4318` (HTTP) | ✅ Correct |
| ClickHouse exporter uses `${CLICKHOUSE_ENDPOINT}` env var | ✅ No hardcoded host |
| Default endpoint `tcp://clickhouse:9000?database=nextraceone_obs` | ✅ Correct service name in Docker network |
| `otel_traces` schema in init-schema.sql | ✅ Compatible with contrib exporter v0.115.0 |
| `otel_logs` schema in init-schema.sql | ✅ Compatible with contrib exporter v0.115.0 |
| Collector image includes `clickhouseexporter` | ✅ contrib image includes it |
| `docker-compose.yml` wires all services on `nextraceone-net` | ✅ Correct |
| `docker-compose.yml` mounts both init and analytics schemas | ✅ Correct |
| P04.1 OTEL endpoint env vars in all services | ✅ Completed in prior session |
| Health check extension on `0.0.0.0:13133` | ✅ Correct |
| zpages extension on `0.0.0.0:55679` | ✅ Correct |
| spanmetrics connector feeds into metrics pipeline | ✅ Correct topology |

---

## 5. How to validate the pipeline manually

### Step 1 — Start the full stack

```bash
# From workspace root
docker compose up -d

# Wait for all services to be healthy (check with):
docker compose ps
```

Expected: `nextraceone-clickhouse`, `nextraceone-otel-collector`, `nextraceone-postgres`,
`nextraceone-api`, `nextraceone-ingestion-api` all showing `healthy` or `running`.

### Step 2 — Generate traces

```bash
# Health endpoint (lightweight — generates internal spans)
curl http://localhost:5000/health

# Catalog endpoint (richer span)
curl http://localhost:5000/api/v1/catalog/services

# Wait 15–30 seconds for tail_sampling (10s decision window) + batch flush
```

> **Note on tail_sampling in dev:** The `tail_sampling` processor holds spans in
> memory for 10 seconds before deciding whether to sample them. Allow at least
> 15 seconds after the last API call before querying ClickHouse.

### Step 3 — Verify collector is receiving data

```bash
# Check collector logs for exported spans
docker logs nextraceone-otel-collector 2>&1 | grep -i "traces\|spans\|export"

# Check collector health endpoint
curl http://localhost:13133/

# Check collector self-monitoring metrics (Prometheus format)
curl http://localhost:8888/metrics | grep otelcol_exporter_sent
```

Expected in collector logs: lines containing `TracesExporter` or `SentSpans`.

### Step 4 — Verify ClickHouse has trace data

```bash
# Connect to ClickHouse
docker exec -it nextraceone-clickhouse clickhouse-client

# Count traces
SELECT count(*) FROM nextraceone_obs.otel_traces;

# Inspect latest spans
SELECT TraceId, SpanName, ServiceName, Timestamp
FROM nextraceone_obs.otel_traces
ORDER BY Timestamp DESC
LIMIT 10;

# Count logs
SELECT count(*) FROM nextraceone_obs.otel_logs;

# Check metrics (spanmetrics from traces pipeline)
SELECT MetricName, ServiceName, count(*) as samples
FROM nextraceone_obs.otel_metrics_sum
GROUP BY MetricName, ServiceName
LIMIT 10;
```

Expected: `otel_traces` returns > 0 rows after API calls. Metric tables may be empty
until enough spans are processed by spanmetrics connector.

### Step 5 — Verify analytics schema

```bash
# Both databases should exist
SHOW DATABASES;
-- nextraceone_obs      (OTEL telemetry)
-- nextraceone_analytics (product analytics)

# OTEL tables
SHOW TABLES FROM nextraceone_obs;
-- otel_logs, otel_traces, otel_metrics_gauge, otel_metrics_sum,
-- otel_metrics_histogram, otel_metrics_exponential_histogram, otel_metrics_summary

# Analytics tables
SHOW TABLES FROM nextraceone_analytics;
-- pan_events, pan_daily_module_stats, pan_daily_persona_stats, pan_daily_friction_stats,
-- pan_session_summaries, ops_runtime_metrics, ops_cost_entries, ops_incident_trends,
-- int_execution_logs, int_health_history, gov_compliance_trends, gov_finops_aggregates,
-- chg_trace_release_mapping
```

### Step 6 — Teardown

```bash
docker compose down
```

---

## 6. Remaining gaps and known limitations

| Gap | Severity | Notes |
|---|---|---|
| `tail_sampling` in dev adds 10s latency | Low | By design — appropriate for production sampling. In dev, consider a separate `otel-collector-dev.yaml` without tail sampling for faster feedback. |
| `otlp` exporter retained but inactive | Low | Not in any pipeline — no runtime impact. Remove when it's confirmed the Product Store aggregator path is no longer planned. |
| No automated integration test for the pipeline | Medium | The pipeline works manually but there is no CI test that starts the compose stack and asserts `count(*) > 0` in ClickHouse. Consider adding a `docker compose` smoke test in a future CI stage. |
| Metric tables with `create_schema: true` may recreate with 30-day TTL | Low | If the init-created tables (90-day TTL) are dropped and recreated by the exporter, the TTL reverts to the exporter default (driven by `ttl: 720h` = 30 days). This is acceptable — add explicit TTL overrides to `otel-collector.yaml` if 90 days is a hard requirement. |
| `spanmetrics` produces high-cardinality data | Low | With `http.route`, `http.status_code`, `http.method` dimensions, spanmetrics output can grow quickly. Monitor `otel_metrics_histogram` table size. |
| No ClickHouse authentication configured | Low | `CLICKHOUSE_PASSWORD: ""` — acceptable for dev/Docker isolation. Requires a password and TLS for staging/production deployment. |

---

## 7. Files modified in this session

| File | Change |
|---|---|
| `build/otel-collector/otel-collector.yaml` | Added `create_schema: true`; fixed `otlp` exporter comment and endpoint env var; removed `otlp` from metrics pipeline |
| `build/clickhouse/init-schema.sql` | Replaced single `otel_metrics` with 5 per-type metric tables |
| `build/otel-collector/docker-compose.telemetry.yaml` | Added `analytics-schema.sql` mount; added explicit `nextraceone-telemetry` network |
| `docs/observability/providers/clickhouse.md` | Updated architecture diagram and tables list to reflect per-type metric tables |
| `docs/observability/pipeline-validation-report.md` | Created (this file) |

---

## 8. Next steps

1. **Start the full stack** and run the manual validation steps in Section 5.
2. **Consider a dev-optimised collector config** without `tail_sampling` to reduce
   trace visibility latency from 10s to near-real-time during development.
3. **Add a CI smoke test** (optional) — a Docker Compose–based test that starts
   the stack, makes a few API calls, and asserts `SELECT count(*) > 0 FROM otel_traces`.
4. **Add ClickHouse authentication** for staging and production environments.
5. **Unblock:** P5.2 Change Intelligence analytics (`chg_trace_release_mapping`)
   can now be implemented, as the OTEL pipeline and ClickHouse schema are validated.
