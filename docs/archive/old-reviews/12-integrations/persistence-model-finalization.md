# Integrations — Persistence Model Finalization

> **Module:** Integrations (12)  
> **Table prefix (target):** `int_`  
> **Date:** 2026-03-25  
> **Status:** Persistence model finalized — pending migration recreation

---

## 1. Tabelas actuais do módulo

| Tabela actual (em Governance) | DbContext | Prefix | Entidade |
|-------------------------------|-----------|--------|----------|
| `gov_integration_connectors` | `GovernanceDbContext` | `gov_` ❌ | `IntegrationConnector` |
| `gov_ingestion_sources` | `GovernanceDbContext` | `gov_` ❌ | `IngestionSource` |
| `gov_ingestion_executions` | `GovernanceDbContext` | `gov_` ❌ | `IngestionExecution` |

**Problema:** Todas usam prefixo `gov_` quando deveriam usar `int_`.

---

## 2. Mapeamento entidades → tabelas (target)

| Entidade | Tabela target | Prefix target |
|----------|--------------|---------------|
| `IntegrationConnector` | `int_integration_connectors` | `int_` ✅ |
| `IngestionSource` | `int_ingestion_sources` | `int_` ✅ |
| `IngestionExecution` | `int_ingestion_executions` | `int_` ✅ |

---

## 3. Definição detalhada — `int_integration_connectors`

### Primary Key
| Coluna | Tipo | Nota |
|--------|------|------|
| `id` | `uuid` | PK, strongly typed `IntegrationConnectorId` |

### Colunas
| Coluna | Tipo PostgreSQL | Nullable | Default | Nota |
|--------|----------------|----------|---------|------|
| `id` | `uuid` | NOT NULL | gen_random_uuid() | PK |
| `tenant_id` | `uuid` | NOT NULL | — | RLS via interceptor |
| `name` | `varchar(200)` | NOT NULL | — | Unique por tenant |
| `connector_type` | `varchar(100)` | NOT NULL | — | "CI/CD", "Telemetry" |
| `description` | `text` | NULL | — | — |
| `provider` | `varchar(200)` | NOT NULL | — | "GitHub", "Datadog" |
| `endpoint` | `varchar(500)` | NULL | — | URL do sistema externo |
| `status` | `varchar(50)` | NOT NULL | 'Pending' | Enum como string |
| `health` | `varchar(50)` | NOT NULL | 'Unknown' | Enum como string |
| `last_success_at` | `timestamptz` | NULL | — | — |
| `last_error_at` | `timestamptz` | NULL | — | — |
| `last_error_message` | `varchar(2000)` | NULL | — | — |
| `freshness_lag_minutes` | `integer` | NULL | — | — |
| `total_executions` | `bigint` | NOT NULL | 0 | — |
| `successful_executions` | `bigint` | NOT NULL | 0 | — |
| `failed_executions` | `bigint` | NOT NULL | 0 | — |
| `environment` | `varchar(100)` | NOT NULL | 'Production' | ⚠️ Target: `environment_id uuid` |
| `authentication_mode` | `varchar(50)` | NOT NULL | 'Not configured' | ⚠️ Target: enum tipado |
| `polling_mode` | `varchar(50)` | NOT NULL | 'Not configured' | ⚠️ Target: enum tipado |
| `allowed_teams` | `jsonb` | NOT NULL | '[]' | Array de team names |
| `max_retry_attempts` | `integer` | NOT NULL | 3 | **NOVO** — retry policy |
| `retry_backoff_seconds` | `integer` | NOT NULL | 60 | **NOVO** — retry policy |
| `timeout_seconds` | `integer` | NOT NULL | 300 | **NOVO** — timeout |
| `credential_encrypted` | `text` | NULL | — | **NOVO** — via EncryptionInterceptor |
| `is_deleted` | `boolean` | NOT NULL | false | **NOVO** — soft delete |
| `deleted_at` | `timestamptz` | NULL | — | **NOVO** — soft delete |
| `created_at` | `timestamptz` | NOT NULL | now() | Audit |
| `updated_at` | `timestamptz` | NULL | — | Audit |
| `created_by` | `varchar(200)` | NULL | — | Audit (via AuditInterceptor) |
| `updated_by` | `varchar(200)` | NULL | — | Audit (via AuditInterceptor) |
| `xmin` | `xid` | — | — | **NOVO** — RowVersion/concurrency |

