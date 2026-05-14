# Auditoria Módulo Catalog — NexTraceOne
**Data:** 2026-05-14  
**Escopo:** `src/modules/catalog/`, `tests/modules/catalog/`, `src/frontend/src/features/catalog/`  
**Branch de trabalho:** `claude/code-review-audit-sIsS0`  
**Auditor:** Claude Code (Revisão automática ponta a ponta)

---

## Sumário Executivo

O módulo Catalog é o maior e mais complexo do NexTraceOne, com ~970 ficheiros C# no backend e 49 ficheiros TypeScript no frontend. Cobre 6 bounded contexts (Graph, Contracts, Portal, LegacyAssets, DeveloperExperience, Templates). A estrutura geral está bem alinhada com DDD, Clean Architecture e CQRS.

Foram encontrados **2 bugs críticos de produção**, **6 problemas de alta severidade** e **12 de média severidade**, distribuídos nas camadas de domínio, persistência, aplicação e frontend. Os problemas mais urgentes são um mismatch de check constraint no banco de dados e um full-table-scan num repositório central.

---

## Índice de Problemas

| # | Severidade | Área | Título |
|---|-----------|------|--------|
| 1 | 🔴 CRÍTICO | Infrastructure/Graph | Check constraint `ExposureType` quebrado — `'External'` ausente |
| 2 | 🔴 CRÍTICO | Infrastructure/Contracts | `ListLatestPerApiAssetAsync` — full table scan em memória |
| 3 | 🔴 CRÍTICO | Infrastructure/Contracts | `GetSummaryAsync` — carrega toda a tabela para aggregate |
| 4 | 🟠 ALTO | Domain/Graph | `ServiceAsset` herda de `Entity<T>` em vez de `AuditableEntity<T>` |
| 5 | 🟠 ALTO | Application/Contracts | `CreateDraft` handler — re-query frágil após commit |
| 6 | 🟠 ALTO | Application/Graph | `ListServices` sem paginação |
| 7 | 🟠 ALTO | Infrastructure/Graph | `SearchAsync` em `ServiceAssetRepository` sem índice GIN |
| 8 | 🟠 ALTO | Domain/Contracts | Nenhuma validação de tamanho de `SpecContent` no domínio |
| 9 | 🟡 MÉDIO | Infrastructure/DeveloperExperience | `NullIDEUsageRepository` existe mas confunde (dead code) |
| 10 | 🟡 MÉDIO | Frontend | `EXPOSURE_VALUES` — valor `'External'` vs enum ok, mas inconsistência conceptual |
| 11 | 🟡 MÉDIO | Frontend/ServiceCatalogListPage | Sem paginação no frontend — carrega todos os serviços |
| 12 | 🟡 MÉDIO | Infrastructure/Contracts | `ContractsDbContext` — DbSet via inner type da interface |
| 13 | 🟡 MÉDIO | Application/Graph | Falta tenant defense-in-depth nos repositórios |
| 14 | 🟡 MÉDIO | Infrastructure/Graph | Sem índice standalone em `ApiAssetId` na `ctr_contract_versions` |
| 15 | 🟡 MÉDIO | Testing | `CreateDraft` test não cobre o path real de re-query (sempre cai no fallback) |
| 16 | 🟡 MÉDIO | Application/Contracts | `CreateDraft` — resposta retorna `dateTimeProvider.UtcNow` se `CreatedAt == default` |
| 17 | 🟡 MÉDIO | Security | Ausência de OpenTelemetry Activity spans em handlers críticos |
| 18 | 🟡 MÉDIO | Frontend | Formulário de registro de serviço sem validação de URL (DocumentationUrl, RepositoryUrl) |
| 19 | 🟡 MÉDIO | Infrastructure | `NullIdeContextReader` nomeado como Reader mas poderia causar confusão DI |
| 20 | 🟡 MÉDIO | Domain | Falta de `[EncryptedField]` em dados sensíveis de `ServiceAsset` |

---

## Problema 1 — 🔴 CRÍTICO: Check Constraint `ExposureType` Quebrado

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Configurations/ServiceAssetConfiguration.cs:25`
- `src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/Enums/ExposureType.cs`

### Descrição

O `CheckConstraint` de banco de dados lista `'Internal', 'Partner', 'Public'` mas o enum C# serializa para `"External"` (não `"Public"`). Qualquer `INSERT` ou `UPDATE` com `ExposureType.External` viola o constraint e gera um `PostgresException` em produção.

**Enum atual:**
```csharp
public enum ExposureType
{
    Internal = 0,  // → serializa "Internal" ✅
    External = 1,  // → serializa "External" ❌ não está na constraint
    Partner  = 2   // → serializa "Partner"  ✅
}
```

**Constraint atual:**
```sql
"ExposureType" IN ('Internal', 'Partner', 'Public')
-- 'External' AUSENTE, 'Public' ERRADO
```

### Impacto
- Todo registo de serviço com exposição `External` falha com erro 500 em produção.
- Dados já existentes na tabela (se `ExposureType = 1`) podem estar inconsistentes se foram inseridos contornando o constraint.

### Correção

**Opção A — Corrigir a constraint (recomendado):**
```csharp
// ServiceAssetConfiguration.cs — linha 25
t.HasCheckConstraint(
    "CK_cat_service_assets_exposure_type",
    "\"ExposureType\" IN ('Internal', 'External', 'Partner')");
