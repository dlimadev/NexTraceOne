# P6.4 — Correlação Contextual de Custo: Equipa, Domínio e Mudança

**Data:** 2026-03-27
**Fase:** P6.4 — Ligar custo a equipa, domínio e mudança
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Complementar o pipeline de ingestão de custo (P6.3) com **correlação contextual real**, ligando `CostRecord` a equipa, domínio e release/mudança. O resultado é que o produto passa a suportar FinOps contextual verdadeiro — onde o custo não é apenas um número por serviço, mas um dado rastreável até à equipa responsável, ao domínio de negócio e à mudança que o originou.

---

## 2. Estado anterior ao P6.4 (pós-P6.3)

| Capacidade | Estado |
|---|---|
| `CostRecord.Team` + `CostRecord.Domain` (campos string) | ✅ Existentes |
| `CostRecordRepository.ListByTeamAsync` / `ListByDomainAsync` | ✅ Existentes |
| `GetCostRecordsByTeam` query handler | ❌ Ausente |
| `GetCostRecordsByDomain` query handler | ❌ Ausente |
| `CostRecord.ReleaseId` (correlação com mudança) | ❌ Ausente |
| `CostRecord.AssignRelease()` / `ClearRelease()` | ❌ Ausente |
| `GetCostRecordsByRelease` query handler | ❌ Ausente |
| `EnrichCostRecordWithRelease` command handler | ❌ Ausente |
| EF migration para `ReleaseId` | ❌ Ausente |

---

## 3. Ficheiros alterados

### 3.1 Domínio — CostRecord

| Ficheiro | Alteração |
|---|---|
| `Domain/Cost/Entities/CostRecord.cs` | Adicionado `ReleaseId?` (nullable Guid) + `AssignRelease(Guid)` + `ClearRelease()` |
| `Domain/Cost/Errors/CostIntelligenceErrors.cs` | Adicionados `NoRecordsForRelease(Guid)` + `RecordNotFound(string)` |

### 3.2 Infraestrutura — EF Core

| Ficheiro | Alteração |
|---|---|
| `Infrastructure/Cost/Persistence/Configurations/CostRecordConfiguration.cs` | Adicionados `Property(x => x.ReleaseId)` + `HasIndex(x => x.ReleaseId)` |
| `Infrastructure/Cost/Persistence/Migrations/20260327072730_P6_4_CostRecord_ReleaseId.cs` | Nova migration: `ADD COLUMN release_id uuid NULLABLE` + índice em `ops_cost_records` |
| `Infrastructure/Cost/Persistence/Repositories/CostRecordRepository.cs` | Implementação de `ListByReleaseAsync(Guid)` |

### 3.3 Application — Repositório

| Ficheiro | Alteração |
|---|---|
| `Application/Cost/Abstractions/ICostRecordRepository.cs` | Adicionado `ListByReleaseAsync(Guid releaseId, CancellationToken)` |

### 3.4 Application — Novos handlers

| Feature | Tipo | Descrição |
|---|---|---|
| `GetCostRecordsByTeam` | Query + Handler | Lista CostRecords por `Team` + período opcional; agrega total de custo |
| `GetCostRecordsByDomain` | Query + Handler | Lista CostRecords por `Domain` + período opcional; agrega total de custo |
| `GetCostRecordsByRelease` | Query + Handler | Lista CostRecords com `ReleaseId` correspondente; retorna NotFound se vazio |
| `EnrichCostRecordWithRelease` | Command + Handler | Associa `ReleaseId` a todos os CostRecords de um serviço+ambiente+período |

### 3.5 Application — DI

| Ficheiro | Alteração |
|---|---|
| `Application/Cost/DependencyInjection.cs` | Adicionados 4 validators P6.4 |

### 3.6 API

| Ficheiro | Alteração |
|---|---|
| `API/Cost/Endpoints/CostIntelligenceEndpointModule.cs` | Adicionados 4 novos endpoints (ver secção 5) |

### 3.7 Testes