### Foreign Keys
Nenhuma FK externa — aggregate root.

### Índices
| Nome | Colunas | Tipo | Nota |
|------|---------|------|------|
| `PK_int_integration_connectors` | `id` | PK | — |
| `IX_int_connectors_tenant_name` | `tenant_id, name` | UNIQUE | Unique por tenant |
| `IX_int_connectors_type` | `connector_type` | Regular | Filtro por tipo |
| `IX_int_connectors_provider` | `provider` | Regular | Filtro por provider |
| `IX_int_connectors_status` | `status` | Regular | Filtro por status |
| `IX_int_connectors_health` | `health` | Regular | Filtro por health |
| `IX_int_connectors_environment` | `environment` | Regular | Filtro por ambiente |
| `IX_int_connectors_is_deleted` | `is_deleted` | Filtered (false) | Soft delete filter |

### Constraints
| Tipo | Detalhe |
|------|---------|
| CHECK | `status IN ('Pending','Active','Paused','Disabled','Failed','Configuring')` |
| CHECK | `health IN ('Unknown','Healthy','Degraded','Unhealthy','Critical')` |
| CHECK | `total_executions >= 0` |
| CHECK | `successful_executions >= 0` |
| CHECK | `failed_executions >= 0` |
| CHECK | `max_retry_attempts >= 0 AND max_retry_attempts <= 100` |
| CHECK | `timeout_seconds >= 1 AND timeout_seconds <= 3600` |

---

## 4. Definição detalhada — `int_ingestion_sources`

### Primary Key
| Coluna | Tipo | Nota |
|--------|------|------|
| `id` | `uuid` | PK, strongly typed `IngestionSourceId` |

### Colunas
| Coluna | Tipo PostgreSQL | Nullable | Default | Nota |
|--------|----------------|----------|---------|------|
| `id` | `uuid` | NOT NULL | gen_random_uuid() | PK |
| `tenant_id` | `uuid` | NOT NULL | — | RLS |
| `connector_id` | `uuid` | NOT NULL | — | FK → `int_integration_connectors` |
| `name` | `varchar(200)` | NOT NULL | — | — |
| `source_type` | `varchar(100)` | NOT NULL | — | "Webhook", "API Polling" |
| `data_domain` | `varchar(100)` | NOT NULL | — | "Changes", "Incidents" |
| `description` | `varchar(2000)` | NULL | — | — |
| `endpoint` | `varchar(500)` | NULL | — | URL da fonte |
| `trust_level` | `varchar(50)` | NOT NULL | 'Unverified' | Enum como string |
| `freshness_status` | `varchar(50)` | NOT NULL | 'Unknown' | Enum como string |
| `status` | `varchar(50)` | NOT NULL | 'Pending' | Enum como string |
| `last_data_received_at` | `timestamptz` | NULL | — | — |
| `last_processed_at` | `timestamptz` | NULL | — | — |
| `data_items_processed` | `bigint` | NOT NULL | 0 | — |
| `expected_interval_minutes` | `integer` | NULL | — | — |
| `is_deleted` | `boolean` | NOT NULL | false | **NOVO** — soft delete |
| `deleted_at` | `timestamptz` | NULL | — | **NOVO** |
| `created_at` | `timestamptz` | NOT NULL | now() | Audit |
| `updated_at` | `timestamptz` | NULL | — | Audit |
| `xmin` | `xid` | — | — | **NOVO** — concurrency |

### Foreign Keys
| FK | Target | On Delete |
|----|--------|-----------|
| `connector_id` → `int_integration_connectors.id` | IntegrationConnector | CASCADE |

### Índices
| Nome | Colunas | Tipo |
|------|---------|------|
| `PK_int_ingestion_sources` | `id` | PK |
| `IX_int_sources_connector` | `connector_id` | Regular |
| `IX_int_sources_connector_name` | `connector_id, name` | UNIQUE |
| `IX_int_sources_type` | `source_type` | Regular |
| `IX_int_sources_domain` | `data_domain` | Regular |
| `IX_int_sources_trust` | `trust_level` | Regular |
| `IX_int_sources_freshness` | `freshness_status` | Regular |
| `IX_int_sources_status` | `status` | Regular |

---

## 5. Definição detalhada — `int_ingestion_executions`

### Primary Key
| Coluna | Tipo | Nota |
|--------|------|------|
| `id` | `uuid` | PK, strongly typed `IngestionExecutionId` |