```
Gerar nova migration: `dotnet ef migrations add Fix_ExposureType_CheckConstraint`

**Opção B — Renomear enum value para Public (breaking change maior):**  
Não recomendado — obriga mudança de nome no domínio e frontend.

---

## Problema 2 — 🔴 CRÍTICO: `ListLatestPerApiAssetAsync` Full Table Scan

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Repositories/ContractVersionRepository.cs:137–172`

### Descrição

O método carrega **toda** a tabela `ctr_contract_versions` para memória, depois agrupa e filtra em LINQ:

```csharp
// ❌ Carrega tabela inteira
var latestVersions = (await context.ContractVersions
        .AsNoTracking()
        .ToListAsync(cancellationToken))
    .GroupBy(v => v.ApiAssetId)
    .Select(g => g.OrderByDescending(v => v.CreatedAt).First())
    .AsEnumerable();

// Filtragem em memória — pós-load
if (protocol.HasValue)
    latestVersions = latestVersions.Where(v => v.Protocol == protocol.Value);
```

Em produção com 10.000+ versões de contrato e tenant com ~500 APIs, isto causa:
- Latência > 5s por request
- Pressão de memória no GC
- Risco de timeout do banco de dados

### Correção

Substituir por query SQL com window function:

```csharp
public async Task<(IReadOnlyList<ContractVersion> Items, int TotalCount)> ListLatestPerApiAssetAsync(
    ContractProtocol? protocol,
    ContractLifecycleState? lifecycleState,
    string? searchTerm,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    // Subquery: número de linha por ApiAssetId ordenado por CreatedAt DESC
    var latestQuery = context.ContractVersions
        .AsNoTracking()
        .GroupBy(v => v.ApiAssetId)
        .Select(g => g.OrderByDescending(v => v.CreatedAt).First());

    if (protocol.HasValue)
        latestQuery = latestQuery.Where(v => v.Protocol == protocol.Value);  // EF traduz para SQL

    if (lifecycleState.HasValue)
        latestQuery = latestQuery.Where(v => v.LifecycleState == lifecycleState.Value);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        latestQuery = latestQuery.Where(v =>
            EF.Functions.ILike(v.SemVer, $"%{searchTerm}%") ||
            EF.Functions.ILike(v.ImportedFrom, $"%{searchTerm}%"));
    }

    var totalCount = await latestQuery.CountAsync(cancellationToken);
    var items = await latestQuery
        .OrderByDescending(v => v.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return (items, totalCount);
}
```

**Nota:** Verificar se EF Core 8+ traduz `GroupBy(...).Select(g => g.OrderByDescending(...).First())` correctamente. Se não, usar `FromSqlRaw` com `DISTINCT ON (ApiAssetId)` do PostgreSQL:

```sql
SELECT DISTINCT ON ("ApiAssetId") *
FROM ctr_contract_versions
WHERE "IsDeleted" = false
ORDER BY "ApiAssetId", "CreatedAt" DESC
```

---

## Problema 3 — 🔴 CRÍTICO: `GetSummaryAsync` Carrega Toda a Tabela

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Repositories/ContractVersionRepository.cs:204–237`

### Descrição

```csharp
// ❌ Carrega toda a tabela para fazer COUNT no cliente
var allVersions = await context.ContractVersions
    .AsNoTracking()
    .Select(v => new { v.ApiAssetId, v.Protocol, v.LifecycleState })
    .ToListAsync(cancellationToken);

var totalVersions = allVersions.Count;
var distinctContracts = allVersions.Select(v => v.ApiAssetId).Distinct().Count();
var draftCount = allVersions.Count(v => v.LifecycleState == ContractLifecycleState.Draft);
// ...
```

Mesmo com a projecção selectiva, transfere todos os rows para o cliente. Deve usar `GROUP BY` e `COUNT` no SQL.

### Correção

```csharp
public async Task<ContractSummaryData> GetSummaryAsync(CancellationToken cancellationToken = default)
{
    var totalVersions    = await context.ContractVersions.CountAsync(cancellationToken);
    var distinctContracts = await context.ContractVersions
        .Select(v => v.ApiAssetId).Distinct().CountAsync(cancellationToken);

    var byCombination = await context.ContractVersions
        .GroupBy(v => new { v.Protocol, v.LifecycleState })
        .Select(g => new { g.Key.Protocol, g.Key.LifecycleState, Count = g.Count() })
        .ToListAsync(cancellationToken);

    var draftCount      = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.Draft).Sum(x => x.Count);
    var inReviewCount   = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.InReview).Sum(x => x.Count);
    var approvedCount   = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.Approved).Sum(x => x.Count);
    var lockedCount     = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.Locked).Sum(x => x.Count);
    var deprecatedCount = byCombination.Where(x =>
        x.LifecycleState is ContractLifecycleState.Deprecated
            or ContractLifecycleState.Sunset
            or ContractLifecycleState.Retired).Sum(x => x.Count);

    var byProtocol = byCombination
        .GroupBy(x => x.Protocol.ToString())
        .Select(g => new ProtocolCount(g.Key, g.Sum(x => x.Count)))
        .OrderByDescending(p => p.Count)
        .ToList();

    return new ContractSummaryData(
        totalVersions, distinctContracts,
        draftCount, inReviewCount, approvedCount, lockedCount, deprecatedCount, byProtocol);
}
```

---

## Problema 4 — 🟠 ALTO: `ServiceAsset` Herda de `Entity<T>` em vez de `AuditableEntity<T>`

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/Entities/ServiceAsset.cs:17`

