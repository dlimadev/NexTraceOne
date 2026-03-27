# P6.1 — Reliability DbContext Expansion Report

**Data:** 2026-03-26
**Fase:** P6.1 — Expandir o ReliabilityDbContext para SLO, SLA, error budget e burn rate
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Expandir o subdomínio Reliability do módulo Operational Intelligence com modelo persistido real para SLO, SLA, error budget e burn rate, alinhando o módulo ao nível enterprise esperado pelo NexTraceOne.

---

## 2. Estado anterior do ReliabilityDbContext

Antes da P6.1, o `ReliabilityDbContext` continha apenas:

| DbSet | Tabela | Descrição |
|---|---|---|
| `ReliabilitySnapshots` | `ops_reliability_snapshots` | Snapshots computados de score de confiabilidade |

Não existia qualquer persistência para:
- definições de SLO
- definições de SLA
- error budget
- burn rate

O módulo calculava reliability scores em memória mas não dispunha de fonte de verdade para os objetivos que fundamentavam esses cálculos.

---

## 3. Ficheiros alterados

### 3.1 Novos ficheiros — Domínio

| Ficheiro | Descrição |
|---|---|
| `Domain/Reliability/Enums/SloType.cs` | Enum: Availability, Latency, ErrorRate, Throughput |
| `Domain/Reliability/Enums/SloStatus.cs` | Enum: Healthy, AtRisk, Violated |
| `Domain/Reliability/Enums/SlaStatus.cs` | Enum: Active, AtRisk, Breached |
| `Domain/Reliability/Enums/BurnRateWindow.cs` | Enum: OneHour, SixHours, TwentyFourHours, SevenDays |
| `Domain/Reliability/Entities/SloDefinition.cs` | Entidade principal de SLO com ID tipado |
| `Domain/Reliability/Entities/SlaDefinition.cs` | Entidade de SLA com FK para SLO |
| `Domain/Reliability/Entities/ErrorBudgetSnapshot.cs` | Snapshot de estado do error budget |
| `Domain/Reliability/Entities/BurnRateSnapshot.cs` | Snapshot de burn rate por janela de tempo |

### 3.2 Ficheiros alterados — Infraestrutura

| Ficheiro | Alteração |
|---|---|
| `Infrastructure/Reliability/Persistence/ReliabilityDbContext.cs` | Adicionados 4 novos DbSets: SloDefinitions, SlaDefinitions, ErrorBudgetSnapshots, BurnRateSnapshots |
| `Infrastructure/Reliability/DependencyInjection.cs` | Registados 4 novos repositórios no container DI |
| `Infrastructure/NexTraceOne.OperationalIntelligence.Infrastructure.csproj` | Adicionado `Microsoft.EntityFrameworkCore.Design` (necessário para migrations) |

### 3.3 Novos ficheiros — Configurações EF Core

| Ficheiro | Tabela criada |
|---|---|
| `Configurations/SloDefinitionConfiguration.cs` | `ops_slo_definitions` |
| `Configurations/SlaDefinitionConfiguration.cs` | `ops_sla_definitions` |
| `Configurations/ErrorBudgetSnapshotConfiguration.cs` | `ops_error_budget_snapshots` |
| `Configurations/BurnRateSnapshotConfiguration.cs` | `ops_burn_rate_snapshots` |

### 3.4 Novos ficheiros — Repositórios

| Ficheiro | Interface registada |
|---|---|
| `Repositories/SloDefinitionRepository.cs` | `ISloDefinitionRepository` |
| `Repositories/SlaDefinitionRepository.cs` | `ISlaDefinitionRepository` |
| `Repositories/ErrorBudgetSnapshotRepository.cs` | `IErrorBudgetSnapshotRepository` |
| `Repositories/BurnRateSnapshotRepository.cs` | `IBurnRateSnapshotRepository` |

### 3.5 Novos ficheiros — Application (Abstrações)

| Ficheiro | Descrição |
|---|---|
| `Abstractions/ISloDefinitionRepository.cs` | Contrato de repositório para SLO |
| `Abstractions/ISlaDefinitionRepository.cs` | Contrato de repositório para SLA |
| `Abstractions/IErrorBudgetSnapshotRepository.cs` | Contrato de repositório para error budget |
| `Abstractions/IBurnRateSnapshotRepository.cs` | Contrato de repositório para burn rate |

### 3.6 Novos ficheiros — Application (CQRS)

| Feature | Tipo | Descrição |
|---|---|---|
| `RegisterSloDefinition` | Command + Handler | Cria e persiste definição de SLO |
| `RegisterSlaDefinition` | Command + Handler | Cria e persiste definição de SLA, com lookup de SLO base |
| `GetErrorBudget` | Query + Handler | Consulta snapshot mais recente de error budget para um SLO |
| `GetBurnRate` | Query + Handler | Consulta snapshot mais recente de burn rate para um SLO e janela |

### 3.7 Ficheiros alterados — Application DI