### Colunas
| Coluna | Tipo PostgreSQL | Nullable | Default | Nota |
|--------|----------------|----------|---------|------|
| `id` | `uuid` | NOT NULL | gen_random_uuid() | PK |
| `tenant_id` | `uuid` | NOT NULL | — | RLS |
| `connector_id` | `uuid` | NOT NULL | — | FK → `int_integration_connectors` |
| `source_id` | `uuid` | NULL | — | FK → `int_ingestion_sources` |
| `correlation_id` | `varchar(100)` | NULL | — | Tracking ID |
| `started_at` | `timestamptz` | NOT NULL | — | — |
| `completed_at` | `timestamptz` | NULL | — | — |
| `duration_ms` | `bigint` | NULL | — | Calculado |
| `result` | `varchar(50)` | NOT NULL | 'Running' | Enum como string |
| `items_processed` | `integer` | NOT NULL | 0 | — |
| `items_succeeded` | `integer` | NOT NULL | 0 | — |
| `items_failed` | `integer` | NOT NULL | 0 | — |
| `error_message` | `varchar(2000)` | NULL | — | — |
| `error_code` | `varchar(100)` | NULL | — | — |
| `retry_attempt` | `integer` | NOT NULL | 0 | — |
| `created_at` | `timestamptz` | NOT NULL | now() | Audit |

### Foreign Keys
| FK | Target | On Delete |
|----|--------|-----------|
| `connector_id` → `int_integration_connectors.id` | IntegrationConnector | CASCADE |
| `source_id` → `int_ingestion_sources.id` | IngestionSource | SET NULL |

### Índices
| Nome | Colunas | Tipo |
|------|---------|------|
| `PK_int_ingestion_executions` | `id` | PK |
| `IX_int_executions_connector` | `connector_id` | Regular |
| `IX_int_executions_source` | `source_id` | Regular |
| `IX_int_executions_correlation` | `correlation_id` | Regular |
| `IX_int_executions_started` | `started_at` | Regular |
| `IX_int_executions_result` | `result` | Regular |
| `IX_int_executions_retry` | `retry_attempt` | Regular |
| `IX_int_executions_connector_started` | `connector_id, started_at DESC` | Composite |

---

## 6. Divergências entre estado actual e modelo final

| # | Divergência | Estado actual | Target |
|---|-----------|--------------|--------|
| 1 | Table prefix | `gov_` | `int_` |
| 2 | DbContext | `GovernanceDbContext` | `IntegrationsDbContext` |
| 3 | RowVersion | Ausente | `xmin` em Connector e Source |
| 4 | Soft delete | Ausente | `is_deleted` + `deleted_at` em Connector e Source |
| 5 | Retry policy fields | Ausentes | `max_retry_attempts`, `retry_backoff_seconds`, `timeout_seconds` |
| 6 | Credential storage | Ausente | `credential_encrypted` com EncryptionInterceptor |
| 7 | Audit columns | `CreatedAt`, `UpdatedAt` | + `created_by`, `updated_by` via AuditInterceptor |
| 8 | CHECK constraints | Ausentes | Em todos os enums e counters |
| 9 | Unique constraint Name | `IX_Name` (global) | `IX_tenant_name` (por tenant) |
| 10 | Environment field | `varchar` livre | Target: `environment_id uuid?` |
| 11 | AuthenticationMode | `varchar` livre | Target: enum tipado |
| 12 | PollingMode | `varchar` livre | Target: enum tipado |
| 13 | Metadata em Execution | Ausente | `metadata jsonb?` para dados adicionais |

---

## 7. IntegrationsDbContext target

```csharp
public sealed class IntegrationsDbContext : NexTraceDbContextBase
{
    public DbSet<IntegrationConnector> IntegrationConnectors => Set<IntegrationConnector>();
    public DbSet<IngestionSource> IngestionSources => Set<IngestionSource>();
    public DbSet<IngestionExecution> IngestionExecutions => Set<IngestionExecution>();
}
```

**Herda de `NexTraceDbContextBase`:**
- ✅ TenantRlsInterceptor (PostgreSQL RLS)
- ✅ AuditInterceptor (CreatedAt/By, UpdatedAt/By)
- ✅ EncryptionInterceptor (AES-256-GCM) — para `credential_encrypted`
- ✅ OutboxInterceptor — para domain events