### Descrição

```csharp
// ❌ Entity<T> — sem audit fields, sem soft-delete
public sealed class ServiceAsset : Entity<ServiceAssetId>
```

Consequências:
1. **Sem `CreatedAt/By`, `UpdatedAt/By`** — impossível saber quem registou ou modificou um serviço.
2. **Sem `IsDeleted`** — o global soft-delete filter da `NexTraceDbContextBase` **não é aplicado** a `ServiceAsset`. Se uma entidade for marcada como eliminada (se tiver um método de SoftDelete custom), não será filtrada nas queries.
3. **`AuditInterceptor` não popula campos** — como a classe não herda `AuditableEntity<T>`, o interceptor ignora-a.
4. **Rompe com a convenção do projecto** — todas as outras entidades de catálogo (`ContractVersion`, `ContractDraft`, etc.) usam `AuditableEntity<T>`.

### Verificação

```csharp
// NexTraceDbContextBase.cs — o filtro só se aplica a AuditableEntity
private static void ApplyGlobalSoftDeleteFilter(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (!IsAssignableToGenericType(entityType.ClrType, typeof(AuditableEntity<>)))
            continue; // ← ServiceAsset é ignorado aqui
        // ...
    }
}
```

### Correção

```csharp
// ServiceAsset.cs — linha 17
public sealed class ServiceAsset : AuditableEntity<ServiceAssetId>
// Remover propriedades duplicadas (CreatedAt, etc.) se existirem
```

Gerar nova migration após a alteração para adicionar as colunas de auditoria à tabela `cat_service_assets`.

---

## Problema 5 — 🟠 ALTO: `CreateDraft` Handler com Re-Query Frágil

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/CreateDraft/CreateDraft.cs:83–108`

### Descrição

Após fazer `Add(draft)` e `CommitAsync()`, o handler re-consulta a base de dados para encontrar o draft recém-criado:

```csharp
// ❌ Anti-pattern: draft.Id.Value JÁ EXISTE após ContractDraft.Create()
var persistedDraft = (await repository.ListAsync(
        DraftStatus.Editing,
        request.ServiceId,
        request.Author,
        1,
        20,      // limite arbitrário de 20 resultados
        cancellationToken))
    .OrderByDescending(item => item.CreatedAt)
    .FirstOrDefault(item => item.Title == request.Title && item.Protocol == request.Protocol)
    ?? draft;  // fallback para o objecto em memória

return new Response(
    persistedDraft.Id.Value,
    persistedDraft.Title,
    persistedDraft.Status.ToString(),
    persistedDraft.CreatedAt == default  // ← indica que o fallback foi usado
        ? dateTimeProvider.UtcNow
        : persistedDraft.CreatedAt);
```

**Problemas:**
1. **Race condition**: Se dois utilizadores criarem drafts com o mesmo `Title` + `Protocol` para o mesmo serviço em simultâneo, o handler pode retornar o ID do draft errado.
2. **Redundante**: `ContractDraft.Create()` gera o ID com `ContractDraftId.New()`. O Id está disponível imediatamente.
3. **O `?? draft` fallback** com `CreatedAt == default` é um code smell que mascara o bug — os testes sempre caem no fallback porque `repository.ListAsync` retorna lista vazia nos mocks.

### Correção

```csharp
public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
{
    Guard.Against.Null(request);

    var service = await serviceAssetRepository.GetByIdAsync(
        ServiceAssetId.From(request.ServiceId), cancellationToken);

    if (service is null)
        return ContractsErrors.CatalogLinkNotFound(request.ServiceId.ToString());

    if (!ServiceContractPolicy.SupportsContracts(service.ServiceType))
        return ContractsErrors.ServiceTypeDoesNotSupportContracts(service.ServiceType.ToString());

    if (!ServiceContractPolicy.IsContractTypeAllowed(service.ServiceType, request.ContractType))
        return ContractsErrors.ContractTypeNotAllowedForServiceType(
            request.ContractType.ToString(), service.ServiceType.ToString());

    var result = ContractDraft.Create(
        request.Title, request.Author, request.ContractType,
        request.Protocol, request.ServiceId, request.Description);

    if (result.IsFailure)
        return result.Error;

    var draft = result.Value;
    repository.Add(draft);
    await unitOfWork.CommitAsync(cancellationToken);

    // ✅ Id, Title, Status e CreatedAt disponíveis no objecto de domínio
    return new Response(
        draft.Id.Value,
        draft.Title,
        draft.Status.ToString(),
        draft.CreatedAt);
}
```

**Nota:** `draft.CreatedAt` será populado pelo `AuditInterceptor` durante `SaveChangesAsync`. Se `ContractDraft` herda `AuditableEntity<T>`, o valor está disponível após o commit. Caso seja necessário o valor exacto antes do `SaveChanges`, usar `dateTimeProvider.UtcNow` directamente.

---

## Problema 6 — 🟠 ALTO: `ListServices` sem Paginação

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Features/ListServices/ListServices.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Repositories/ServiceAssetRepository.cs:31`

