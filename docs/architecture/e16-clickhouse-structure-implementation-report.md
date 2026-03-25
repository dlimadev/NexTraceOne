# E16 — Relatório de Implementação da Estrutura ClickHouse

> **Status:** CONCLUÍDO  
> **Data:** 2026-03-25  
> **Fase:** E16 — Implementação da Camada Analítica ClickHouse  
> **Precedido por:** E15 — Geração de Baseline PostgreSQL por Módulo  
> **Sucedido por:** E17 — Validação Ponta a Ponta do Produto  

---

## 1. Objetivo da Fase

Implementar a estrutura física inicial da camada analítica ClickHouse no NexTraceOne, cobrindo:

- definição e classificação dos módulos por nível ClickHouse
- criação do schema SQL analítico (`nextraceone_analytics`)
- implementação de tabelas analíticas por domínio
- abstracção C# para escrita analítica (`IAnalyticsWriter`)
- preparação da ingestão inicial
- validação da separação PostgreSQL vs ClickHouse
- configuração docker-compose para inicialização automática
- documentação do estado final

---

## 2. Classificação Final dos Módulos

### 2.1 IMPLEMENT_CLICKHOUSE_NOW (E16)

| Módulo | Nível ClickHouse | Justificação |
|--------|-----------------|-------------|
| **Product Analytics (13)** | REQUIRED | Módulo puramente analítico — eventos de uso, funnels, sessões, métricas de adopção |
| **Operational Intelligence (06)** | RECOMMENDED | Runtime metrics time-series, custo operacional, tendências de incidentes |
| **Integrations (12)** | RECOMMENDED | Logs de execução de conectores (alto volume append-only), histórico de health |
| **Governance Analytics (08)** | RECOMMENDED | Compliance score trends, FinOps contextuais por equipa/serviço |

### 2.2 PREPARE_ONLY (E16 — schema definido, tabelas comentadas)

| Módulo | Nível ClickHouse | Estado | Razão do Adiamento |
|--------|-----------------|--------|-------------------|
| **AI & Knowledge (07)** | OPTIONAL_LATER | Schema definido no SQL, tabelas comentadas | Core funcional em PostgreSQL; volume ainda não justifica ClickHouse no MVP1 |

### 2.3 NOT_IN_SCOPE_FOR_E16

| Módulo | Nível ClickHouse | Estado |
|--------|-----------------|--------|
| Identity & Access (01) | NONE | Totalmente em PostgreSQL |
| Environment Management (02) | NONE | Totalmente em PostgreSQL |
| Contracts (04) | NONE | Totalmente em PostgreSQL |
| Change Governance (05) | OPTIONAL_LATER | Não prioritário no E16 |
| Configuration (09) | NONE | Totalmente em PostgreSQL |
| Audit & Compliance (10) | OPTIONAL_LATER | Não prioritário no E16 |
| Notifications (11) | NONE | Totalmente em PostgreSQL |
| Service Catalog (03) | OPTIONAL_LATER | Não prioritário no E16 |

---

## 3. Convenções Implementadas

### 3.1 Base de Dados ClickHouse

| Base de Dados | Propósito |
|--------------|-----------|
| `nextraceone_obs` | Dados de observabilidade OpenTelemetry (logs, traces, métricas — já existente) |
| `nextraceone_analytics` | Dados analíticos de domínio (novo — criado no E16) |

### 3.2 Naming Conventions de Tabelas

| Prefixo | Módulo | Tipo |
|---------|--------|------|
| `pan_*` | Product Analytics | Eventos + materialized views |
| `ops_*` | Operational Intelligence | Métricas de runtime e custo |
| `int_*` | Integrations | Logs de execução e health |
| `gov_*` | Governance | Compliance + FinOps |
| `aik_*` | AI & Knowledge | (reservado, tabelas comentadas) |

### 3.3 Convenções de Colunas Técnicas

| Coluna | Tipo | Uso |
|--------|------|-----|
| `tenant_id` | UUID | Obrigatório em todas as tabelas — isolamento multi-tenant |
| `occurred_at` / `captured_at` / `created_at` | DateTime64(3, 'UTC') | Timestamp principal em UTC |
| `environment_id` / `environment` | UUID / LowCardinality(String) | Dimensão ambiente (opcional onde aplicável) |
| `service_id` / `service_name` | UUID / LowCardinality(String) | Chave de correlação com Catalog |

