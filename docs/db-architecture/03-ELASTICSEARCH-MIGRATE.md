# 03 — Schemas Elasticsearch (Store Alternativo)

> Este ficheiro define os **índices Elasticsearch equivalentes** às tabelas ClickHouse do ficheiro
> [02-CLICKHOUSE-MIGRATE.md](./02-CLICKHOUSE-MIGRATE.md). Um cliente que escolha Elasticsearch
> como analytics store usa estes índices em vez das tabelas ClickHouse.
>
> **O código da aplicação é idêntico** — apenas o provider concreto de `IAnalyticsStore` muda.

---

## Considerações de design ES vs ClickHouse

| Aspecto | Decisão para ES |
|---------|----------------|
| Time-series | Usar **TSDS** (Time Series Data Streams) para séries temporais |
| Rollup | Usar **ILM policies** para arquivamento automático |
| Aggregations | Usar **doc_values** em todos os campos numéricos |
| Full-text | Usar **text** fields com `analyzer: "standard"` só onde necessário |
| Low-cardinality | Usar `keyword` (não `text`) para campos de categoria |
| Storage | Usar **_source: false** + stored fields em índices de alto volume |

---

## 1. Módulo Observability

### 1.1 `service_metrics_snapshots` (TSDS)

```json
PUT _index_template/service_metrics_snapshots
{
  "index_patterns": ["nextraceone.service_metrics_snapshots*"],
  "data_stream": {},
  "template": {
    "settings": {
      "mode": "time_series",
      "routing_path": ["tenant_id", "service_id"],
      "number_of_shards": 2,
      "number_of_replicas": 1
    },
    "mappings": {
      "properties": {
        "@timestamp":        { "type": "date" },
        "tenant_id":         { "type": "keyword", "time_series_dimension": true },
        "service_id":        { "type": "keyword", "time_series_dimension": true },
        "service_name":      { "type": "keyword" },
        "cpu_usage_pct":     { "type": "float",   "time_series_metric": "gauge" },
        "memory_usage_pct":  { "type": "float",   "time_series_metric": "gauge" },
        "request_rate":      { "type": "double",  "time_series_metric": "gauge" },
        "error_rate":        { "type": "double",  "time_series_metric": "gauge" },
        "p50_latency_ms":    { "type": "float",   "time_series_metric": "gauge" },
        "p95_latency_ms":    { "type": "float",   "time_series_metric": "gauge" },
        "p99_latency_ms":    { "type": "float",   "time_series_metric": "gauge" },
        "metadata":          { "type": "object",  "enabled": false }
      }
    }
  }
}

PUT _ilm/policy/metrics_2yr
{
  "policy": {
    "phases": {
      "hot":    { "min_age": "0ms", "actions": { "rollover": { "max_age": "7d", "max_primary_shard_size": "10gb" } } },
      "warm":   { "min_age": "30d", "actions": { "shrink": { "number_of_shards": 1 }, "forcemerge": { "max_num_segments": 1 } } },
      "cold":   { "min_age": "90d", "actions": { "freeze": {} } },
      "delete": { "min_age": "730d","actions": { "delete": {} } }
    }
  }
}
```

---

### 1.2 `runtime_snapshots` (TSDS)

```json
PUT _index_template/runtime_snapshots
{
  "index_patterns": ["nextraceone.runtime_snapshots*"],
  "data_stream": {},
  "template": {
    "settings": { "mode": "time_series", "routing_path": ["tenant_id", "service_id"] },
    "mappings": {
      "properties": {
        "@timestamp":       { "type": "date" },
        "tenant_id":        { "type": "keyword", "time_series_dimension": true },
        "environment_id":   { "type": "keyword", "time_series_dimension": true },
        "service_id":       { "type": "keyword", "time_series_dimension": true },
        "heap_used_bytes":  { "type": "long",  "time_series_metric": "gauge" },
        "heap_total_bytes": { "type": "long",  "time_series_metric": "gauge" },
        "thread_count":     { "type": "integer","time_series_metric": "gauge" },
        "gc_pause_ms":      { "type": "float",  "time_series_metric": "gauge" },
        "open_fds":         { "type": "integer","time_series_metric": "gauge" },
        "uptime_seconds":   { "type": "long",   "time_series_metric": "counter" }
      }
    }
  }
}
```

