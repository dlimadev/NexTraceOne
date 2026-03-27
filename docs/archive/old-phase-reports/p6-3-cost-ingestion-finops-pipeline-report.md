# P6.3 — Pipeline de Ingestão de Custo / FinOps por Serviço e Ambiente

**Data:** 2026-03-27
**Fase:** P6.3 — Implementar pipeline real de ingestão de custo / FinOps
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Evoluir o subdomínio Cost do módulo Operational Intelligence de um estado onde os handlers e entidades já existiam mas algumas partes estavam quebradas ou incompletas, para um estado com **pipeline real de ingestão, persistência e consulta de custo** por serviço e ambiente.

---

## 2. Estado anterior ao P6.3

| Capacidade | Estado |
|---|---|
| Entidades de custo (CostSnapshot, CostAttribution, CostTrend, ServiceCostProfile, CostImportBatch, CostRecord) | ✅ Presente |
| `ImportCostBatch` handler — importação de batch com CostRecords | ✅ Funcional |
| `IngestCostSnapshot` handler — ingestão de snapshot por serviço/ambiente | ✅ Funcional |
| `AttributeCostToService` handler | ✅ Funcional |
| `GetCostReport` handler | ✅ Funcional |
| `GetCostByRelease`, `GetCostByRoute`, `GetCostDelta` handlers | ✅ Funcional |
| `AlertCostAnomaly` handler | ✅ Funcional |
| `ComputeCostTrend` handler | ❌ **Bug: CostTrend criado mas não persistido** (sem ICostTrendRepository) |
| `ICostTrendRepository` | ❌ Ausente |
| `ICostImportBatchRepository.ListAsync` | ❌ Ausente |
| `ListCostImportBatches` query | ❌ Ausente |
| `GetCostRecordsByService` query | ❌ Ausente |
| `CreateServiceCostProfile` command | ❌ Ausente |

---

## 3. Ficheiros alterados

### 3.1 Novos ficheiros — Application (Abstractions)

| Ficheiro | Descrição |
|---|---|
| `Application/Cost/Abstractions/ICostTrendRepository.cs` | Interface com `GetByIdAsync`, `ListByServiceAsync`, `Add` |

### 3.2 Novos ficheiros — Infrastructure (Repository)

| Ficheiro | Descrição |
|---|---|
| `Infrastructure/Cost/Persistence/Repositories/CostTrendRepository.cs` | Implementação de `ICostTrendRepository` com EF Core (filtro por serviceName+environment, ordenação por PeriodStart desc) |

### 3.3 Novos ficheiros — Application (CQRS Handlers)

| Feature | Tipo | Descrição |
|---|---|---|
| `ListCostImportBatches` | Query + Handler | Lista batches de importação paginados por data desc |
| `GetCostRecordsByService` | Query + Handler | Lista CostRecords por serviceId+período, com total aggregado |
| `CreateServiceCostProfile` | Command + Handler | Cria ou devolve idempotentemente o ServiceCostProfile de um serviço |

### 3.4 Ficheiros alterados — Bug fix ComputeCostTrend

| Ficheiro | Alteração |
|---|---|
| `Application/Cost/Features/ComputeCostTrend/ComputeCostTrend.cs` | Adicionado `ICostTrendRepository` ao handler; `trendRepository.Add(trend)` chamado antes de `CommitAsync` |

### 3.5 Ficheiros alterados — ICostImportBatchRepository

| Ficheiro | Alteração |
|---|---|
| `Application/Cost/Abstractions/ICostImportBatchRepository.cs` | Adicionado método `ListAsync(page, pageSize, ct)` |
| `Infrastructure/Cost/Persistence/Repositories/CostImportBatchRepository.cs` | Implementação de `ListAsync` com ordenação por `ImportedAt desc` |

### 3.6 Ficheiros alterados — Application DI

| Ficheiro | Alteração |
|---|---|
| `Application/Cost/DependencyInjection.cs` | Adicionados validators para `CreateServiceCostProfile`, `ListCostImportBatches`, `GetCostRecordsByService` |

### 3.7 Ficheiros alterados — Infrastructure DI