### 3.4 Engines Utilizadas

| Engine | Uso |
|--------|-----|
| `MergeTree` | Tabelas principais de eventos — queries flexíveis |
| `SummingMergeTree` | Agregações de tendências — somas automáticas por partition key |
| `AggregatingMergeTree` | Aggregating state — funnels e cohorts |

### 3.5 Particionamento

```
PARTITION BY (tenant_id, toYYYYMM(timestamp_column))
ORDER BY (tenant_id, ..., timestamp_column)
```

Todas as tabelas são particionadas por `tenant_id` + mês do timestamp principal.  
Isto garante:
- isolamento de dados por tenant em queries e manutenção
- compressão eficiente por período temporal
- TTL por partição para retenção automática

---

## 4. Tabelas Analíticas Criadas

### 4.1 Product Analytics (`nextraceone_analytics`)

| Tabela | Engine | TTL | Descrição |
|--------|--------|-----|-----------|
| `pan_events` | MergeTree | 2 anos | Tabela principal de eventos de uso de produto |
| `pan_daily_module_stats` | SummingMergeTree (MView) | 1 ano | Adopção por módulo — agregação diária |
| `pan_daily_persona_stats` | SummingMergeTree (MView) | 1 ano | Uso por persona — agregação diária |
| `pan_daily_friction_stats` | SummingMergeTree (MView) | 1 ano | Indicadores de fricção — agregação diária |
| `pan_session_summaries` | AggregatingMergeTree (MView) | 90 dias | Resumos de sessão para funnels |

**Total: 5 objectos (1 tabela base + 4 materialized views)**

### 4.2 Operational Intelligence (`nextraceone_analytics`)

| Tabela | Engine | TTL | Descrição |
|--------|--------|-----|-----------|
| `ops_runtime_metrics` | MergeTree | 90 dias | Métricas de runtime por serviço/ambiente (latência, error rate, CPU) |
| `ops_cost_entries` | MergeTree | 1 ano | Entradas de custo operacional por serviço/ambiente/período |
| `ops_incident_trends` | MergeTree | 1 ano | Stream de eventos do ciclo de vida de incidentes |

**Total: 3 tabelas**

### 4.3 Integrations (`nextraceone_analytics`)

| Tabela | Engine | TTL | Descrição |
|--------|--------|-----|-----------|
| `int_execution_logs` | MergeTree | 1 ano | Histórico de execuções de conectores (completadas) |
| `int_health_history` | MergeTree | 1 ano | Histórico de transições de health de conectores |

**Total: 2 tabelas**

### 4.4 Governance Analytics (`nextraceone_analytics`)

| Tabela | Engine | TTL | Descrição |
|--------|--------|-----|-----------|
| `gov_compliance_trends` | SummingMergeTree | 2 anos | Compliance score trends por serviço/política |
| `gov_finops_aggregates` | SummingMergeTree | 2 anos | Agregações FinOps contextuais por equipa/serviço |

**Total: 2 tabelas**

### 4.5 AI & Knowledge — PREPARE_ONLY (`nextraceone_analytics`)

| Tabela | Estado | Condição de Activação |
|--------|--------|----------------------|
| `aik_token_usage` | Comentada — schema definido | Ativar quando > 10K eventos/dia/tenant |
| `aik_model_performance` | Comentada — schema definido | Ativar junto com `aik_token_usage` |

**Total: 2 tabelas definidas, 0 activas**

### Resumo de Objectos Criados no E16

| Módulo | Tabelas | Materialized Views | Total |
|--------|---------|-------------------|-------|
| Product Analytics | 1 | 4 | 5 |
| Operational Intelligence | 3 | 0 | 3 |
| Integrations | 2 | 0 | 2 |
| Governance | 2 | 0 | 2 |
| **Total activo** | **8** | **4** | **12** |
| AI & Knowledge (prepare only) | 2 (comentadas) | 0 | 0 activas |

---

## 5. Camada C# de Escrita Analítica

### 5.1 Ficheiros Criados

