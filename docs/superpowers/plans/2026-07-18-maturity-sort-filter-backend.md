# Sort/filter por maturidade (backend + frontend, batch) — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps usam checkbox (`- [ ]`).

**Goal:** Permitir filtrar e ordenar o Catálogo de Serviços por maturidade, com o cálculo feito no backend em batch (sem N+1), e o controlo no frontend.

**Architecture:** Extrair o cálculo de maturidade do dashboard para um serviço partilhado com gather em batch; a lista de serviços usa-o num caminho "computar-depois-paginar em memória" quando há filtro/sort por maturidade (caminho rápido SQL inalterado caso contrário); frontend ganha dropdown de filtro + coluna ordenável.

**Tech Stack:** .NET 10 (CQRS/MediatR, EF Core, xUnit+FluentAssertions+NSubstitute); React 19/TS/TanStack Query/Vitest.

## Global Constraints

- Backend: `Result<T>` (sem exceções p/ negócio); DTOs em Contracts/Application; XML-docs em PT, identificadores EN; `TreatWarningsAsErrors`; testes em `tests/modules/catalog/NexTraceOne.Catalog.Tests`.
- Frontend: UI só via i18n (4 locales, chaves em todos); testes Vitest só em `src/frontend/src/__tests__/**`; TS strict.
- **O scoring a partilhar é o do `GetServiceMaturityDashboard`** (6 sinais → média → `ScoreToLevel`), NÃO o `ComputeServiceMaturity` dimensional.
- Caminho rápido da lista (sem maturidade) deve permanecer byte-idêntico em comportamento e não invocar o calculator.
- Comando de build backend do módulo: `dotnet build src/modules/catalog/NexTraceOne.Catalog.Application/NexTraceOne.Catalog.Application.csproj` (e Infrastructure/API conforme a task). Testes: `dotnet test tests/modules/catalog/NexTraceOne.Catalog.Tests/NexTraceOne.Catalog.Tests.csproj --filter "FullyQualifiedName!~E2E"`.

---

### Task 1: Batch repos + ServiceMaturityCalculator + refactor do dashboard