---

### 1.3 `reliability_snapshots` (TSDS)

```json
PUT _index_template/reliability_snapshots
{
  "index_patterns": ["nextraceone.reliability_snapshots*"],
  "data_stream": {},
  "template": {
    "settings": { "mode": "time_series", "routing_path": ["tenant_id", "service_id"] },
    "mappings": {
      "properties": {
        "@timestamp":        { "type": "date" },
        "tenant_id":         { "type": "keyword", "time_series_dimension": true },
        "service_id":        { "type": "keyword", "time_series_dimension": true },
        "availability_pct":  { "type": "double", "time_series_metric": "gauge" },
        "mttr_minutes":      { "type": "float",  "time_series_metric": "gauge" },
        "incident_count":    { "type": "integer","time_series_metric": "counter" },
        "error_budget_pct":  { "type": "double", "time_series_metric": "gauge" },
        "deployment_count":  { "type": "integer","time_series_metric": "counter" }
      }
    }
  }
}
```

---

### 1.4 `alert_firing_records` (regular data stream)

```json
PUT _index_template/alert_firing_records
{
  "index_patterns": ["nextraceone.alert_firing_records*"],
  "data_stream": {},
  "template": {
    "settings": { "number_of_shards": 1 },
    "mappings": {
      "properties": {
        "@timestamp":   { "type": "date" },
        "tenant_id":    { "type": "keyword" },
        "alert_id":     { "type": "keyword" },
        "alert_name":   { "type": "keyword" },
        "severity":     { "type": "keyword" },
        "fired_at":     { "type": "date" },
        "resolved_at":  { "type": "date" },
        "duration_sec": { "type": "integer" },
        "labels":       { "type": "flattened" },
        "annotations":  { "type": "flattened" }
      }
    }
  }
}
```

---

## 2. Módulo AI Knowledge / Governance

### 2.1 `token_usage_ledger` (data stream com rollup)

```json
PUT _index_template/token_usage_ledger
{
  "index_patterns": ["nextraceone.token_usage_ledger*"],
  "data_stream": {},
  "template": {
    "settings": { "number_of_shards": 2 },
    "mappings": {
      "properties": {
        "@timestamp":         { "type": "date" },
        "tenant_id":          { "type": "keyword" },
        "agent_id":           { "type": "keyword" },
        "agent_name":         { "type": "keyword" },
        "model_id":           { "type": "keyword" },
        "model_provider":     { "type": "keyword" },
        "prompt_tokens":      { "type": "integer", "doc_values": true },
        "completion_tokens":  { "type": "integer", "doc_values": true },
        "total_tokens":       { "type": "integer", "doc_values": true },
        "cost_usd":           { "type": "double",  "doc_values": true },
        "latency_ms":         { "type": "integer" },
        "request_id":         { "type": "keyword" },
        "skill_id":           { "type": "keyword" }
      }
    }
  }
}
```

---

### 2.2 `external_inference_records`

```json
PUT _index_template/external_inference_records
{
  "index_patterns": ["nextraceone.external_inference_records*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":     { "type": "date" },
        "tenant_id":      { "type": "keyword" },
        "provider":       { "type": "keyword" },
        "model":          { "type": "keyword" },
        "endpoint":       { "type": "keyword" },
        "status_code":    { "type": "short" },
        "prompt_tokens":  { "type": "integer", "doc_values": true },
        "output_tokens":  { "type": "integer", "doc_values": true },
        "cost_usd":       { "type": "double",  "doc_values": true },
        "latency_ms":     { "type": "integer" },
        "error_code":     { "type": "keyword" },
        "request_id":     { "type": "keyword" }
      }
    }
  }
}
```

