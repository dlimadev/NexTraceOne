# Sort/filter por maturidade no catálogo (backend + frontend) — Design

**Data:** 2026-07-18
**Contexto:** Ciclo 41. Completa o pedido "Score & Maturidade → filtro do catálogo" a sério (o ciclo 40 entregou só a coluna). Cross-stack (.NET + React).

---

## Motivação e restrição

A maturidade é **computada em C#** (não é coluna SQL): por serviço, deriva 6 sinais (ownership, contracts, docs, runbook, monitoring, repo) → média → `overallScore` → `level` (`GetServiceMaturityDashboard`). Logo, filtrar/ordenar a lista paginada por maturidade exige **computar-depois-paginar em memória** quando (e só quando) o sort/filter por maturidade está ativo. O caminho normal (paginação SQL) mantém-se inalterado.

**Decisão do utilizador:** evitar N+1 → gather em **batch** (poucas queries) em vez de por-serviço.

---

## Backend

### 1. Métodos de repositório batch

- `IServiceLinkRepository.ListByServiceIdsAsync(IReadOnlyCollection<ServiceAssetId> serviceAssetIds, CancellationToken) → IReadOnlyList<ServiceLink>` — EF `WHERE ServiceAssetId IN (ids)`. Espelhar o `ListByServiceAsync` existente.
- `IApiAssetRepository.ListByServiceIdsAsync(IReadOnlyCollection<ServiceAssetId> serviceIds, CancellationToken) → IReadOnlyList<ApiAsset>` — EF `WHERE OwnerService.Id IN (ids)` (ver o `ListByServiceIdAsync` existente para o caminho de navegação/FK correto).
- `IContractVersionRepository.ListByApiAssetIdsAsync(apiIds)` — **já é batch**, reusar.

### 2. Serviço partilhado de maturidade (DRY + de-N+1)

Novo `ServiceMaturityCalculator` (Application, `Graph/Maturity/`), registado no DI:
- **Scoring puro** `Compute(ServiceAsset service, IReadOnlyList<ServiceLink> links, IReadOnlyList<ApiAsset> apis, int contractCount) → ServiceMaturityResult` — mover **verbatim** a lógica de sinais + `dimensionScores` + `ScoreToLevel` das linhas 72-93/151-156 de `GetServiceMaturityDashboard`. `ServiceMaturityResult` = `(string Level, decimal OverallScore, bool HasOwnership, bool HasContracts, bool HasDocumentation, bool HasRunbook, bool HasMonitoring, bool HasRepository, int ApiCount, int ContractCount, int LinkCount)`.
- **Orquestrador batch** `ComputeForServicesAsync(IReadOnlyList<ServiceAsset> services, CancellationToken) → IReadOnlyDictionary<Guid, ServiceMaturityResult>` — 3 queries batch (links por serviceIds, apis por serviceIds, contracts por todos os apiIds), agrupa por serviço em memória, chama `Compute` por serviço. **Sem N+1.**

### 3. Refactor `GetServiceMaturityDashboard`

- Passa a usar `ComputeForServicesAsync` (remove o loop N+1). Mapeia `ServiceMaturityResult` → `ServiceMaturityItemDto` (mesmos campos/valores). **Comportamento preservado** — os scores/níveis são idênticos.

### 4. Estender `ListServices`

- `Query` ganha: `string? MaturityLevel` (filtro; um de Initial/Developing/Defined/Managed/Optimizing), `string? SortBy` ("name" default | "maturity"), `bool SortDescending = false`.
- Validador: `MaturityLevel` ∈ níveis válidos quando não-nulo; `SortBy` ∈ {name, maturity} quando não-nulo.
- Handler:
  - **Caminho rápido (inalterado):** `MaturityLevel is null && SortBy != "maturity"` → `ListFilteredAsync` paginado no SQL como hoje.
  - **Caminho de maturidade:** buscar TODOS os serviços que batem os outros filtros (`ListFilteredAsync` com `page:1, pageSize:10_000`), `ComputeForServicesAsync`, filtrar por `MaturityLevel` se dado, ordenar por `OverallScore` (asc/desc), depois paginar em memória (`Skip((page-1)*pageSize).Take(pageSize)`). `TotalCount` = nº após o filtro de maturidade.
- **`ServiceListItem` DTO inalterado** (sem campos de maturidade): a coluna do frontend continua a mostrar maturidade via o dashboard (join, ciclo 40); o backend só decide *quais* linhas e *que ordem*. Consistente (mesmo calculator).

### 5. Endpoint

- `ServiceCatalogEndpointModule.cs`: o endpoint de listagem aceita `maturityLevel`, `sortBy`, `sortDescending` como query params (`[AsParameters]` ou binding manual) e passa à `ListServices.Query`.

---

## Frontend

### 6. `ServiceCatalogListPage` — filtro + ordenação por maturidade

- `serviceCatalogApi.listServices` params ganham `maturityLevel?`, `sortBy?`, `sortDescending?`.
- Filtro **"Maturidade"** (dropdown de níveis: Todos + 5 níveis) na barra de filtros, junto aos existentes; entra na `queryKey` e nos params.
- Coluna **"Maturidade"** com cabeçalho **clicável** → alterna `sortBy='maturity'` asc↔desc (ícone de direção); reset para 'name' opcional. A coluna de badges (ciclo 40) mantém-se (via dashboard join).
- Reset de página ao mudar filtro/sort (como os outros filtros).

---

## i18n (4 locales)

- `catalog.filters.maturity` — EN "Maturity" · ES "Madurez" · pt "Maturidade" (reusar `serviceMaturity.level.*` nas opções do dropdown).
- (sort é por ícone; sem string nova além do `aria-label` — `catalog.columns.sortByMaturity`: EN "Sort by maturity").

---

## Testes

**Backend (xUnit + FluentAssertions + NSubstitute):**
- `ServiceMaturityCalculator.Compute` — casos: serviço completo → Optimizing/Managed; vazio → Initial; sem apis → o ramo `apis.Count==0`.
- `ComputeForServicesAsync` — com repos substituídos devolve maturidade por serviceId, e faz **1 chamada batch** a cada repo (não N chamadas).
- `ListServices.Handler` — filtro por nível devolve só os serviços desse nível; sort por maturidade ordena por score (asc/desc); paginação em memória correta; caminho rápido não invoca o calculator.
- `GetServiceMaturityDashboard` — output inalterado após o refactor (mesmos níveis/scores).

**Frontend (Vitest):**
- `ServiceCatalogListPage` — selecionar nível no dropdown envia `maturityLevel`; clicar no cabeçalho Maturidade envia `sortBy=maturity` e alterna `sortDescending`.

**Gates:** backend `dotnet build` + testes do módulo catalog; frontend tsc/eslint/build/suite/validate:i18n. Stub (frontend) para o filtro/sort visual.

---

## Fora de âmbito

- Materializar maturidade (coluna computada/tabela + job) — o compute-then-page em memória é suficiente e evita N+1 via batch.
- Unificar os OUTROS cálculos de maturidade (`ComputeServiceMaturity` dimensional, `GetServiceMaturityScoreV2`, `GetOwnershipAudit`) — mantêm-se; só o dashboard partilha o calculator novo. Follow-up.
- Adicionar maturidade ao `ServiceListItem` DTO — desnecessário (frontend usa o join do dashboard para display).