| Ficheiro | Descrição |
|---|---|
| `Tests/Cost/Application/CostContextCorrelationHandlerTests.cs` | 13 novos testes unitários |

---

## 4. Modelo de correlação contextual adoptado

### 4.1 Correlação com equipa e domínio

`CostRecord` já continha `Team` e `Domain` como campos string desde a criação do modelo. A correlação acontece **na ingestão**: ao chamar `POST /api/v1/cost/import`, cada `CostRecordInput` inclui `Team` e `Domain` opcionais.

O P6.4 acrescenta os **query handlers** que filtram e agregam por estes campos, tornando-os consultáveis via API.

```
GET /api/v1/cost/records/by-team?team=team-platform&period=2026-03
GET /api/v1/cost/records/by-domain?domain=commerce&period=2026-03
```

### 4.2 Correlação com release/mudança

Para ligar custo a uma release (Change Governance), foi adicionado `ReleaseId?` a `CostRecord`:

**Estratégia temporal**: após uma release ser criada no módulo Change Governance, o operador/sistema pode chamar:

```
POST /api/v1/cost/records/enrich-release
{
  "releaseId": "...",
  "serviceId": "svc-api",
  "environment": "production",
  "period": "2026-03"
}
```

Todos os `CostRecord` que correspondam ao `serviceId + environment + period` têm o `ReleaseId` atribuído automaticamente. A associação fica persistida na base de dados.

Consulta posterior:
```
GET /api/v1/cost/records/by-release/{releaseId}
```

---

## 5. Novos endpoints API

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/v1/cost/records/by-team` | `operations:cost:read` | Custo por equipa + período opcional |
| GET | `/api/v1/cost/records/by-domain` | `operations:cost:read` | Custo por domínio + período opcional |
| GET | `/api/v1/cost/records/by-release/{releaseId}` | `operations:cost:read` | CostRecords ligados a uma release |
| POST | `/api/v1/cost/records/enrich-release` | `operations:cost:write` | Associa ReleaseId a CostRecords por serviceId+env+período |

---

## 6. Ligação com entidades de outros módulos

| Módulo | Entidade | Ligação |
|---|---|---|
| Change Governance | `Release` | `CostRecord.ReleaseId` → `Release.Id` (correlação por GUID, sem FK cross-module) |
| Catalog | `ServiceAsset` | Correlação via `CostRecord.ServiceId` (string) — sem FK cross-module |
| Organization | `Team` | Correlação via `CostRecord.Team` (string) — sem FK cross-module |
| Organization | `GovernanceDomain` | Correlação via `CostRecord.Domain` (string) — sem FK cross-module |

**Regra de isolamento respeitada**: não há FKs cross-module. A correlação é feita por valores identificadores (GUID/string), preservando o isolamento de bounded contexts.

---

## 7. Esquema de base de dados

Nova migration: `20260327072730_P6_4_CostRecord_ReleaseId`

```sql
-- Up
ALTER TABLE ops_cost_records ADD COLUMN "ReleaseId" uuid NULL;
CREATE INDEX "IX_ops_cost_records_ReleaseId" ON ops_cost_records ("ReleaseId");

-- Down
DROP INDEX "IX_ops_cost_records_ReleaseId";
ALTER TABLE ops_cost_records DROP COLUMN "ReleaseId";
```

---

## 8. Validação

| Critério | Estado |
|---|---|
| Compilação sem erros | ✅ Build succeeded |
| Testes pré-P6.4 mantidos | ✅ 397 → 410 (0 falhas) |
| Novos testes | ✅ 13 novos testes |
| `AssignRelease`/`ClearRelease` domain logic | ✅ Testado |
| `GetCostRecordsByTeam` inclui ReleaseId no output | ✅ Testado |
| `EnrichCostRecordWithRelease` filtra por environment | ✅ Testado |
| `GetCostRecordsByRelease` retorna NotFound quando vazio | ✅ Testado |
| Migration gerada e correcta | ✅ Verificado |