---

### 2.3 `model_prediction_samples`

```json
PUT _index_template/model_prediction_samples
{
  "index_patterns": ["nextraceone.model_prediction_samples*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":     { "type": "date" },
        "tenant_id":      { "type": "keyword" },
        "model_id":       { "type": "keyword" },
        "model_version":  { "type": "keyword" },
        "input_hash":     { "type": "keyword" },
        "confidence":     { "type": "float",  "doc_values": true },
        "prediction_ms":  { "type": "integer" },
        "label":          { "type": "keyword" },
        "ground_truth":   { "type": "keyword" },
        "correct":        { "type": "boolean" }
      }
    }
  }
}
```

---

### 2.4 `benchmark_snapshots`

```json
PUT _index_template/benchmark_snapshots
{
  "index_patterns": ["nextraceone.benchmark_snapshots*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":         { "type": "date" },
        "tenant_id":          { "type": "keyword" },
        "agent_id":           { "type": "keyword" },
        "benchmark_score":    { "type": "float", "doc_values": true },
        "accuracy":           { "type": "float", "doc_values": true },
        "normalized_rating":  { "type": "float", "doc_values": true },
        "feedback_coverage":  { "type": "float", "doc_values": true },
        "rl_bonus":           { "type": "float", "doc_values": true },
        "tier":               { "type": "keyword" }
      }
    }
  }
}
```

---

## 3. Módulo Cost Management

### 3.1 `cost_records`

```json
PUT _index_template/cost_records
{
  "index_patterns": ["nextraceone.cost_records*"],
  "data_stream": {},
  "template": {
    "settings": { "number_of_shards": 2 },
    "mappings": {
      "properties": {
        "@timestamp":      { "type": "date" },
        "tenant_id":       { "type": "keyword" },
        "cost_center_id":  { "type": "keyword" },
        "category":        { "type": "keyword" },
        "sub_category":    { "type": "keyword" },
        "resource_id":     { "type": "keyword" },
        "resource_name":   { "type": "keyword" },
        "quantity":        { "type": "double", "doc_values": true },
        "unit":            { "type": "keyword" },
        "unit_cost_usd":   { "type": "double", "doc_values": true },
        "total_cost_usd":  { "type": "double", "doc_values": true },
        "currency":        { "type": "keyword" },
        "tags":            { "type": "flattened" }
      }
    }
  }
}
```

---

### 3.2 `burn_rate_snapshots` (TSDS)

```json
PUT _index_template/burn_rate_snapshots
{
  "index_patterns": ["nextraceone.burn_rate_snapshots*"],
  "data_stream": {},
  "template": {
    "settings": { "mode": "time_series", "routing_path": ["tenant_id", "budget_id"] },
    "mappings": {
      "properties": {
        "@timestamp":      { "type": "date" },
        "tenant_id":       { "type": "keyword", "time_series_dimension": true },
        "budget_id":       { "type": "keyword", "time_series_dimension": true },
        "allocated_usd":   { "type": "double", "time_series_metric": "gauge" },
        "spent_usd":       { "type": "double", "time_series_metric": "gauge" },
        "forecasted_usd":  { "type": "double", "time_series_metric": "gauge" },
        "burn_rate_pct":   { "type": "float",  "time_series_metric": "gauge" },
        "days_remaining":  { "type": "integer" },
        "status":          { "type": "keyword" }
      }
    }
  }
}
```

---

## 4. Módulo SLO / Reliability

### 4.1 `error_budget_snapshots` (TSDS)