| Ficheiro | Localização | Descrição |
|----------|------------|-----------|
| `IAnalyticsWriter.cs` | `BuildingBlocks.Observability/Analytics/Abstractions/` | Interface principal — porta de escrita analítica |
| `AnalyticsOptions.cs` | `BuildingBlocks.Observability/Analytics/Configuration/` | Configuração de Analytics (enabled, connection string, timeouts) |
| `AnalyticsRecords.cs` | `BuildingBlocks.Observability/Analytics/Events/` | Todos os record types por módulo (9 tipos) |
| `NullAnalyticsWriter.cs` | `BuildingBlocks.Observability/Analytics/Writers/` | Graceful degradation — sem I/O quando ClickHouse desactivado |
| `ClickHouseAnalyticsWriter.cs` | `BuildingBlocks.Observability/Analytics/Writers/` | Implementação real via HTTP interface do ClickHouse |

### 5.2 Tipos de Eventos Definidos

| Record | Tabela Destino | Módulo |
|--------|---------------|--------|
| `ProductAnalyticsRecord` | `pan_events` | Product Analytics |
| `RuntimeMetricRecord` | `ops_runtime_metrics` | Operational Intelligence |
| `CostEntryRecord` | `ops_cost_entries` | Operational Intelligence |
| `IncidentTrendRecord` | `ops_incident_trends` | Operational Intelligence |
| `IntegrationExecutionRecord` | `int_execution_logs` | Integrations |
| `ConnectorHealthRecord` | `int_health_history` | Integrations |
| `ComplianceTrendRecord` | `gov_compliance_trends` | Governance |
| `FinOpsAggregateRecord` | `gov_finops_aggregates` | Governance |

### 5.3 Registo DI (`AddBuildingBlocksAnalytics`)

```csharp
// Em Program.cs ou equivalente:
services.AddBuildingBlocksAnalytics(configuration);
```

| Condição | Writer Registado |
|---------|-----------------|
| `Analytics:Enabled = true` | `ClickHouseAnalyticsWriter` (via HttpClient) |
| `Analytics:Enabled = false` (default) | `NullAnalyticsWriter` (graceful degradation) |

### 5.4 Configuração appsettings

```json
{
  "Analytics": {
    "Enabled": false,
    "ConnectionString": "http://clickhouse:8123/?database=nextraceone_analytics",
    "WriteTimeoutSeconds": 10,
    "MaxBatchSize": 500,
    "SuppressWriteErrors": true
  }
}
```

**NOTA:** `Enabled: false` é o padrão. Activar quando ClickHouse estiver disponível e validado.

---

## 6. Preparação de Ingestão Inicial

### 6.1 Fluxo de Ingestão Preparado

```
Módulos de Domínio (Application Layer)
        │
        ▼
IAnalyticsWriter.WriteXxxAsync(record)
        │
        ├─ [Enabled = false] → NullAnalyticsWriter → Task.CompletedTask
        │
        └─ [Enabled = true]  → ClickHouseAnalyticsWriter
                                      │
                                      ▼
                              POST http://clickhouse:8123/
                              ?query=INSERT INTO nextraceone_analytics.{table}
                              FORMAT JSONEachRow
                                      │
                                      ▼
                              ClickHouse (nextraceone_analytics)
```

### 6.2 Vectores de Ingestão Preparados por Módulo

| Módulo | Vector Preparado | Estado |
|--------|-----------------|--------|
| Product Analytics | Escrita directa via `IAnalyticsWriter.WriteProductEventAsync` | ✅ Pronto |
| Product Analytics (batch) | `WriteProductEventsBatchAsync` para ingestão em lote | ✅ Pronto |
| Operational Intelligence | `WriteRuntimeMetricAsync`, `WriteCostEntryAsync`, `WriteIncidentTrendEventAsync` | ✅ Pronto |
| Integrations | `WriteIntegrationExecutionAsync`, `WriteConnectorHealthEventAsync` | ✅ Pronto |
| Governance | `WriteComplianceTrendAsync`, `WriteFinOpsAggregateAsync` | ✅ Pronto |

### 6.3 O Que Falta para Ingestão Real

| Item | Status | Fase |
|------|--------|------|
| Chamadas a `IAnalyticsWriter` nos handlers de domínio | ❌ Pendente | E17 |
| Outbox processor → ClickHouse consumer | ❌ Pendente | E17 |
| Activar `Analytics:Enabled = true` em ambientes não-prod | ❌ Pendente | E17 |
| Validação de escrita real com dados de teste | ❌ Pendente | E17 |