### Descrição

`ListServices.Query` não possui `Page`/`PageSize` e `ServiceAssetRepository.ListFilteredAsync` devolve sempre **todos os serviços** que correspondem aos filtros:

```csharp
// ❌ Sem paginação
public sealed record Query(
    string? TeamName, string? Domain,
    ServiceType? ServiceType, ..., string? SearchTerm) : IQuery<Response>;

// No repositório:
return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken); // ← todos os resultados
```

Num tenant enterprise com 1.000+ serviços, este endpoint pode devolver payloads de MB, saturar a memória do servidor e causar timeouts.

### Correção

**1. Alterar a Query para incluir paginação:**
```csharp
public sealed record Query(
    string? TeamName,
    string? Domain,
    ServiceType? ServiceType,
    Criticality? Criticality,
    LifecycleStatus? LifecycleStatus,
    ExposureType? ExposureType,
    string? SearchTerm,
    int Page = 1,
    int PageSize = 50) : IPagedQuery<Response>;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        // ...filtros existentes...
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
```

**2. Actualizar o repositório:**
```csharp
public async Task<(IReadOnlyList<ServiceAsset> Items, int TotalCount)> ListFilteredAsync(
    ..., int page, int pageSize, CancellationToken cancellationToken)
{
    var query = _context.ServiceAssets.AsNoTracking().AsQueryable();
    // ...filtros...
    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderBy(s => s.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);
    return (items, totalCount);
}
```

**3. Actualizar o endpoint para aceitar query params de paginação.**

---

## Problema 7 — 🟠 ALTO: `SearchAsync` em `ServiceAssetRepository` sem Índice GIN

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Repositories/ServiceAssetRepository.cs:87–113`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/Configurations/ServiceAssetConfiguration.cs`

### Descrição

O `SearchAsync` calcula `to_tsvector` em tempo de execução sobre 5 colunas em cada linha:

```csharp
// ❌ tsvector calculado inline — sem índice, sequential scan em toda a tabela
.Select(s => new
{
    Service = s,
    SearchVector = EF.Functions.ToTsVector(
        "simple",
        (s.Name ?? string.Empty) + " " + (s.DisplayName ?? string.Empty) + " " + ...)
})
.Where(x => x.SearchVector.Matches(tsQuery))
```

Sem um índice GIN no `tsvector`, o PostgreSQL faz um sequential scan com cálculo vectorial por linha. Com 1.000+ serviços, a latência pode exceder 500ms (threshold do `PerformanceBehavior`).

### Correção

**1. Adicionar coluna gerada + índice GIN na configuração EF:**
```csharp
// ServiceAssetConfiguration.cs — adicionar após índices existentes
builder.HasGeneratedTsVectorColumn(
    x => x.SearchVector,
    "simple",
    x => new { x.Name, x.DisplayName, x.Domain, x.TeamName, x.Description })
    .HasIndex(x => x.SearchVector)
    .HasMethod("GIN");
```

**2. Adicionar a propriedade na entidade:**
```csharp
// ServiceAsset.cs — adicionar propriedade de FTS
public NpgsqlTsVector SearchVector { get; private set; } = null!;
```

**3. Simplificar `SearchAsync`:**
```csharp
public async Task<IReadOnlyList<ServiceAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
{
    var term = searchTerm.Trim();
    if (term.Length == 0) return [];

    var tsQuery = EF.Functions.PlainToTsQuery("simple", term);
    return await _context.ServiceAssets
        .AsNoTracking()
        .Where(s => s.SearchVector.Matches(tsQuery))
        .OrderByDescending(s => s.SearchVector.Rank(tsQuery))
        .ToListAsync(cancellationToken);
}
```

**4. Gerar migration** para persistir a coluna gerada e criar o índice GIN.

---

## Problema 8 — 🟠 ALTO: Nenhuma Validação de Tamanho de `SpecContent` no Domínio

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/ContractVersion.cs` — método `Import`
- `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/ContractDraft.cs` — método `SetContent`

### Descrição

A documentação (`/// <summary>`) menciona "máx. 1MB" para `SpecContent`, mas não há nenhum guard no código:

```csharp
// ContractVersion.Import — sem validação de tamanho
public static Result<ContractVersion> Import(
    Guid apiAssetId, string semVer, string specContent,
    string format, string importedFrom, ContractProtocol protocol = ContractProtocol.OpenApi)
{
    Guard.Against.Default(apiAssetId);
    Guard.Against.NullOrWhiteSpace(format);
    Guard.Against.NullOrWhiteSpace(importedFrom);
    // ❌ Sem Guard.Against para specContent size
```