```json
PUT _index_template/error_budget_snapshots
{
  "index_patterns": ["nextraceone.error_budget_snapshots*"],
  "data_stream": {},
  "template": {
    "settings": { "mode": "time_series", "routing_path": ["tenant_id", "slo_id"] },
    "mappings": {
      "properties": {
        "@timestamp":        { "type": "date" },
        "tenant_id":         { "type": "keyword", "time_series_dimension": true },
        "slo_id":            { "type": "keyword", "time_series_dimension": true },
        "target_pct":        { "type": "double", "time_series_metric": "gauge" },
        "achieved_pct":      { "type": "double", "time_series_metric": "gauge" },
        "error_budget_pct":  { "type": "double", "time_series_metric": "gauge" },
        "burn_rate_1h":      { "type": "float",  "time_series_metric": "gauge" },
        "burn_rate_6h":      { "type": "float",  "time_series_metric": "gauge" },
        "burn_rate_24h":     { "type": "float",  "time_series_metric": "gauge" },
        "burn_rate_72h":     { "type": "float",  "time_series_metric": "gauge" },
        "status":            { "type": "keyword" }
      }
    }
  }
}
```

---

### 4.2 `sli_measurements` (TSDS)

```json
PUT _index_template/sli_measurements
{
  "index_patterns": ["nextraceone.sli_measurements*"],
  "data_stream": {},
  "template": {
    "settings": { "mode": "time_series", "routing_path": ["tenant_id", "slo_id", "sli_type"] },
    "mappings": {
      "properties": {
        "@timestamp":    { "type": "date" },
        "tenant_id":     { "type": "keyword", "time_series_dimension": true },
        "slo_id":        { "type": "keyword", "time_series_dimension": true },
        "sli_type":      { "type": "keyword", "time_series_dimension": true },
        "value":         { "type": "double",  "time_series_metric": "gauge" },
        "good_events":   { "type": "long",    "time_series_metric": "counter" },
        "bad_events":    { "type": "long",    "time_series_metric": "counter" },
        "total_events":  { "type": "long",    "time_series_metric": "counter" }
      }
    }
  }
}
```

---

## 5. Módulo Analytics / Product

### 5.1 `analytics_events` — índice com full-text

> Este é o índice onde o ES brilha mais: pesquisa de eventos por keywords, facetas dinâmicas.

```json
PUT _index_template/analytics_events
{
  "index_patterns": ["nextraceone.analytics_events*"],
  "data_stream": {},
  "template": {
    "settings": { "number_of_shards": 3 },
    "mappings": {
      "properties": {
        "@timestamp":  { "type": "date" },
        "tenant_id":   { "type": "keyword" },
        "event_id":    { "type": "keyword" },
        "user_id":     { "type": "keyword" },
        "session_id":  { "type": "keyword" },
        "event_type":  { "type": "keyword" },
        "entity_type": { "type": "keyword" },
        "entity_id":   { "type": "keyword" },
        "properties":  { "type": "flattened" },
        "ip_hash":     { "type": "keyword" },
        "user_agent":  { "type": "keyword" },
        "country":     { "type": "keyword" }
      }
    }
  }
}
```

---

### 5.2 `dashboard_usage_events`

```json
PUT _index_template/dashboard_usage_events
{
  "index_patterns": ["nextraceone.dashboard_usage_events*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":    { "type": "date" },
        "tenant_id":     { "type": "keyword" },
        "user_id":       { "type": "keyword" },
        "dashboard_id":  { "type": "keyword" },
        "action":        { "type": "keyword" },
        "duration_sec":  { "type": "integer" },
        "filters_json":  { "type": "flattened" }
      }
    }
  }
}
```

---

### 5.3 `productivity_snapshots`