| Ficheiro | Alteração |
|---|---|
| `Infrastructure/Cost/DependencyInjection.cs` | Adicionado `services.AddScoped<ICostTrendRepository, CostTrendRepository>()` |

### 3.8 Ficheiros alterados — API

| Ficheiro | Alteração |
|---|---|
| `API/Cost/Endpoints/Endpoints/CostIntelligenceEndpointModule.cs` | Adicionados 4 novos endpoints (ver secção 5) |

### 3.9 Novos ficheiros — Testes

| Ficheiro | Descrição |
|---|---|
| `Tests/Cost/Application/CostPipelineHandlerTests.cs` | 10 testes unitários dos handlers P6.3 (ComputeCostTrend persistence, CreateServiceCostProfile, ListCostImportBatches, GetCostRecordsByService) |

---

## 4. Modelo de ingestão adoptado

### 4.1 Pipeline de ingestão via batch (CostImportBatch + CostRecords)

```
POST /api/v1/cost/import
  1. Validate deduplication (Source + Period must be unique)
  2. CostImportBatch.Create(source, period, now, currency)
  3. For each record:
     CostRecord.Create(batchId, serviceId, serviceName, team, domain, environment, period, totalCost, ...)
  4. batch.Complete(recordCount)
  5. Persist (CostImportBatch + CostRecords) via UnitOfWork
  6. Return BatchId + status
```

### 4.2 Pipeline de ingestão via snapshot pontual

```
POST /api/v1/cost/snapshots
  1. CostSnapshot.Create(serviceName, environment, totalCost, cpuShare, memoryShare, ...)
  2. Persist via ICostSnapshotRepository
  3. Return SnapshotId
```

### 4.3 Correlação por serviço e ambiente

Todos os registos de custo incluem `ServiceId`, `ServiceName`, `Environment` e `Period`.

Consultas disponíveis pós-P6.3:
- `GET /api/v1/cost/records?serviceId=svc-api&period=2026-03` → CostRecords por serviço/período
- `GET /api/v1/cost/report?serviceName=...&environment=...` → CostSnapshots por serviço/ambiente
- `GET /api/v1/cost/by-route?serviceName=...&environment=...` → CostAttributions por serviço/ambiente

### 4.4 Perfil de custo do serviço

```
POST /api/v1/cost/profiles   { serviceName, environment, alertThresholdPercent, monthlyBudget }
  → Cria ServiceCostProfile (idempotente: devolve existente se já criado)
  → Necessário para AlertCostAnomaly funcionar

POST /api/v1/cost/anomaly-check  { serviceName, environment, currentCost }
  → Carrega ServiceCostProfile
  → Atualiza currentMonthCost
  → Se currentCost >= threshold do orçamento → publica CostAnomalyDetectedEvent
```

---

## 5. Novos endpoints API

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/v1/cost/import` | `operations:cost:read` | Lista batches de importação com paginação |
| GET | `/api/v1/cost/records` | `operations:cost:read` | Lista CostRecords por serviceId e período opcional |
| POST | `/api/v1/cost/profiles` | `operations:cost:write` | Cria (ou devolve idempotentemente) ServiceCostProfile |
| GET | `/api/v1/cost/profiles` | `operations:cost:read` | Obtém o ServiceCostProfile de um serviço/ambiente |

---

## 6. Validação

| Critério | Estado |
|---|---|
| Compilação sem erros | ✅ Build succeeded |
| Testes pré-P6.3 mantidos | ✅ 387 → 397 testes (0 falhas) |
| Novos testes adicionados | ✅ 10 novos testes |
| Bug ComputeCostTrend corrigido | ✅ Trend persistido via ICostTrendRepository |
| ICostTrendRepository registado no DI | ✅ Scoped |
| ListAsync em ICostImportBatchRepository | ✅ Implementado |
| Endpoints funcionais com permissão | ✅ 4 novos endpoints |
| Idempotência em CreateServiceCostProfile | ✅ Devolve existente sem erro |
| Coerência com padrões da plataforma | ✅ Result<T>, CancellationToken, strongly typed IDs, FluentValidation |