Uma especificação de 50MB enviada pelo frontend chegaria ao banco de dados sem qualquer rejeição, causando:
- Timeouts de base de dados
- Pressão de memória
- Degradação de performance no parsing

### Correção

```csharp
// ContractVersion.cs — em Import()
private const int MaxSpecContentSizeBytes = 1 * 1024 * 1024; // 1MB

public static Result<ContractVersion> Import(...)
{
    // ...guards existentes...
    if (!string.IsNullOrEmpty(specContent) &&
        System.Text.Encoding.UTF8.GetByteCount(specContent) > MaxSpecContentSizeBytes)
        return ContractsErrors.SpecContentTooLarge(MaxSpecContentSizeBytes);
    // ...
}
```

Adicionar também no `Validator` do `CreateDraft.Command` via `FluentValidation`:
```csharp
RuleFor(x => x.SpecContent)
    .Must(c => c is null || System.Text.Encoding.UTF8.GetByteCount(c) <= 1_048_576)
    .WithMessage("SpecContent excede o limite de 1MB.");
```

---

## Problema 9 — 🟡 MÉDIO: `NullIDEUsageRepository` é Dead Code Confuso

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/DeveloperExperience/Services/NullIDEUsageRepository.cs`

### Descrição

Existe uma implementação `NullIDEUsageRepository` que descarta silenciosamente todos os registos IDE. No entanto, o `DeveloperExperience/DependencyInjection.cs` já regista correctamente `EfIdeUsageRepository`:

```csharp
// DependencyInjection.cs — correcto
services.AddScoped<IIDEUsageRepository, EfIdeUsageRepository>();
```

A `NullIDEUsageRepository` é dead code que:
- Viola a regra do CLAUDE.md: "Se você ver um `NullXxxRepository`, isso é um bug"
- Cria confusão sobre qual implementação está activa
- Pode ser activada acidentalmente se alguém mudar o registo

### Correção

Eliminar `NullIDEUsageRepository.cs`. Se necessário um fallback para testes, usar `Substitute.For<IIDEUsageRepository>()` com NSubstitute.

---

## Problema 10 — 🟡 MÉDIO: `EXPOSURE_VALUES` Frontend — Coerência Conceptual

### Localização
- `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx:74`
- `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx:277-278`

### Descrição

O enum C# `ExposureType.External` serializa para `"External"` e o frontend usa `'External'` — tecnicamente correcto. No entanto, o label exibido ao utilizador é `"External / Public"` o que é ambíguo:

```typescript
const EXPOSURE_VALUES = ['Internal', 'External', 'Partner'] as const;
// ...
<option value="External">{t('serviceCatalog.exposureExternal', 'External / Public')}</option>
```

A descrição do enum no domínio diz "exposto a consumidores externos" para `External`, mas o label `"External / Public"` mistura dois conceitos.

### Correção

Alinhar labels com i18n e torná-los precisos:
```typescript
// i18n keys em vez de fallback hardcoded
<option value="Internal">{t('catalog.exposureType.internal')}</option>
<option value="External">{t('catalog.exposureType.external')}</option>
<option value="Partner">{t('catalog.exposureType.partner')}</option>
```
Definir nos ficheiros de tradução:
- `catalog.exposureType.internal` → "Interno"
- `catalog.exposureType.external` → "Externo (público)"
- `catalog.exposureType.partner` → "Parceiros"

---

## Problema 11 — 🟡 MÉDIO: Frontend sem Paginação no Catálogo de Serviços

### Localização
- `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx:149–161`
- `src/frontend/src/features/catalog/api/serviceCatalog.ts`

### Descrição

O componente `ServiceCatalogListPage` não implementa paginação:

```typescript
const {
    data,
    isLoading,
    isError,
} = useQuery({
    queryKey: ['catalog-services', queryParams, activeEnvironmentId],
    queryFn: () => serviceCatalogApi.listServices(queryParams),
    // ❌ sem page/pageSize nos params
});

const services: ServiceListItem[] = data?.items ?? []; // todos os resultados renderizados
```

Com centenas de serviços, o componente renderizará todos de uma vez sem virtualização, causando:
- Slow initial render
- Jank durante scroll
- Alto consumo de memória no browser

### Correção

**1. Adicionar estado de paginação:**
```typescript
const [currentPage, setCurrentPage] = useState(1);
const PAGE_SIZE = 50;

const queryParams = useMemo(() => ({
    ...activeFilters,
    page: currentPage,
    pageSize: PAGE_SIZE,
}), [...deps, currentPage]);
```

**2. Adicionar componente Pagination** ou usar paginação cursor-based.

**3. Considerar react-virtual** para virtualização de lista quando o total > 200 items.

---

## Problema 12 — 🟡 MÉDIO: `ContractsDbContext` DbSet via Inner Type de Interface

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs:95`

### Descrição

```csharp
// ❌ DbSet via inner type da interface — acoplamento desnecessário
public DbSet<IDeprecationScheduleRepository.DeprecationScheduleRecord> DeprecationSchedules
    => Set<IDeprecationScheduleRepository.DeprecationScheduleRecord>();
```