```json
PUT _index_template/productivity_snapshots
{
  "index_patterns": ["nextraceone.productivity_snapshots*"],
  "data_stream": {},
  "template": {
    "settings": { "mode": "time_series", "routing_path": ["tenant_id", "team_id"] },
    "mappings": {
      "properties": {
        "@timestamp":         { "type": "date" },
        "tenant_id":          { "type": "keyword", "time_series_dimension": true },
        "team_id":            { "type": "keyword", "time_series_dimension": true },
        "period_type":        { "type": "keyword" },
        "deploy_frequency":   { "type": "float", "time_series_metric": "gauge" },
        "lead_time_hours":    { "type": "float", "time_series_metric": "gauge" },
        "change_fail_rate":   { "type": "float", "time_series_metric": "gauge" },
        "mttr_hours":         { "type": "float", "time_series_metric": "gauge" },
        "pr_cycle_time_h":    { "type": "float", "time_series_metric": "gauge" },
        "review_coverage":    { "type": "float", "time_series_metric": "gauge" }
      }
    }
  }
}
```

---

## 6. Módulo Security

### 6.1 `security_events` — rich full-text + facets

```json
PUT _index_template/security_events
{
  "index_patterns": ["nextraceone.security_events*"],
  "data_stream": {},
  "template": {
    "settings": { "number_of_shards": 2 },
    "mappings": {
      "properties": {
        "@timestamp":    { "type": "date" },
        "tenant_id":     { "type": "keyword" },
        "event_id":      { "type": "keyword" },
        "event_type":    { "type": "keyword" },
        "severity":      { "type": "keyword" },
        "actor_id":      { "type": "keyword" },
        "actor_type":    { "type": "keyword" },
        "resource_type": { "type": "keyword" },
        "resource_id":   { "type": "keyword" },
        "action":        { "type": "text", "fields": { "raw": { "type": "keyword" } } },
        "outcome":       { "type": "keyword" },
        "ip_address":    { "type": "ip" },
        "country":       { "type": "keyword" },
        "details":       { "type": "flattened" }
      }
    }
  }
}
```

---

### 6.2 `threat_signals`

```json
PUT _index_template/threat_signals
{
  "index_patterns": ["nextraceone.threat_signals*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":    { "type": "date" },
        "tenant_id":     { "type": "keyword" },
        "signal_type":   { "type": "keyword" },
        "source_ip":     { "type": "ip" },
        "actor_id":      { "type": "keyword" },
        "risk_score":    { "type": "float", "doc_values": true },
        "indicators":    { "type": "flattened" },
        "correlated_to": { "type": "keyword" }
      }
    }
  }
}
```

---

## 7. Módulo Developer Productivity

### 7.1–7.4 Agent + Code Review + Deploy + Pipeline

```json
PUT _index_template/agent_query_records
{
  "index_patterns": ["nextraceone.agent_query_records*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":    { "type": "date" },
        "tenant_id":     { "type": "keyword" },
        "user_id":       { "type": "keyword" },
        "agent_id":      { "type": "keyword" },
        "query_type":    { "type": "keyword" },
        "response_ms":   { "type": "integer" },
        "tokens_used":   { "type": "integer" },
        "satisfied":     { "type": "boolean" },
        "ide_type":      { "type": "keyword" },
        "language":      { "type": "keyword" }
      }
    }
  }
}

PUT _index_template/code_review_cycles
{
  "index_patterns": ["nextraceone.code_review_cycles*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":        { "type": "date" },
        "tenant_id":         { "type": "keyword" },
        "repo_id":           { "type": "keyword" },
        "pr_id":             { "type": "keyword" },
        "merged_at":         { "type": "date" },
        "cycle_time_hours":  { "type": "float" },
        "review_count":      { "type": "short" },
        "comment_count":     { "type": "integer" },
        "author_id":         { "type": "keyword" },
        "size_lines":        { "type": "integer" }
      }
    }
  }
}

PUT _index_template/deployment_records
{
  "index_patterns": ["nextraceone.deployment_records*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":    { "type": "date" },
        "tenant_id":     { "type": "keyword" },
        "service_id":    { "type": "keyword" },
        "environment":   { "type": "keyword" },
        "version":       { "type": "keyword" },
        "strategy":      { "type": "keyword" },
        "duration_sec":  { "type": "integer" },
        "success":       { "type": "boolean" },
        "triggered_by":  { "type": "keyword" }
      }
    }
  }
}

PUT _index_template/pipeline_run_records
{
  "index_patterns": ["nextraceone.pipeline_run_records*"],
  "data_stream": {},
  "template": {
    "mappings": {
      "properties": {
        "@timestamp":   { "type": "date" },
        "tenant_id":    { "type": "keyword" },
        "pipeline_id":  { "type": "keyword" },
        "run_id":       { "type": "keyword" },
        "status":       { "type": "keyword" },
        "duration_sec": { "type": "integer" },
        "stage":        { "type": "keyword" },
        "triggered_by": { "type": "keyword" },
        "branch":       { "type": "keyword" },
        "commit_sha":   { "type": "keyword" }
      }
    }
  }
}
```