| Ficheiro | Alteração |
|---|---|
| `Application/Reliability/DependencyInjection.cs` | Adicionados validators para os 4 novos handlers |

### 3.8 Ficheiro alterado — API

| Ficheiro | Alteração |
|---|---|
| `API/Reliability/Endpoints/ReliabilityEndpointModule.cs` | Adicionados 4 endpoints: POST /slos, POST /slas, GET /slos/{id}/error-budget, GET /slos/{id}/burn-rate |

### 3.9 Novos ficheiros — Migração EF Core

| Ficheiro | Descrição |
|---|---|
| `Migrations/20260326232107_P6_1_SloSlaBudgetBurnRate.cs` | Cria tabelas: ops_slo_definitions, ops_sla_definitions, ops_error_budget_snapshots, ops_burn_rate_snapshots |
| `Migrations/20260326232107_P6_1_SloSlaBudgetBurnRate.Designer.cs` | Ficheiro de designer gerado pelo EF Core |
| `Migrations/ReliabilityDbContextModelSnapshot.cs` | Snapshot do modelo atualizado |

### 3.10 Novos ficheiros — Testes

| Ficheiro | Descrição |
|---|---|
| `Tests/Reliability/Domain/Entities/SloSlaEntityTests.cs` | 14 testes unitários de domínio |
| `Tests/Reliability/Application/SloSlaHandlerTests.cs` | 13 testes unitários dos handlers |

---

## 4. Modelo de dados introduzido

### 4.1 SloDefinition (ops_slo_definitions)

| Coluna | Tipo | Descrição |
|---|---|---|
| Id | uuid | Identificador (strongly typed) |
| TenantId | uuid | Tenant |
| ServiceId | varchar(200) | Serviço alvo |
| Environment | varchar(100) | Ambiente |
| Name | varchar(200) | Nome descritivo |
| Description | varchar(1000) | Descrição opcional |
| Type | integer | SloType: Availability/Latency/ErrorRate/Throughput |
| TargetPercent | numeric(8,4) | Objetivo numérico (0–100) |
| AlertThresholdPercent | numeric(8,4) | Limiar de alerta (opcional) |
| WindowDays | integer | Janela de medição em dias |
| IsActive | boolean | SLO ativo |
| xmin | xid | Concorrência otimista |
| CreatedAt/By, UpdatedAt/By, IsDeleted | — | Auditoria via base |

### 4.2 SlaDefinition (ops_sla_definitions)

Contém FK para `ops_slo_definitions`. Persiste acordo contratual com estado, vigência e notas de penalidade.

### 4.3 ErrorBudgetSnapshot (ops_error_budget_snapshots)

Contém FK para `ops_slo_definitions`. Persiste total/consumed/remaining em minutos e percentagem consumida. Status calculado automaticamente:
- `< 80%` consumido → Healthy
- `80–100%` → AtRisk
- `100%+` → Violated

### 4.4 BurnRateSnapshot (ops_burn_rate_snapshots)

Contém FK para `ops_slo_definitions`. Persiste burn rate calculado (observedErrorRate / toleratedErrorRate) por janela. Limiares heurísticos (Google SRE):
- `>= 14.4` → Violated
- `>= 6` → AtRisk
- `< 6` → Healthy

---

## 5. Endpoints expostos

| Método | Endpoint | Permissão |
|---|---|---|
| POST | `/api/v1/reliability/slos` | `operations:reliability:write` |
| POST | `/api/v1/reliability/slas` | `operations:reliability:write` |
| GET | `/api/v1/reliability/slos/{sloId}/error-budget` | `operations:reliability:read` |
| GET | `/api/v1/reliability/slos/{sloId}/burn-rate?window=...` | `operations:reliability:read` |

---

## 6. Validação

| Critério | Estado |
|---|---|
| Compilação sem erros | ✅ Build succeeded |
| Testes existentes mantidos | ✅ 323 → 350 testes (0 falhas) |
| Novos testes adicionados | ✅ 27 novos testes |
| Migração EF Core gerada | ✅ 20260326232107_P6_1_SloSlaBudgetBurnRate |
| Tabelas criadas com relações corretas | ✅ 4 tabelas com FKs e índices |
| Wiring DI completo | ✅ Repositórios e validators registados |
| API endpoints expostos | ✅ 4 endpoints |

---

## 7. Coerência com padrões da plataforma

- Entidades herdam de `AuditableEntity<TId>` — auditoria e soft-delete automáticos via base
- IDs fortemente tipados: `SloDefinitionId`, `SlaDefinitionId`, `ErrorBudgetSnapshotId`, `BurnRateSnapshotId`
- `RowVersion` (PostgreSQL xmin) em todas as entidades — concorrência otimista
- `CancellationToken` em todos os métodos async
- Guard clauses no início dos factory methods
- Configurações EF Core separadas por entidade (`IEntityTypeConfiguration<T>`)
- Repositórios `internal sealed` — bounded context isolado