---

## 7. Docker Compose — Inicialização Automática

### 7.1 Montagem de Ficheiros SQL

```yaml
clickhouse:
  volumes:
    - clickhouse-data:/var/lib/clickhouse
    - ./build/clickhouse/01-init-schema.sql:/docker-entrypoint-initdb.d/01-init-schema.sql:ro
    - ./build/clickhouse/02-analytics-schema.sql:/docker-entrypoint-initdb.d/02-analytics-schema.sql:ro
```

Ao iniciar o container ClickHouse:
1. `01-init-schema.sql` cria `nextraceone_obs` com tabelas OTEL
2. `02-analytics-schema.sql` cria `nextraceone_analytics` com tabelas analíticas de domínio

### 7.2 Ficheiros SQL

| Ficheiro | Base de Dados | Conteúdo |
|----------|--------------|---------|
| `build/clickhouse/init-schema.sql` | `nextraceone_obs` | OTEL logs, traces, metrics (existente) |
| `build/clickhouse/analytics-schema.sql` | `nextraceone_analytics` | Domain analytics — novo no E16 |

---

## 8. Validação da Separação PostgreSQL vs ClickHouse

### 8.1 Dados que Ficam no PostgreSQL (Estado Transacional)

| Módulo | Dados | Razão |
|--------|-------|-------|
| Todos | Entidades de domínio (users, tenants, services, contracts, etc.) | CRUD, ACID, FK constraints |
| Product Analytics | `pan_analytics_events` (buffer temporário) | Buffer de ingestão antes de flush |
| Product Analytics | `pan_definitions`, `pan_journey_steps` | Configuração transacional |
| Operational Intelligence | `ops_incidents`, `ops_mitigation_workflows` | State machines, ACID |
| Operational Intelligence | `ops_automation_workflows`, `ops_runbooks` | Workflows, referential integrity |
| Operational Intelligence | `ops_runtime_baselines`, `ops_cost_import_batches` | Configuração/estado de batch |
| Integrations | `int_connectors`, `int_ingestion_sources` | CRUD operacional, credenciais |
| Integrations | `int_ingestion_executions` (activas/recentes) | Estado activo, in-flight |
| Governance | `gov_compliance_policies`, `gov_risk_assessments` | Dados transacionais de governança |

### 8.2 Dados que Vão para ClickHouse (Analítico/Time-Series)

| Módulo | Dados | Tabela ClickHouse |
|--------|-------|------------------|
| Product Analytics | Todos os eventos de uso | `pan_events` |
| Operational Intelligence | Métricas de runtime históricas | `ops_runtime_metrics` |
| Operational Intelligence | Entradas de custo históricas | `ops_cost_entries` |
| Operational Intelligence | Tendências de incidentes | `ops_incident_trends` |
| Integrations | Execuções completadas | `int_execution_logs` |
| Integrations | Histórico de health | `int_health_history` |
| Governance | Scores de compliance ao longo do tempo | `gov_compliance_trends` |
| Governance | Agregações FinOps contextuais | `gov_finops_aggregates` |

### 8.3 Mistura Indevida Detectada e Corrigida

Nenhuma mistura indevida detectada no E16. O schema ClickHouse foi desenhado exclusivamente com dados de natureza analítica/time-series.

### 8.4 Nota sobre `pan_analytics_events` no PostgreSQL

A tabela `pan_analytics_events` no PostgreSQL (módulo Governance/ProductAnalytics, prefixo `pan_`) serve como **buffer de ingestão**. Não é duplicação — é parte do padrão de ingestão:

```
Application → pan_analytics_events (PostgreSQL buffer, 7 dias)
                     │
                     └─ IAnalyticsWriter → pan_events (ClickHouse permanente)
```

Esta separação mantém garantias transacionais na escrita e performance analítica na leitura.

---

## 9. Chaves de Correlação Adotadas