---

## Índices de Full-Text Search (especiais do ES)

> Estes índices existem **apenas** quando o cliente escolhe Elasticsearch. No ClickHouse, o mesmo
> conteúdo é armazenado com índice `tokenbf_v1` (mais simples). O ES oferece Lucene nativo aqui.

### `knowledge_document_content`

```json
PUT nextraceone.knowledge_document_content
{
  "settings": {
    "number_of_shards": 2,
    "analysis": {
      "analyzer": {
        "content_analyzer": {
          "type": "custom",
          "tokenizer": "standard",
          "filter": ["lowercase", "stop", "snowball"]
        }
      }
    }
  },
  "mappings": {
    "properties": {
      "document_id":    { "type": "keyword" },
      "tenant_id":      { "type": "keyword" },
      "title":          { "type": "text", "analyzer": "content_analyzer", "fields": { "keyword": { "type": "keyword" } } },
      "content":        { "type": "text", "analyzer": "content_analyzer" },
      "summary":        { "type": "text", "analyzer": "content_analyzer" },
      "tags":           { "type": "keyword" },
      "source_type":    { "type": "keyword" },
      "created_at":     { "type": "date" },
      "updated_at":     { "type": "date" },
      "vector":         { "type": "dense_vector", "dims": 1536, "similarity": "cosine" }
    }
  }
}
```

### `contract_version_content`

```json
PUT nextraceone.contract_version_content
{
  "settings": { "number_of_shards": 1 },
  "mappings": {
    "properties": {
      "version_id":    { "type": "keyword" },
      "contract_id":   { "type": "keyword" },
      "tenant_id":     { "type": "keyword" },
      "spec_content":  { "type": "text", "analyzer": "standard" },
      "description":   { "type": "text", "analyzer": "standard" },
      "status":        { "type": "keyword" },
      "version_tag":   { "type": "keyword" },
      "created_at":    { "type": "date" }
    }
  }
}
```

---

## Tabela de equivalência ClickHouse ↔ Elasticsearch

| Tabela ClickHouse | Índice Elasticsearch | Diferença principal |
|-------------------|---------------------|---------------------|
| `service_metrics_snapshots` | `nextraceone.service_metrics_snapshots` (TSDS) | CH: columnar + MergeTree; ES: TSDS rollup |
| `token_usage_ledger` | `nextraceone.token_usage_ledger` | CH: SummingMergeTree pre-agg; ES: aggs runtime |
| `cost_records` | `nextraceone.cost_records` | CH: SummingMergeTree; ES: sum aggs |
| `error_budget_snapshots` | `nextraceone.error_budget_snapshots` (TSDS) | CH: TTL automático; ES: ILM policy |
| `security_events` | `nextraceone.security_events` | CH: tokenbf; ES: Lucene full-text |
| `analytics_events` | `nextraceone.analytics_events` | CH: ngram index; ES: facets nativas |
| `knowledge_document_content` | `nextraceone.knowledge_document_content` | CH: apenas basic FTS; ES: snowball + vector |