**Files:**
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Abstractions/IServiceLinkRepository.cs`
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Abstractions/IApiAssetRepository.cs`
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Repositories/ServiceLinkRepository.cs`
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Repositories/ApiAssetRepository.cs`
- Create: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Maturity/ServiceMaturityCalculator.cs`
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/DependencyInjection.cs` (registar o calculator)
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Features/GetServiceMaturityDashboard/GetServiceMaturityDashboard.cs`
- Test: `tests/modules/catalog/NexTraceOne.Catalog.Tests/Graph/Maturity/ServiceMaturityCalculatorTests.cs`

**Interfaces (Produces):**
- `IServiceLinkRepository.ListByServiceIdsAsync(IReadOnlyCollection<ServiceAssetId> serviceAssetIds, CancellationToken) : Task<IReadOnlyList<ServiceLink>>`
- `IApiAssetRepository.ListByServiceIdsAsync(IReadOnlyCollection<ServiceAssetId> serviceIds, CancellationToken) : Task<IReadOnlyList<ApiAsset>>`
- `ServiceMaturityCalculator` (registado como scoped) com:
  - `ServiceMaturityResult Compute(ServiceAsset service, IReadOnlyList<ServiceLink> links, IReadOnlyList<ApiAsset> apis, int contractCount)`
  - `Task<IReadOnlyDictionary<Guid, ServiceMaturityResult>> ComputeForServicesAsync(IReadOnlyList<ServiceAsset> services, CancellationToken)`
- `public sealed record ServiceMaturityResult(string Level, decimal OverallScore, bool HasOwnership, bool HasContracts, bool HasDocumentation, bool HasRunbook, bool HasMonitoring, bool HasRepository, int ApiCount, int ContractCount, int LinkCount)`

- [ ] **Step 1: Teste do calculator (falha)**

Criar `ServiceMaturityCalculatorTests.cs`. Construir `ServiceAsset` via os factory/métodos de domínio existentes (ver como outros testes do módulo criam `ServiceAsset` — procurar em `tests/modules/catalog`). Testar `Compute` (função pura, sem repos):
- serviço com team+owner, doc link, runbook link, monitoring link, repo url, e `contractCount=1`, `apis` não-vazio → `Level == "Optimizing"` ou `"Managed"` e `OverallScore >= 0.7m`.
- serviço vazio (sem nada), `apis` vazio, `contractCount=0` → `Level == "Initial"`.
Instanciar `new ServiceMaturityCalculator(linkRepo, apiRepo, contractRepo)` com substitutos NSubstitute (não usados no `Compute`).

- [ ] **Step 2: Correr — falha** (`dotnet test ... --filter "FullyQualifiedName~ServiceMaturityCalculator"`) → FAIL (tipo inexistente).

- [ ] **Step 3: Criar `ServiceMaturityCalculator`**

Mover a lógica de `GetServiceMaturityDashboard` (linhas 72-93 = sinais + dimensionScores + overallScore; 151-156 = `ScoreToLevel`) para `Compute(...)`. O `ComputeForServicesAsync`:
1. `var ids = services.Select(s => s.Id).ToList();`
2. `links = await linkRepo.ListByServiceIdsAsync(ids, ct)` → agrupar `linksByService = links.GroupBy(l => l.ServiceAssetId).ToDictionary(...)` (usar a propriedade FK de `ServiceLink`; ver `ListByServiceAsync`).
3. `apis = await apiRepo.ListByServiceIdsAsync(ids, ct)` → `apisByService` agrupado por serviço (ver o caminho de owner em `ListByServiceIdAsync`).
4. `allApiIds = apis.Select(a => a.Id.Value)`; `contracts = await contractRepo.ListByApiAssetIdsAsync(allApiIds, ct)`; contar por serviço via o mapa api→serviço.
5. Para cada serviço: `Compute(service, linksByService.GetValueOrDefault(id, []), apisByService.GetValueOrDefault(id, []), contractCountByService)`.
6. Devolver `Dictionary<Guid, ServiceMaturityResult>` (chave `service.Id.Value`).
Construtor injeta `IServiceLinkRepository`, `IApiAssetRepository`, `IContractVersionRepository`. XML-docs PT.

- [ ] **Step 4: Métodos batch nos repos**

Adicionar as assinaturas às 2 interfaces (Application/Graph/Abstractions). Implementar nos EF repos **espelhando** os métodos single existentes (`ListByServiceAsync`/`ListByServiceIdAsync`), trocando por `.Where(x => ids.Contains(x.ServiceAssetId))` (ServiceLink) e o equivalente por owner service para ApiAsset (converter `IReadOnlyCollection<ServiceAssetId>` para `List<Guid>` de valores conforme o mapeamento). Seguir o padrão de query EF já usado no ficheiro.

- [ ] **Step 5: Registar no DI** — em `Graph/DependencyInjection.cs`, `services.AddScoped<ServiceMaturityCalculator>();` (seguir o estilo de registo do ficheiro).

- [ ] **Step 6: Refactor do dashboard**

Em `GetServiceMaturityDashboard.Handler`: injetar `ServiceMaturityCalculator`; substituir o loop N+1 (linhas 59-114) por `var maturity = await calculator.ComputeForServicesAsync(services, ct);` e mapear cada `ServiceMaturityResult` + os campos do serviço (`ServiceName`, `DisplayName`, `TeamName`, `Domain`, `Criticality`, `LifecycleStatus`) → `ServiceMaturityItemDto`. Manter `ScoreToLevel`/summary/ordenação — o summary usa os mesmos DTOs. Remover injeções de repo que deixem de ser usadas diretamente pelo handler (link/api/contract repos passam a estar no calculator) — **verificar** que já não são referenciadas no handler antes de remover.

- [ ] **Step 7: Testes do calculator passam + build**

`dotnet build` dos projetos Application + Infrastructure; `dotnet test ... --filter "FullyQualifiedName~ServiceMaturityCalculator|FullyQualifiedName~MaturityDashboard"` → PASS. Confirmar que os testes existentes do dashboard (se houver) continuam verdes.

- [ ] **Step 8: Commit**

```bash
git add src/modules/catalog
git commit -m "refactor(catalog): ServiceMaturityCalculator partilhado + gather batch (de-N+1 no dashboard)"
```

---

### Task 2: ListServices — filtro + ordenação por maturidade

**Files:**
- Modify: `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Features/ListServices/ListServices.cs`
- Modify: `src/modules/catalog/NexTraceOne.Catalog.API/Graph/Endpoints/Endpoints/ServiceCatalogEndpointModule.cs`
- Test: `tests/modules/catalog/NexTraceOne.Catalog.Tests/Graph/Features/ListServicesMaturityTests.cs`

**Interfaces:**
- Consumes: `ServiceMaturityCalculator.ComputeForServicesAsync` (Task 1); `IServiceAssetRepository.ListFilteredAsync`.

- [ ] **Step 1: Teste (falha)**

Criar `ListServicesMaturityTests.cs`. Substituir `IServiceAssetRepository` (devolve N serviços via `ListFilteredAsync`) e injetar um `ServiceMaturityCalculator` real com repos substituídos que dão maturidades diferentes por serviço (ou substituir o calculator se for extraída interface — mas é classe concreta; então mockar os 3 repos que ele usa). Casos:
- `SortBy="maturity"` ascendente → items ordenados por score crescente.
- `MaturityLevel="Managed"` → só os serviços de nível Managed; `TotalCount` = esse número.
- caminho rápido (`MaturityLevel=null, SortBy=null`) → usa `ListFilteredAsync` paginado, ordem do repo, e **não** chama os repos do calculator.

(Se mockar o calculator concreto for difícil, extrair `IServiceMaturityCalculator` na Task 1 e registar por interface — decisão do implementador; preferir interface se simplificar o teste.)

- [ ] **Step 2: Correr — falha.**

- [ ] **Step 3: Implementar**

`ListServices.Query` ganha `string? MaturityLevel = null, string? SortBy = null, bool SortDescending = false`. `Validator`: `MaturityLevel` ∈ {Initial,Developing,Defined,Managed,Optimizing} quando não-nulo; `SortBy` ∈ {name,maturity} quando não-nulo. Handler injeta `ServiceMaturityCalculator`:
- Fast path (`MaturityLevel is null && !string.Equals(SortBy,"maturity",OrdinalIgnoreCase)`) → como hoje.
- Maturity path → `ListFilteredAsync(..., page:1, pageSize:10_000, ct)` (todos os que batem os outros filtros); `var mat = await calculator.ComputeForServicesAsync(all, ct);` filtrar `all` por `mat[id].Level == MaturityLevel` se dado; ordenar por `mat[id].OverallScore` (asc/desc via `SortDescending`); `total = filtered.Count`; `paged = filtered.Skip((page-1)*pageSize).Take(pageSize)`; mapear para `ServiceListItem` (DTO inalterado); `Response(items, total)`.

- [ ] **Step 4: Endpoint** — `ServiceCatalogEndpointModule.cs`: o handler do GET de listagem passa a ler `maturityLevel`, `sortBy`, `sortDescending` da query string e a incluí-los na `ListServices.Query`. Seguir o padrão de binding já usado (provavelmente `[AsParameters]` num record de request, ou parâmetros individuais).

- [ ] **Step 5: Testes passam + build** — `dotnet test ... --filter "FullyQualifiedName~ListServices"` PASS; `dotnet build` Application + API.

- [ ] **Step 6: Commit**

```bash
git add src/modules/catalog
git commit -m "feat(catalog): ListServices aceita filtro e ordenação por maturidade (compute-then-page)"
```

---

### Task 3: Frontend — filtro + coluna ordenável por maturidade

**Files:**
- Modify: `src/frontend/src/features/catalog/api/serviceCatalog.ts` (params de `listServices`)
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx`
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`
- Test: `src/frontend/src/__tests__/catalog/ServiceCatalogListPage.maturitySortFilter.test.tsx`

- [ ] **Step 1: Teste (falha)**

Novo teste: mock de `serviceCatalogApi.listServices` e `getMaturityDashboard`. Asserir: (a) escolher um nível no dropdown "Maturidade" faz `listServices` ser chamado com `maturityLevel: '<nível>'`; (b) clicar no cabeçalho "Maturidade" faz `listServices` ser chamado com `sortBy: 'maturity'` e alterna `sortDescending` no 2º clique. (Inspecionar os args via `expect(listServices).toHaveBeenCalledWith(expect.objectContaining({...}))`.)

- [ ] **Step 2: Correr — falha.**

- [ ] **Step 3: Implementar**

- `listServices` params: adicionar `maturityLevel?: string; sortBy?: string; sortDescending?: boolean;` e passá-los a `client.get('/catalog/services', { params })`.
- `ServiceCatalogListPage`: adicionar `maturityFilter` e `sortBy`/`sortDescending` a estado/filters; incluir na `queryKey` e nos `params`; dropdown "Maturidade" (`Select` com Todos + 5 níveis via `serviceMaturity.level.*`); cabeçalho da coluna Maturidade clicável (botão) que alterna `sortBy='maturity'` asc↔desc com ícone (`ArrowUp/ArrowDown` lucide); reset de `currentPage` ao mudar. Coluna de badges (ciclo 40) mantém-se.

- [ ] **Step 4: i18n x4** — `catalog.filters.maturity` (EN Maturity/ES Madurez/pt Maturidade) e `catalog.columns.sortByMaturity` (aria-label; EN "Sort by maturity"/ES/pt equivalentes). Preservar newline final.

- [ ] **Step 5: Correr — passa; build+lint** — `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceCatalogListPage.maturitySortFilter.test.tsx` PASS; `npm run build && npm run lint` 0 erros.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/api/serviceCatalog.ts src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx src/frontend/src/locales src/frontend/src/__tests__/catalog/ServiceCatalogListPage.maturitySortFilter.test.tsx
git commit -m "feat(catalog): filtro e ordenação por maturidade na lista do catálogo"
```

---

### Task 4: Gates finais + verificação de stub

**Files:** nenhum (controlador).

- [ ] **Step 1: Backend** — `dotnet build NexTraceOne.sln` (ou os projetos catalog) + `dotnet test tests/modules/catalog/NexTraceOne.Catalog.Tests/NexTraceOne.Catalog.Tests.csproj --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~Selenium&FullyQualifiedName!~IntegrationTests"` → PASS.
- [ ] **Step 2: Frontend** — `cd src/frontend && npm run test -- --run && npm run validate:i18n && npm run build` → PASS.
- [ ] **Step 3: Stub** — o stub é frontend-only (MSW). O handler MSW de `/catalog/services` provavelmente ignora `maturityLevel`/`sortBy` (backend real fá-lo). Verificar visualmente: dropdown "Maturidade" presente + cabeçalho Maturidade clicável com ícone de ordenação; 0 erros de consola. (A filtragem/ordenação real só se vê com backend real — nota honesta; opcionalmente estender o handler MSW para simular, se trivial.)