Isto expõe o `DbContext` a detalhes internos da interface do repositório. Se a interface mudar, o `DbContext` quebra. O EF Core deve referenciar o tipo de domínio directamente.

### Correção

Criar um tipo de entidade concreto no domínio ou infrastructure:
```csharp
// Na pasta Domain/Contracts/Entities/ ou Infrastructure/Contracts/
public sealed class DeprecationScheduleRecord : AuditableEntity<DeprecationScheduleRecordId>
{
    // ...propriedades de domínio
}

// ContractsDbContext.cs
public DbSet<DeprecationScheduleRecord> DeprecationSchedules => Set<DeprecationScheduleRecord>();
```

---

## Problema 13 — 🟡 MÉDIO: Falta Defense-in-Depth de Tenant nos Repositórios

### Localização
- Todos os repositórios em `Graph/Persistence/Repositories/` e `Contracts/Persistence/Repositories/`

### Descrição

O CLAUDE.md define claramente:
> **Repository-level filter** — every read method must also add `.Where(e => e.TenantId == currentTenant.Id)` as defense-in-depth. Background jobs... intentionally skip this filter.

Actualmente, os repositórios dependem **exclusivamente** do `TenantRlsInterceptor` (PostgreSQL RLS via `set_config`). Se o interceptor falhar (bug, misconfiguration, bypass) ou em contextos sem tenant (background jobs mal configurados), dados cross-tenant poderiam ser expostos.

**`ServiceAsset` não tem `TenantId`** (problema 4 relacionado), então o filtro nem pode ser aplicado.

### Correção

**Passo 1:** Após corrigir o Problema 4 (herança de `AuditableEntity`), adicionar `TenantId` a `ServiceAsset`:
```csharp
// ServiceAsset.cs
public Guid TenantId { get; private set; }
```

**Passo 2:** Injectar `ICurrentTenant` nos repositórios críticos:
```csharp
internal sealed class ServiceAssetRepository(CatalogGraphDbContext context, ICurrentTenant currentTenant)
    : RepositoryBase<ServiceAsset, ServiceAssetId>(context), IServiceAssetRepository
{
    public async Task<IReadOnlyList<ServiceAsset>> ListFilteredAsync(...)
    {
        var query = _context.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenant.Id); // ← defense-in-depth
        // ...
    }
}
```

---

## Problema 14 — 🟡 MÉDIO: Índice em `ApiAssetId` para Queries de Contrato

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Configurations/ContractVersionConfiguration.cs`

### Descrição

Existe um índice composto único em `(ApiAssetId, SemVer)`. Queries frequentes filtram apenas por `ApiAssetId` (ex: `ListByApiAssetAsync`). O planner do PostgreSQL pode usar o índice composto para queries por `ApiAssetId`, mas um índice standalone em `ApiAssetId` com `INCLUDE` pode ser mais eficiente para range scans ordenados.

### Correção

Avaliar após análise de `EXPLAIN ANALYZE` em produção. Se necessário:
```csharp
builder.HasIndex(x => x.ApiAssetId)
    .HasDatabaseName("IX_ctr_contract_versions_ApiAssetId");
