# E10 — Integrations Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Integrations (atualmente alojado no módulo Governance
pendente extração OI-02). Adicionada concorrência otimista, renomeação de prefixo de tabelas,
check constraints em 6 colunas de enum, e permissão `integrations:write` para TechLead.

---

## Ficheiros de Código Alterados

### Domain — Entidades (2 ficheiros)
| Ficheiro | Alteração |
|----------|-----------|
| `IntegrationConnector.cs` | Adicionado RowVersion (uint xmin). |
| `IngestionSource.cs` | Adicionado RowVersion (uint xmin). |

### Persistence — EF Core Configurations (3 ficheiros)
| Ficheiro | Alteração |
|----------|-----------|
| `IntegrationConnectorConfiguration.cs` | Tabela gov_integration_connectors → int_connectors. 2 check constraints (Status, Health). IsRowVersion(). |
| `IngestionSourceConfiguration.cs` | Tabela gov_ingestion_sources → int_ingestion_sources. 3 check constraints (Status, FreshnessStatus, TrustLevel). IsRowVersion(). |
| `IngestionExecutionConfiguration.cs` | Tabela gov_ingestion_executions → int_ingestion_executions. 1 check constraint (Result). |

### Security — Permissions (1 ficheiro)
| Ficheiro | Alteração |
|----------|-----------|
| `RolePermissionCatalog.cs` | `integrations:write` registado para TechLead. |

---

## Correções por Parte

### PART 1 — Fronteira Governance vs Integrations
- ✅ Tabelas renomeadas de `gov_*` para `int_*`, separando visualmente o ownership
- ⏳ Extração física para módulo próprio depende de OI-02 (Wave 0)

### PART 2 — Conectores, Status, Retries e Health
- ✅ Check constraints guardam estados válidos de ConnectorStatus e ConnectorHealth
- ✅ Check constraint para ExecutionResult (inclui Running, Success, PartialSuccess, Failed, Cancelled, TimedOut)
- ✅ RowVersion para controlo de concorrência em IntegrationConnector

### PART 3 — Domínio
- ✅ RowVersion (uint) em 2 entidades mutáveis (IntegrationConnector, IngestionSource)
- ✅ IngestionExecution mantida sem RowVersion (imutável após criação/completação)

### PART 4 — Persistência
- ✅ 3 tabelas renomeadas: int_connectors, int_ingestion_sources, int_ingestion_executions
- ✅ 6 check constraints em 6 colunas de enum
- ✅ `IsRowVersion()` xmin em 2 entidades mutáveis

### PART 5 — PostgreSQL vs ClickHouse
- ✅ Todas as entidades permanecem em PostgreSQL (transacional)
- ⏳ Pipeline analítico ClickHouse para fase futura

### PART 6 — Backend
- ✅ 7 endpoints verificados ativos no IntegrationHubEndpointModule
- ✅ Permissões granulares: integrations:read e integrations:write

### PART 7 — Frontend
- ✅ 4 páginas verificadas: IntegrationHubPage, ConnectorDetailPage, IngestionFreshnessPage, IngestionExecutionsPage

### PART 8 — Segurança
- ✅ `integrations:write` registado para TechLead (antes apenas PlatformAdmin)
- ✅ 3 roles com integrations:read (PlatformAdmin, TechLead, Developer)
- ✅ 2 roles com integrations:write (PlatformAdmin, TechLead)

### PART 9 — Dependências
- ✅ Módulo usa GovernanceDbContext temporariamente (OI-02 pendente)

### PART 10 — Documentação
- ✅ Execution report e gap report criados

---

## Validação

- ✅ Build: 0 erros
- ✅ 163 testes Governance: todos passam
- ✅ 290 testes Identity: todos passam (após alteração RolePermissionCatalog)

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `IntegrationConnector` | RowVersion (uint xmin) |
| `IngestionSource` | RowVersion (uint xmin) |
| 3 EF Configurations | Check constraints + table rename + IsRowVersion() |
| `RolePermissionCatalog` | integrations:write para TechLead |
