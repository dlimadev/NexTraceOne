# P10.2 — Cross-Module Search com PostgreSQL FTS Report

> **Status:** COMPLETED  
> **Date:** 2026-03-27  
> **Phase:** P10.2 — Search cross-module com PostgreSQL FTS para Command Palette e Knowledge Hub

---

## Objetivo

Implementar backend real de pesquisa cross-module com PostgreSQL Full Text Search (FTS) para suportar:

- Command Palette (frontend)
- Knowledge Hub (backend dedicado criado no P10.1)
- Source of Truth (Catalog/Contracts/References)

Sem ampliar escopo para vector DB, embeddings ou semantic search avançada.

---

## Estado anterior (antes desta alteração)

### Command Palette

- `src/frontend/src/components/CommandPalette.tsx` já consumia backend real via:
  - `globalSearchApi.search(...)`
  - endpoint `/api/v1/source-of-truth/global-search`
- Contrato frontend já existia e estava estável:
  - `SearchResultItem { entityId, entityType, title, subtitle, owner, status, route, relevanceScore }`
  - `GlobalSearchResponse { items, facetCounts, totalResults }`

### Backend Search

- Endpoint global existente:
  - `GET /api/v1/source-of-truth/global-search`
  - `GlobalSearch` handler em `Catalog.Application`
- Integração cross-module com Knowledge já existente via:
  - `IKnowledgeSearchProvider` (`NexTraceOne.Knowledge.Contracts`)
  - injeção opcional no `GlobalSearch.Handler`

### Gap principal

- Repositórios ainda usavam pesquisa textual por `LIKE/ILIKE` e ordenação simples.
- Roadmap exigia PostgreSQL FTS como motor inicial.

---

## Modelo de resultado de search adotado (mantido)

Para manter compatibilidade total com Command Palette e minimizar risco, o contrato unificado foi mantido:

- `entityType` (service, contract, doc, runbook, knowledge, note)
- `title`
- `subtitle`
- `owner`
- `status`
- `route`
- `relevanceScore`

Com isto, o frontend não precisou de breaking changes.

---

## Implementação PostgreSQL FTS escolhida

Foi adotado PostgreSQL FTS via funções do provider EF Core/Npgsql:

- `EF.Functions.PlainToTsQuery("simple", term)`
- `EF.Functions.ToTsVector("simple", concatenatedText).Matches(tsQuery)`
- `EF.Functions.ToTsVector(...).Rank(tsQuery)` para ordenação por relevância

Estratégia desta fase:

- **Sem novas colunas tsvector materializadas**
- **Sem migração de schema**
- FTS aplicado diretamente nas queries dos repositórios já existentes

Isto entrega funcionalidade mínima correta com mudança cirúrgica, preservando evolução futura para:

- tsvector persistido
- GIN index
- tuning de ranking

---

## Ficheiros alterados

### Backend — Search FTS

1. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs`
   - `SearchAsync` migrou de `ILIKE` para PostgreSQL FTS (`ToTsVector/PlainToTsQuery/Rank`)

2. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/OperationalNoteRepository.cs`
   - `SearchAsync` migrou de `ILIKE` para PostgreSQL FTS

3. `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Repositories/ServiceAssetRepository.cs`
   - `SearchAsync` migrou de `LIKE` para PostgreSQL FTS

4. `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Repositories/LinkedReferenceRepository.cs`
   - `SearchAsync` migrou de `ILIKE` para PostgreSQL FTS

5. `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Repositories/ContractVersionRepository.cs`
   - `SearchAsync` migrou de `LIKE` para PostgreSQL FTS (SemVer/ImportedFrom/Protocol)
   - ordenação por `Rank` quando há `searchTerm`

### Backend — documentação inline

6. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Search/KnowledgeSearchProvider.cs`
   - resumo atualizado para refletir que agora usa FTS (não mais ILIKE)

### Testes

7. `tests/modules/catalog/NexTraceOne.Catalog.Tests/SourceOfTruth/Application/Features/SourceOfTruthApplicationTests.cs`
   - adicionados testes de `GlobalSearch` para:
     - incluir resultados de Knowledge quando provider está disponível
     - garantir facetas `knowledge`/`notes` com zero quando provider ausente

---

## Handlers / Services / Queries / Endpoints impactados

### Endpoints (já existentes, reutilizados)

- `GET /api/v1/source-of-truth/global-search`
  - `src/modules/catalog/NexTraceOne.Catalog.API/SourceOfTruth/Endpoints/Endpoints/SourceOfTruthEndpointModule.cs`
- `GET /api/v1/knowledge/search`
  - `src/modules/knowledge/NexTraceOne.Knowledge.API/Endpoints/KnowledgeEndpointModule.cs`

### Handler principal cross-module (já existente, mantido)

- `GlobalSearch.Handler`
  - `src/modules/catalog/NexTraceOne.Catalog.Application/SourceOfTruth/Features/GlobalSearch/GlobalSearch.cs`
  - continua agregando:
    - Services (Catalog)
    - Contracts (Catalog)
    - Docs/Runbooks via LinkedReference (Source of Truth)
    - Knowledge/Notes via `IKnowledgeSearchProvider`

### Service/provider cross-module (já existente, mantido)

- `KnowledgeSearchProvider`
  - `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Search/KnowledgeSearchProvider.cs`
  - agora passa a operar sobre repositórios com FTS real

---

## Módulos/fontes incluídos na primeira onda

### Incluídos e funcionais

- **Catalog / Source of Truth**
  - `ServiceAsset`
  - `LinkedReference` (docs/runbooks)
- **Contracts**
  - `ContractVersion`
- **Knowledge**
  - `KnowledgeDocument`
  - `OperationalNote`

### Não incluídos nesta fase (por escopo)

- vector DB / embeddings / semantic ranking por ML
- facetas avançadas enterprise
- knowledge relations profundas com scoring dedicado

---

## Impacto no frontend (mínimo)

- Nenhuma quebra de contrato para:
  - `src/frontend/src/features/catalog/api/globalSearch.ts`
  - `src/frontend/src/components/CommandPalette.tsx`
- Command Palette continua consumindo o mesmo endpoint e DTO.
- O backend agora devolve resultados usando motor FTS real.

---

## Validação executada

### Restore/build baseline

- `dotnet restore NexTraceOne.sln` ✅
- `dotnet build NexTraceOne.sln --configuration Release --no-restore` ✅

### Testes impactados

- `dotnet test tests/modules/catalog/NexTraceOne.Catalog.Tests/NexTraceOne.Catalog.Tests.csproj --configuration Release --no-restore` ✅
  - Passed: 642
- `dotnet test tests/modules/knowledge/NexTraceOne.Knowledge.Tests/NexTraceOne.Knowledge.Tests.csproj --configuration Release --no-restore` ✅
  - Passed: 23

### Build de host API

- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj --configuration Release --no-restore` ✅

> Observação: warnings pré-existentes do repositório permaneceram (sem regressão funcional introduzida por P10.2).

---

## Conclusão

P10.2 foi fechado com mudança mínima e rastreável:

- backend cross-module search real permanece operacional
- motor inicial de busca foi migrado para PostgreSQL FTS
- Knowledge Hub participa da pesquisa global
- Source of Truth/Catalog/Contracts participam com FTS
- Command Palette mantém integração com backend real sem alterações de contrato

Isso avança o pilar **Source of Truth & Operational Knowledge** sem desviar para escopo de P10.3.