```

---

## Problema 15 — 🟡 MÉDIO: Teste `CreateDraft` Não Valida o Path Real

### Localização
- `tests/modules/catalog/NexTraceOne.Catalog.Tests/Contracts/Application/Features/ContractDraftCrudTests.cs:49–72`

### Descrição

O teste `CreateDraft_Should_ReturnResponse_When_ValidCommand` mockeia `repository.ListAsync` para retornar lista vazia:
```csharp
repository.ListAsync(...).Returns(new List<ContractDraft>());
```

Isso faz com que o handler **sempre** use o fallback `?? draft`, nunca testando o path da re-query. O teste passa mesmo com o comportamento bugado. Após a correcção do Problema 5, este teste deve ser simplificado para não mockar `ListAsync` de todo.

### Correção

Após aplicar a correcção do Problema 5, actualizar o teste:
```csharp
[Fact]
public async Task CreateDraft_Should_ReturnDraftId_When_ValidCommand()
{
    var repository  = Substitute.For<IContractDraftRepository>();
    var unitOfWork  = CreateUnitOfWork();
    var clock       = Substitute.For<IDateTimeProvider>();
    clock.UtcNow.Returns(FixedNow);

    var sut = new CreateDraftFeature.Handler(
        repository, CreateServiceRepo(), unitOfWork, clock);

    var result = await sut.Handle(
        new CreateDraftFeature.Command("My API", "author",
            ContractType.RestApi, ContractProtocol.OpenApi, TestServiceId),
        CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.DraftId.Should().NotBeEmpty(); // ← não pode ser Guid.Empty
    result.Value.Title.Should().Be("My API");
    // Remover: repository.Received para ListAsync — não deve mais ser chamado
    repository.Received(1).Add(Arg.Any<ContractDraft>());
    await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
}
```

---

## Problema 16 — 🟡 MÉDIO: `CreatedAt` pode ser `UtcNow` em vez do valor real

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/CreateDraft/CreateDraft.cs:104–108`

### Descrição

```csharp
persistedDraft.CreatedAt == default  // ← indica que o fallback ?? draft foi usado
    ? dateTimeProvider.UtcNow         // ← valor aproximado, não o de persistência
    : persistedDraft.CreatedAt
```

Este bloco retorna uma data aproximada (`UtcNow` no momento da resposta) quando `CreatedAt` está em `default`. Pode haver uma discrepância de milissegundos entre a data retornada e a data real persistida no banco.

**Resolvido automaticamente pela correcção do Problema 5** — ao usar `draft.Id.Value` directamente, não há necessidade do ternário.

---

## Problema 17 — 🟡 MÉDIO: Ausência de OpenTelemetry Activity Spans em Handlers Críticos

### Localização
- Handlers de `CreateDraft`, `ApproveDraft`, `DeprecateContractVersion`, `RegisterServiceAsset`

### Descrição

Os handlers executam lógica de negócio significativa (validação de políticas, criação de entidades, commits) sem emitir Activity spans do OpenTelemetry. Isso torna invisível a instrumentação a nível de feature em ambientes de produção.

### Correção

```csharp
// Exemplo em CreateDraft.Handler
public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
{
    using var activity = NexTraceActivitySources.Catalog.StartActivity("CreateDraft");
    activity?.SetTag("draft.serviceId", request.ServiceId.ToString());
    activity?.SetTag("draft.protocol", request.Protocol.ToString());
    // ...handler logic...
    activity?.SetTag("draft.id", draft.Id.Value.ToString());
    return new Response(...);
}
```

Usar `NexTraceActivitySources.Catalog` definido no `BuildingBlocks.Observability`.

---

## Problema 18 — 🟡 MÉDIO: Frontend sem Validação de URL

### Localização
- `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx:266–276`

### Descrição

O formulário de registo de serviço aceita `documentationUrl` e `repositoryUrl` como text livre sem validar formato URL:

```tsx
<input type="text" value={serviceForm.documentationUrl}
    onChange={...}
    placeholder="https://..."
    className="..." />
```

URLs malformadas chegam ao backend, que os persiste (campo `HasMaxLength(1000)` sem validação de formato).

### Correção

```tsx
<input type="url"  {/* ← muda de text para url — validação nativa HTML5 */}
    value={serviceForm.documentationUrl}
    onChange={...}
    pattern="https?://.+"
    title={t('validation.urlRequired')} />
```

Adicionar também no `Validator` do `RegisterServiceAsset.Command`:
```csharp
RuleFor(x => x.DocumentationUrl)
    .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
    .When(x => !string.IsNullOrEmpty(x.DocumentationUrl))
    .WithMessage("DocumentationUrl deve ser uma URL válida.");
```

---

## Problema 19 — 🟡 MÉDIO: `NullIdeContextReader` Duplica Comportamento

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/DeveloperExperience/Services/NullIdeContextReader.cs`

### Descrição

`IIdeContextReader` é uma interface Reader (não Repository), portanto o null implementation é um placeholder legítimo (Honest-Null Pattern). No entanto, o ficheiro está na pasta `Services/` de Infrastructure junto ao `NullIDEUsageRepository` que é um bug. A nomenclatura e localização podem confundir.

### Correção

Mover `NullIdeContextReader` para `Application/DeveloperExperience/Services/` (mesma camada dos outros null readers), deixando `Infrastructure/Services/` apenas com implementações reais.

---

## Problema 20 — 🟡 MÉDIO: Falta `[EncryptedField]` em Dados Sensíveis de `ServiceAsset`

### Localização
- `src/modules/catalog/NexTraceOne.Catalog.Domain/Graph/Entities/ServiceAsset.cs`

### Descrição

`ServiceAsset` contém campos como `OnCallRotationId`, `ContactChannel` e `TechnicalOwner` que podem conter informações PII (emails, identificadores de colaboradores). Nenhum destes campos usa `[EncryptedField]` apesar do `EncryptionInterceptor` estar disponível.

Comparativamente, `ContractVersion` usa `[EncryptedField]` em campos sensíveis de spec.

### Correção

Avaliar quais campos merecem encriptação at-rest conforme política de classificação de dados. Candidatos prováveis:

```csharp
[EncryptedField]
public string TechnicalOwner { get; private set; } = string.Empty;

[EncryptedField]
public string BusinessOwner { get; private set; } = string.Empty;

[EncryptedField]
public string ContactChannel { get; private set; } = string.Empty;
```

Gerar migration após aplicar os atributos.

---

## Análise de Cobertura de Testes

### Pontos Positivos
- 217 ficheiros de teste cobrindo domain, application e infrastructure.
- Uso correcto de xUnit + FluentAssertions + NSubstitute.
- Testes de domínio bem isolados com test doubles (`TestCurrentTenant`, `TestDateTimeProvider`).
- Cobertura de fluxos de negócio: `ApproveDraft`, `DeprecateContractVersion`, `ExecuteContractPipeline`.

### Lacunas de Cobertura Identificadas

| Área | Gap |
|------|-----|
| `ServiceAssetRepository.SearchAsync` | Sem teste de integração com PostgreSQL FTS |
| `ListLatestPerApiAssetAsync` | Sem teste com volume real de dados |
| `GetSummaryAsync` | Sem teste de performance/volume |
| Frontend `ServiceCatalogListPage` | Apenas 1 test básico (`ServiceCatalogListPage.test.tsx`), sem cenários de filtro ou paginação |
| `ExposureType.External` constraint | Sem teste de integração que cubra o INSERT com ExposureType.External |

---

## Análise de Bibliotecas (Backend)

| Biblioteca | Versão Esperada 2026 | Observação |
|-----------|---------------------|-----------|
| EF Core | 9.x | Verificar uso de `DISTINCT ON` nativo (problema 2) |
| Ardalis.GuardClauses | 4.x | OK — uso consistente |
| FluentValidation | 11.x | OK |
| MediatR | 12.x | OK |
| Hot Chocolate | 14.x | OK |
| NpgsqlEntityFrameworkCore | 9.x | Verificar suporte a `HasGeneratedTsVectorColumn` (problema 7) |
| OpenTelemetry .NET | 1.9+ | OK na infra; falta nos handlers |

---

## Análise de Bibliotecas (Frontend)

| Biblioteca | Observação |
|-----------|-----------|
| React 19 + Vite 6 | OK |
| TanStack Query v5 | OK — uso correcto de `useQuery`/`useMutation` |
| React Router v7 | OK |
| i18next + react-i18next | OK — mas strings hardcoded encontradas (problema 10) |
| Lucide React | OK |
| Tailwind CSS v4 | OK |
| **Falta** | `react-virtual` ou `@tanstack/react-virtual` para virtualização de lista longa |

---

## Validação: PostgreSQL vs ClickHouse/Elasticsearch

### Decisões Correctas (PostgreSQL)

| Entidade | Justificação |
|---------|-------------|
| `ContractVersion` | Entidade transaccional — controlo de concorrência (xmin), referential integrity |
| `ServiceAsset` | Dados de governança estruturada — queries relacionais por team/domain |
| `ContractDraft` | Fluxo de aprovação com auditoria e rollback |
| `ContractNegotiation` | Workflow com estados e participantes |

### Candidatos para ClickHouse/Elasticsearch (não-relacional)

| Dado | Motivação |
|------|-----------|
| `ContractHealthScore` (histórico temporal) | Série temporal — melhor em ClickHouse para trend analysis |
| `PortalAnalyticsEvent` | Volume alto, append-only — ideal para ClickHouse |
| `NodeHealthRecord` | Métricas de saúde de nós — série temporal |
| `ContractVersionHistoryReader` | Read model analítico — Elasticsearch para pesquisa full-text |
| Dados de `ITrafficObservationReader` | Série temporal de tráfego — ClickHouse |

**Recomendação:** `ContractHealthScore` e `NodeHealthRecord` devem ter implementações reais em ClickHouse (além da persistência em PostgreSQL para o estado actual). Os null readers para estes dados devem ser priorizados para implementação real.

---

## Priorização de Correções

### Sprint 1 — Críticos (bloqueia produção)
1. ✅ Corrigir check constraint `ExposureType` (migration) — **1 hora**
2. ✅ Corrigir `ListLatestPerApiAssetAsync` — **3 horas**
3. ✅ Corrigir `GetSummaryAsync` — **2 horas**

### Sprint 2 — Alta prioridade
4. `ServiceAsset` → `AuditableEntity<T>` + migration de colunas de auditoria — **4 horas**
5. `CreateDraft` handler simplificação (remover re-query) — **1 hora**
6. `ListServices` + frontend: adicionar paginação — **1 dia**
7. `SearchAsync` + índice GIN — **3 horas**

### Sprint 3 — Qualidade e segurança
8. Validação `SpecContent` tamanho — **1 hora**
9. Defense-in-depth `TenantId` nos repositórios — **4 horas**
10. URL validation no frontend — **1 hora**
11. Remover `NullIDEUsageRepository.cs` (dead code) — **15 min**
12. OpenTelemetry spans nos handlers — **2 horas**
13. Actualizar testes após correcções — **4 horas**

---

## Notas Finais

O módulo Catalog demonstra forte aderência ao DDD e à arquitectura modular definida no CLAUDE.md. A separação entre bounded contexts (Graph, Contracts, Portal, etc.) é clara e consistente. A documentação XML em Português está presente na maioria das entidades e handlers.

Os problemas encontrados concentram-se principalmente em:
- **Performance de base de dados** — full table scans que passaram nos testes (dados pequenos) mas falharão em produção.
- **Consistência do domínio** — `ServiceAsset` sem herança de `AuditableEntity<T>` é a excepção mais significativa às convenções do projecto.
- **Um bug crítico de schema** — o mismatch da check constraint `ExposureType` é produção-breaking.

A qualidade geral do código é elevada. O padrão Honest-Null está correctamente aplicado para Readers. Os testes de domínio são sólidos. A correcção dos 3 problemas críticos e dos 2-3 problemas de alta prioridade pode ser feita em 2 dias de desenvolvimento focado.