| Chave ClickHouse | Tabela PostgreSQL | Padrão de Join |
|-----------------|-------------------|----------------|
| `tenant_id` | `iam_tenants.Id` | Filtro obrigatório em todas as queries |
| `user_id` | `iam_users.Id` | Join via Application Layer para enrichment de nome |
| `service_id` / `service_name` | `cat_service_assets.Id` | Join via API para nome/dados do serviço |
| `environment_id` / `environment` | `env_environments.Id` | Join via API para nome/dados do ambiente |
| `connector_id` | `int_connectors.Id` | Join via API para detalhes do conector |
| `policy_id` | `gov_compliance_policies.Id` | Join via API para detalhes da política |
| `team_id` | `iam_teams.Id` | Join via API para nome da equipa |

**Princípio:** ClickHouse nunca faz JOIN com PostgreSQL directamente. Enrichment é feito na Application Layer via API calls separados.

---

## 10. Validação Técnica da Estrutura

### 10.1 Validação SQL

- SQL verificado manualmente contra documentação oficial ClickHouse 24.8
- `CREATE TABLE IF NOT EXISTS` — idempotente, safe para re-execução
- `CREATE MATERIALIZED VIEW IF NOT EXISTS` — idempotente
- Tipos de dados validados: UUID, LowCardinality(String), DateTime64(3, 'UTC'), Decimal64, UInt32, Int32, Bool
- TTL validado: sintaxe `occurred_at + INTERVAL N DAY/YEAR`
- MergeTree engines: `MergeTree`, `SummingMergeTree`, `AggregatingMergeTree`

### 10.2 Validação C# — Build

```
Build succeeded.
0 Errors — 31 Warnings (todos pre-existentes ou CA1848 matching pattern existente)
```

### 10.3 Validação de Testes

| Suite de Testes | Antes E16 | Após E16 | Estado |
|----------------|-----------|---------|--------|
| Identity Access Tests | 290 passed | 290 passed | ✅ Sem regressão |
| AI Knowledge Tests | 410 passed | 410 passed | ✅ Sem regressão |

---

## 11. Ficheiros Implementados no E16

| Ficheiro | Tipo | Descrição |
|----------|------|-----------|
| `build/clickhouse/analytics-schema.sql` | SQL DDL | Schema ClickHouse — nextraceone_analytics |
| `src/building-blocks/.../Analytics/Abstractions/IAnalyticsWriter.cs` | C# Interface | Porta de escrita analítica |
| `src/building-blocks/.../Analytics/Configuration/AnalyticsOptions.cs` | C# Config | Opções de configuração de analytics |
| `src/building-blocks/.../Analytics/Events/AnalyticsRecords.cs` | C# Records | 8 tipos de eventos analíticos |
| `src/building-blocks/.../Analytics/Writers/NullAnalyticsWriter.cs` | C# Writer | Graceful degradation |
| `src/building-blocks/.../Analytics/Writers/ClickHouseAnalyticsWriter.cs` | C# Writer | Implementação real via HTTP |
| `src/building-blocks/.../DependencyInjection.cs` | C# DI | Método `AddBuildingBlocksAnalytics` |
| `docker-compose.yml` | YAML | Montagem do analytics-schema.sql |

---

## 12. Desvios Corrigidos

| Desvio | Correcção |
|--------|-----------|
| `init-schema.sql` montado sem numeração explícita | Renomeado para `01-init-schema.sql`, novo como `02-analytics-schema.sql` — garante ordem de execução |
| AI & Knowledge estava no scope inicial como RECOMMENDED | Reclassificado como PREPARE_ONLY para E16 — schema definido mas tabelas comentadas até volume justificar |

---

## 13. Preparação para E17

O E16 entrega as pré-condições para o E17 (validação ponta a ponta):

| Item | Estado após E16 |
|------|-----------------|
| Schema ClickHouse para 4 módulos prioritários | ✅ Pronto |
| `IAnalyticsWriter` com DI configurável | ✅ Pronto |
| `NullAnalyticsWriter` para desenvolvimento sem ClickHouse | ✅ Pronto |
| `ClickHouseAnalyticsWriter` implementado | ✅ Pronto |
| Docker Compose inicializa ambas as DBs | ✅ Pronto |
| Chamadas `IAnalyticsWriter` nos handlers de domínio | ❌ Pendente E17 |
| Activação `Analytics:Enabled = true` em ambiente não-prod | ❌ Pendente E17 |
| Testes de integração ClickHouse | ❌ Pendente E17 |
| Validação de escrita real com dados | ❌ Pendente E17 |
| Outbox processor → ClickHouse consumer | ❌ Pendente E17 |
