# Auditoria Ponta-a-Ponta — Módulo Knowledge

**Data:** 2026-05-14  
**Revisão:** Análise completa Backend + Frontend  
**Branch:** `claude/code-review-audit-i0rFs`  
**Scope:** `src/modules/knowledge/`, `tests/modules/knowledge/`, `src/frontend/src/features/knowledge/`  
**Referência:** CLAUDE.md, copilot-instructions.md (`.github/`)

---

## Índice

1. [Resumo Executivo](#1-resumo-executivo)
2. [Inventário de Ficheiros](#2-inventário-de-ficheiros)
3. [Problemas Críticos de Segurança](#3-problemas-críticos-de-segurança)
4. [Problemas de Domínio](#4-problemas-de-domínio)
5. [Problemas de Application Layer](#5-problemas-de-application-layer)
6. [Problemas de Infrastructure Layer](#6-problemas-de-infrastructure-layer)
7. [Problemas de API Endpoints](#7-problemas-de-api-endpoints)
8. [Problemas de Base de Dados e Mapeamento](#8-problemas-de-base-de-dados-e-mapeamento)
9. [Problemas de Frontend](#9-problemas-de-frontend)
10. [Problemas de Testes](#10-problemas-de-testes)
11. [Avaliação de Base de Dados (PostgreSQL vs ClickHouse/Elasticsearch)](#11-avaliação-de-base-de-dados)
12. [Avaliação de Bibliotecas e Boas Práticas 2026](#12-avaliação-de-bibliotecas-e-boas-práticas-2026)
13. [Conformidade com CLAUDE.md e copilot-instructions.md](#13-conformidade-com-claudemd-e-copilot-instructionsmd)
14. [Plano de Correções Priorizado](#14-plano-de-correções-priorizado)

---

## 1. Resumo Executivo

### Estado Geral

O módulo **Knowledge** tem uma estrutura arquitetural correta e bem organizada. A divisão em camadas Domain / Application / Contracts / Infrastructure / API está respeitada. O padrão CQRS com MediatR está implementado. Os 23 handlers cobrem os casos de uso centrais do Knowledge Hub.

No entanto, a auditoria identificou **problemas graves que impedem o uso seguro em produção**, com destaque para a ausência total de isolamento multi-tenant nos repositórios e entidades, e uma inconsistência crítica entre o enum `DocumentCategory` e o CHECK constraint da base de dados.

### Score por Dimensão

| Dimensão | Score | Notas |
|---|---|---|
| Estrutura de pastas e DDD | ✅ 9/10 | Muito boa organização |
| Padrão CQRS | ✅ 8/10 | Correto; pequenas inconsistências |
| Segurança multi-tenant | ❌ 1/10 | Ausência total de TenantId nas entidades e repositórios |
| Segurança de dados | ⚠️ 5/10 | AuthorId/EditorId from body; search bypassa pipeline |
| Performance | ⚠️ 4/10 | Múltiplos N+1 queries; FTS sem índice materializado |
| Integridade de dados | ❌ 3/10 | Enum vs DB constraint mismatch; unique constraint errado |
| Documentação de código | ✅ 8/10 | XML docs em português; bom nível geral |
| Testes | ⚠️ 6/10 | Boa cobertura de domínio; falha em cenários de tenant |
| Frontend | ⚠️ 5/10 | Funcional; UX e Markdown rendering em falta |
| Conformidade i18n | ✅ 8/10 | Maioria das strings usa useTranslation; alguns lapsos |
| Boas práticas 2026 | ✅ 7/10 | Stack moderna; lacunas em observabilidade e audit events |

---

## 2. Inventário de Ficheiros

### Backend — 63 ficheiros

#### Domain (7 entidades / 7 enums)
```
Domain/Entities/KnowledgeDocument.cs       — Agregado principal; sem TenantId
Domain/Entities/KnowledgeRelation.cs       — Relação entre knowledge e outros contextos; sem TenantId
Domain/Entities/KnowledgeGraphSnapshot.cs  — Snapshot do grafo; TenantId nullable
Domain/Entities/OperationalNote.cs         — Nota operacional; sem TenantId
Domain/Entities/ProposedRunbook.cs         — Runbook proposto; sem TenantId; enum mislocated
Domain/Enums/DocumentCategory.cs           — 13 valores (7 no DB constraint; mismatch crítico)
Domain/Enums/DocumentStatus.cs             — 4 valores (alinhado com DB)
Domain/Enums/RelationType.cs               — 7 valores (alinhado com DB)
Domain/Enums/KnowledgeSourceEntityType.cs  — 2 valores (alinhado com DB)
Domain/Enums/NoteSeverity.cs               — 3 valores (alinhado com DB)
Domain/Enums/OperationalNoteType.cs        — 5 valores (alinhado com DB)
Domain/Enums/KnowledgeGraphSnapshotStatus.cs — 3 valores (alinhado com DB)
```

#### Application (23 handlers + 6 abstrações)
```
Abstractions/IKnowledgeDocumentRepository.cs
Abstractions/IKnowledgeRelationRepository.cs
Abstractions/IOperationalNoteRepository.cs
Abstractions/IKnowledgeGraphSnapshotRepository.cs
Abstractions/IProposedRunbookRepository.cs
Abstractions/IKnowledgeBaseUtilizationReader.cs  — honest-null (correto)
Abstractions/ITeamKnowledgeSharingReader.cs       — honest-null (correto)
Features/CreateKnowledgeDocument/
Features/UpdateKnowledgeDocument/
Features/GetKnowledgeDocumentById/
Features/ListKnowledgeDocuments/
Features/CreateOperationalNote/
Features/UpdateOperationalNote/
Features/ListOperationalNotes/
Features/CreateKnowledgeRelation/
Features/GetKnowledgeRelationsBySource/
Features/GetKnowledgeByRelationTarget/
Features/BuildKnowledgeGraphSnapshot/
Features/GetKnowledgeGraphSnapshot/
Features/GetKnowledgeGraphOverview/
Features/ListKnowledgeGraphSnapshots/
Features/ValidateDocumentReviewGate/
Features/ScoreDocumentFreshness/
Features/GetFreshnessReport/
Features/GenerateAutoDocumentation/
Features/GetServiceOperationalTimeline/
Features/ProposeRunbookFromIncident/
Features/SearchAcrossModules/
Features/GetKnowledgeBaseUtilizationReport/
Features/GetTeamKnowledgeSharingReport/
NullKnowledgeBaseUtilizationReader.cs
NullTeamKnowledgeSharingReader.cs
```

#### Infrastructure (5 repositórios + search + services + 5 configs)
```
Persistence/KnowledgeDbContext.cs
Persistence/KnowledgeDbContextDesignTimeFactory.cs
Persistence/Repositories/KnowledgeDocumentRepository.cs
Persistence/Repositories/OperationalNoteRepository.cs
Persistence/Repositories/KnowledgeRelationRepository.cs
Persistence/Repositories/KnowledgeGraphSnapshotRepository.cs
Persistence/Repositories/ProposedRunbookRepository.cs
Persistence/Configurations/KnowledgeDocumentConfiguration.cs
Persistence/Configurations/OperationalNoteConfiguration.cs
Persistence/Configurations/KnowledgeRelationConfiguration.cs
Persistence/Configurations/KnowledgeGraphSnapshotConfiguration.cs
Persistence/Configurations/ProposedRunbookConfiguration.cs
Persistence/Migrations/20260511134525_InitialCreate.cs
Search/KnowledgeSearchProvider.cs
Search/RunbookKnowledgeLinkingService.cs
Services/KnowledgeModuleService.cs
DependencyInjection.cs
```

#### API (2 ficheiros)
```
Endpoints/KnowledgeEndpointModule.cs  — 15 endpoints mapeados
Endpoints/DependencyInjection.cs
```

#### Contracts (1 ficheiro, 3 interfaces públicas)
```
Contracts/KnowledgeContracts.cs
  IKnowledgeModule
  IKnowledgeSearchProvider
  IRunbookKnowledgeLinkingService
```

### Frontend — 11 ficheiros

```
features/knowledge/api/knowledge.ts            — API client (axios wrapper)
features/knowledge/pages/KnowledgeHubPage.tsx  — Página principal do hub
features/knowledge/pages/KnowledgeDocumentPage.tsx
features/knowledge/pages/KnowledgeGraphPage.tsx
features/knowledge/pages/OperationalNotesPage.tsx
features/knowledge/pages/AutoDocumentationPage.tsx
features/knowledge/pages/ServiceTimelinePage.tsx
features/knowledge/hooks/index.ts
features/knowledge/components/KnowledgeContextPanel.tsx
features/knowledge/index.ts
routes/knowledgeRoutes.tsx
```

### Testes — 18 ficheiros
```
Domain/KnowledgeDocumentTests.cs
Domain/KnowledgeRelationTests.cs
Domain/KnowledgeGraphSnapshotTests.cs
Domain/OperationalNoteTests.cs
Application/KnowledgeCrudFeatureTests.cs
Application/UpdateFeatureTests.cs
Application/CreateKnowledgeRelationFeatureTests.cs
Application/WaveAyKnowledgeIntelligenceTests.cs
Application/Features/BuildKnowledgeGraphSnapshotTests.cs
Application/Features/DocumentReviewGateTests.cs
Application/Features/GetKnowledgeGraphSnapshotTests.cs
Application/Features/GetServiceOperationalTimelineTests.cs
Application/Features/KnowledgeHubB4Tests.cs
Application/Features/KnowledgeIntelligenceTests.cs
Application/Features/ListKnowledgeGraphSnapshotsTests.cs
Infrastructure/KnowledgePersistenceAndSearchIntegrationTests.cs
Infrastructure/RunbookKnowledgeLinkingServiceTests.cs
GlobalUsings.cs
```

---

## 3. Problemas Críticos de Segurança

### P-SEC-01 — [CRÍTICO] Ausência total de TenantId nas entidades e repositórios

**Severidade:** CRÍTICA — BLOCKER para produção  
**Ficheiros afetados:**
- `Domain/Entities/KnowledgeDocument.cs` (linha 23)
- `Domain/Entities/OperationalNote.cs` (linha 27)
- `Domain/Entities/KnowledgeRelation.cs` (linha 24)
- `Domain/Entities/ProposedRunbook.cs` (linha 14)
- Todos os repositórios em `Infrastructure/Persistence/Repositories/`

**Problema:**  
As entidades `KnowledgeDocument`, `OperationalNote`, `KnowledgeRelation` e `ProposedRunbook` não têm campo `TenantId`. Os repositórios correspondentes não aplicam filtro por tenant em nenhuma operação de leitura.

```csharp
// ATUAL — sem filtro de tenant:
public async Task<KnowledgeDocument?> GetByIdAsync(KnowledgeDocumentId id, CancellationToken ct)
    => await context.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);

// CORRETO — com filtro de tenant:
public async Task<KnowledgeDocument?> GetByIdAsync(KnowledgeDocumentId id, CancellationToken ct)
    => await context.KnowledgeDocuments
        .Where(d => d.TenantId == currentTenant.Id)
        .FirstOrDefaultAsync(d => d.Id == id, ct);
```

**Impacto:**  
Qualquer utilizador autenticado pode ler, pesquisar e listar documentos de conhecimento, notas operacionais e relações de QUALQUER tenant. A `TenantRlsInterceptor` configura `app.current_tenant_id` no PostgreSQL mas não existem RLS policies nas tabelas `knw_*`, o que significa que a segunda linha de defesa (RLS a nível da base de dados) também está ausente.

**Correção:**  
1. Adicionar `TenantId` a todas as entidades:
```csharp
public Guid TenantId { get; private init; }
```
2. Adicionar filtro por tenant em todos os repositórios (injetar `ICurrentTenant`):
```csharp
internal sealed class KnowledgeDocumentRepository(
    KnowledgeDbContext context,
    ICurrentTenant currentTenant) : IKnowledgeDocumentRepository
```
3. Adicionar migration com a nova coluna e índices.
4. Configurar políticas RLS no PostgreSQL para as tabelas `knw_*`.

---

### P-SEC-02 — [ALTO] AuthorId e EditorId recebidos do corpo do request

**Severidade:** Alta  
**Ficheiros afetados:**
- `Application/Features/CreateKnowledgeDocument/CreateKnowledgeDocument.cs` (linha 16)
- `Application/Features/UpdateKnowledgeDocument/UpdateKnowledgeDocument.cs` (linha 19)
- `Application/Features/CreateOperationalNote/CreateOperationalNote.cs`

**Problema:**  
Os comandos `CreateKnowledgeDocument.Command` e `UpdateKnowledgeDocument.Command` aceitam `AuthorId` e `EditorId` no corpo do request, permitindo que qualquer utilizador autenticado se faça passar por outro utilizador ao criar ou editar documentos.

```csharp
// ATUAL — AuthorId do body:
public sealed record Command(
    string Title, string Content, string? Summary,
    DocumentCategory Category, IReadOnlyList<string>? Tags,
    Guid AuthorId) : ICommand<Response>;

// CORRETO — AuthorId do ICurrentUser:
public sealed record Command(
    string Title, string Content, string? Summary,
    DocumentCategory Category,
    IReadOnlyList<string>? Tags) : ICommand<Response>;

internal sealed class Handler(
    IKnowledgeDocumentRepository documentRepository,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IDateTimeProvider clock) : ICommandHandler<Command, Response>
{
    public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
    {
        var document = KnowledgeDocument.Create(
            request.Title, request.Content, request.Summary,
            request.Category, request.Tags,
            currentUser.Id,    // ← de ICurrentUser, não do body
            currentTenant.Id,  // ← de ICurrentTenant
            clock.UtcNow);
        // ...
    }
}
```

---

### P-SEC-03 — [ALTO] Endpoint de search bypassa o pipeline MediatR

**Severidade:** Alta  
**Ficheiro afetado:** `API/Endpoints/KnowledgeEndpointModule.cs` (linhas 54-76)

**Problema:**  
O endpoint `GET /api/v1/knowledge/search` chama diretamente `IKnowledgeSearchProvider` em vez de usar `ISender` do MediatR. Isto significa que os seguintes behaviors são **completamente ignorados**:
- `LoggingBehavior` — sem logging estruturado
- `PerformanceBehavior` — sem monitorização de performance
- `TenantIsolationBehavior` — **sem validação de tenant** (crítico)
- `ValidationBehavior` — sem validação de FluentValidation
- `TransactionBehavior` — não aplicável a query mas demonstra o padrão incorreto

```csharp
// ATUAL — bypassa pipeline:
knowledge.MapGet("/search", async (
    string q, string? scope, int? maxResults,
    [FromServices] IKnowledgeSearchProvider searchProvider,
    CancellationToken cancellationToken) =>
{
    var results = await searchProvider.SearchAsync(q, scope, max, cancellationToken);
    ...
})

// CORRETO — usa MediatR pipeline:
knowledge.MapGet("/search", async (
    string q, string? scope, int? maxResults,
    ISender sender, IErrorLocalizer localizer,
    CancellationToken cancellationToken) =>
{
    var result = await sender.Send(
        new SearchAcrossModulesFeature.Query(q, maxResults ?? 25),
        cancellationToken);
    return result.ToHttpResult(localizer);
})
```

---

### P-SEC-04 — [MÉDIO] RowVersion com setter público

**Severidade:** Média  
**Ficheiros afetados:**
- `Domain/Entities/KnowledgeDocument.cs` (linha 65)
- `Domain/Entities/OperationalNote.cs` (linha 75)

**Problema:**  
O campo `RowVersion` tem setter público (`public uint RowVersion { get; set; }`). O setter deveria ser `internal set` (usado apenas pelo EF Core) para proteger o invariante de concorrência. Um setter público permite alteração acidental em código de aplicação.

**Correção:**
```csharp
// ATUAL:
public uint RowVersion { get; set; }

// CORRETO:
public uint RowVersion { get; internal set; }
```

---

## 4. Problemas de Domínio

### P-DOM-01 — [CRÍTICO] Enum DocumentCategory incompatível com CHECK constraint da BD

**Severidade:** Crítica — BLOCKER  
**Ficheiros afetados:**
- `Domain/Enums/DocumentCategory.cs`
- `Infrastructure/Persistence/Configurations/KnowledgeDocumentConfiguration.cs` (linha 20-24)
- `Infrastructure/Persistence/Migrations/20260511134525_InitialCreate.cs`

**Problema:**  
O enum `DocumentCategory` tem **13 valores** mas o CHECK constraint da base de dados apenas aceita **7 valores originais**:

```csharp
// Enum tem 13 valores (Domain):
public enum DocumentCategory
{
    General, Runbook, Troubleshooting, Architecture,
    Procedure, PostMortem, Reference,
    ApiDocumentation,    // ← Novo — não está no CHECK constraint
    ChangeLog,           // ← Novo — não está no CHECK constraint
    ComplianceEvidence,  // ← Novo — não está no CHECK constraint
    DecisionRecord,      // ← Novo — não está no CHECK constraint
    IncidentAnalysis,    // ← Novo — não está no CHECK constraint
    OperationalPlaybook  // ← Novo — não está no CHECK constraint
}

// DB CHECK constraint aceita apenas 7:
"\"Category\" IN ('General','Runbook','Troubleshooting','Architecture','Procedure','PostMortem','Reference')"
```

**Impacto:**  
Qualquer tentativa de criar um documento com uma das 6 novas categorias irá resultar em **violação de CHECK constraint** e falha na transação.

**Correção:**  
Atualizar o CHECK constraint via nova migration:
```csharp
migrationBuilder.Sql(@"
    ALTER TABLE knw_documents
    DROP CONSTRAINT IF EXISTS ""CK_knw_documents_category"";
    ALTER TABLE knw_documents
    ADD CONSTRAINT ""CK_knw_documents_category""
    CHECK (""Category"" IN (
        'General','Runbook','Troubleshooting','Architecture',
        'Procedure','PostMortem','Reference',
        'ApiDocumentation','ChangeLog','ComplianceEvidence',
        'DecisionRecord','IncidentAnalysis','OperationalPlaybook'));");
```

---

### P-DOM-02 — [ALTO] KnowledgeDocument herda Entity mas deveria herdar AuditableEntity

**Severidade:** Alta  
**Ficheiro afetado:** `Domain/Entities/KnowledgeDocument.cs` (linha 23)

**Problema:**  
`KnowledgeDocument` herda `Entity<KnowledgeDocumentId>` em vez de `AuditableEntity<KnowledgeDocumentId>`. Isto significa:
- **Soft delete não está ativo** — registos apagados ficam na base de dados sem filtro automático
- `CreatedBy`, `UpdatedBy` geridos manualmente (em vez de pelo `AuditInterceptor`)
- Campos duplicados em relação ao padrão do sistema

Por contrapartida, `KnowledgeGraphSnapshot` herda corretamente `AuditableEntity`.

**Nota:** A entidade já gere `CreatedAt` e `UpdatedAt` manualmente no domínio. A migração para `AuditableEntity` requer análise cuidadosa para não duplicar colunas.

**Correção proposta:**
```csharp
// ATUAL:
public sealed class KnowledgeDocument : Entity<KnowledgeDocumentId>

// CORRETO:
public sealed class KnowledgeDocument : AuditableEntity<KnowledgeDocumentId>
// Remover campos: CreatedAt, UpdatedAt (geridos pelo AuditInterceptor)
// Adicionar: IsDeleted (gerido pelo AuditInterceptor)
// Manter: PublishedAt, Version (campos de negócio específicos)
```

---

### P-DOM-03 — [ALTO] Unique constraint em KnowledgeRelation bloqueia múltiplas relações

**Severidade:** Alta  
**Ficheiro afetado:** `Infrastructure/Persistence/Configurations/KnowledgeRelationConfiguration.cs` (linha 64)

**Problema:**  
O índice único `(SourceEntityId, TargetEntityId)` impede que a mesma entidade de origem tenha mais de UMA relação com a mesma entidade de destino, independentemente do tipo (`RelationType`). Isto bloqueia cenários válidos como um documento que é ao mesmo tempo `Runbook` e `Service` para a mesma entidade.

```csharp
// ATUAL — demasiado restritivo:
builder.HasIndex(x => new { x.SourceEntityId, x.TargetEntityId }).IsUnique();

// CORRETO — incluir o tipo na unicidade:
builder.HasIndex(x => new { x.SourceEntityId, x.TargetEntityId, x.TargetType }).IsUnique();
```

---

### P-DOM-04 — [MÉDIO] ProposedRunbookStatus definido no ficheiro da entidade

**Severidade:** Média (organização)  
**Ficheiro afetado:** `Domain/Entities/ProposedRunbook.cs` (linha 7)

**Problema:**  
`public enum ProposedRunbookStatus { Proposed, UnderReview, Approved, Rejected }` está definido diretamente no ficheiro da entidade, violando a convenção do projeto onde todos os enums do domínio residem em `Domain/Enums/`.

**Correção:**  
Mover para `Domain/Enums/ProposedRunbookStatus.cs`.

---

### P-DOM-05 — [MÉDIO] ProposedRunbook sem documentação XML completa

**Severidade:** Média  
**Ficheiro afetado:** `Domain/Entities/ProposedRunbook.cs`

**Problema:**  
As propriedades públicas de `ProposedRunbook` não têm XML doc comments (`/// <summary>`), violando as diretrizes do projeto que exigem documentação de todas as propriedades públicas de entidades.

**Correção:**  
Adicionar `/// <summary>...</summary>` em cada propriedade pública.

---

### P-DOM-06 — [MÉDIO] KnowledgeGraphSnapshot.TenantId é nullable mas devia ser obrigatório

**Severidade:** Média  
**Ficheiro afetado:** `Domain/Entities/KnowledgeGraphSnapshot.cs` (linha 70)

**Problema:**  
`public Guid? TenantId { get; private set; }` é nullable, enquanto a configuração EF (`KnowledgeGraphSnapshotConfiguration`) declara `.IsRequired()`. Esta inconsistência cria confusão e potencialmente erros de runtime.

**Correção:**
```csharp
// ATUAL (Domain):
public Guid? TenantId { get; private set; }

// CORRETO (Domain):
public Guid TenantId { get; private init; }
```

---

### P-DOM-07 — [BAIXO] KnowledgeDocumentId sem métodos New() e From()

**Severidade:** Baixa  
**Ficheiro afetado:** `Domain/Entities/KnowledgeDocument.cs` (linha 12)

**Problema:**  
`KnowledgeDocumentId`, `KnowledgeRelationId` e `OperationalNoteId` não implementam os métodos `New()` e `From(Guid)` presentes em todos os outros IDs do sistema (ex: `KnowledgeGraphSnapshotId`, `ProposedRunbookId`).

**Correção:**
```csharp
public sealed record KnowledgeDocumentId(Guid Value) : TypedIdBase(Value)
{
    public static KnowledgeDocumentId New() => new(Guid.NewGuid());
    public static KnowledgeDocumentId From(Guid id) => new(id);
}
```

---

### P-DOM-08 — [BAIXO] Comentários em inglês no método GenerateSlug

**Severidade:** Baixa  
**Ficheiro afetado:** `Domain/Entities/KnowledgeDocument.cs` (linhas 194-209)

**Problema:**  
Os comentários inline em `GenerateSlug` estão em inglês ("Replace spaces and common separators with dashes"), violando a convenção do projeto que exige comentários inline em português.

---

### P-DOM-09 — [MÉDIO] KnowledgeDocument sem Domain Events

**Severidade:** Média  
**Ficheiro afetado:** `Domain/Entities/KnowledgeDocument.cs`

**Problema:**  
Operações semanticamente importantes como `Publish()`, `Archive()`, `MarkReviewed()` não geram Domain Events. Outros módulos (ex: AIKnowledge para indexação RAG, AuditCompliance para auditoria) não conseguem reagir a estas mudanças via Outbox.

**Correção:**  
Criar domain events e publicar via `RaiseDomainEvent()`:
```csharp
// Novos ficheiros:
// Domain/Events/KnowledgeDocumentPublishedEvent.cs
// Domain/Events/KnowledgeDocumentReviewedEvent.cs

public void Publish(DateTimeOffset utcNow)
{
    Status = DocumentStatus.Published;
    PublishedAt = utcNow;
    UpdatedAt = utcNow;
    RaiseDomainEvent(new KnowledgeDocumentPublishedEvent(Id.Value, TenantId, utcNow));
}
```

---

## 5. Problemas de Application Layer

### P-APP-01 — [ALTO] GetFreshnessReport muta estado de entidade em handler de Query

**Severidade:** Alta  
**Ficheiro afetado:** `Application/Features/GetFreshnessReport/GetFreshnessReport.cs` (linha 24)

**Problema:**  
O handler de query `GetFreshnessReport` chama `d.ComputeFreshnessScore(now)` sobre cada documento, o que **muta o estado da entidade** em memória. Um handler de Query não deve ter side effects — a mutação não é persistida, mas altera o estado do EF Core change tracker desnecessariamente.

```csharp
// ATUAL — muta entidade num query handler:
var items = docs.Select(d => {
    d.ComputeFreshnessScore(now);  // ← side effect num Query!
    return new FreshnessItemDto(...);
}).ToList();

// CORRETO — computar sem mutar:
var items = docs.Select(d => {
    var referenceDate = d.LastReviewedAt ?? d.UpdatedAt ?? d.CreatedAt;
    var daysSinceReview = (now - referenceDate).TotalDays;
    var score = Math.Max(0, (int)Math.Round(100 - daysSinceReview / 180.0 * 100));
    return new FreshnessItemDto(d.Id.Value.ToString(), d.Title,
        d.Category.ToString(), score, d.LastReviewedAt,
        score >= 80 ? "Fresh" : score >= 50 ? "Aging" : "Stale");
}).ToList();
```

**Alternativa:** Mover a lógica de score para um método estático puro (não membro da entidade) ou usar um Value Object.

---

### P-APP-02 — [ALTO] SearchAcrossModules carrega todos os runbooks em memória

**Severidade:** Alta  
**Ficheiro afetado:** `Application/Features/SearchAcrossModules/SearchAcrossModules.cs` (linha 38)

**Problema:**  
```csharp
var runbooks = await runbookRepo.ListAsync(serviceName: null, ct: cancellationToken);
var matchingRunbooks = runbooks
    .Where(r => r.Title.Contains(request.Term, StringComparison.OrdinalIgnoreCase)
        || r.ContentMarkdown.Contains(request.Term, StringComparison.OrdinalIgnoreCase))
    .Take(maxPerSource)
    .ToList();
```

`ListAsync` sem filtro carrega TODOS os runbooks de TODOS os tenants para memória e depois filtra em C#. A `ContentMarkdown` pode ser muito longa. Isto é um problema grave de memória e performance.

**Correção:**  
Adicionar `SearchAsync(string term, int maxResults, CancellationToken ct)` ao `IProposedRunbookRepository` com FTS PostgreSQL.

---

### P-APP-03 — [MÉDIO] BuildKnowledgeGraphSnapshot chama CommitAsync diretamente (inconsistência)

**Severidade:** Média  
**Ficheiro afetado:** `Application/Features/BuildKnowledgeGraphSnapshot/BuildKnowledgeGraphSnapshot.cs` (linha 83)

**Problema:**  
Este handler chama `await unitOfWork.CommitAsync(cancellationToken)` explicitamente, enquanto todos os outros handlers do projeto confiam no `TransactionBehavior` do pipeline MediatR. Esta inconsistência pode causar dupla commit ou comportamento inesperado se o `TransactionBehavior` também executar.

**Correção:**  
Remover `unitOfWork.CommitAsync()` do handler e depender exclusivamente do `TransactionBehavior`, ou documentar explicitamente por que este handler é uma exceção (ex: se `IPublicRequest`, que não tem transaction behavior).

---

### P-APP-04 — [MÉDIO] ScoreDocumentFreshness.Command usa string em vez de Guid

**Severidade:** Média  
**Ficheiro afetado:** `Application/Features/ScoreDocumentFreshness/ScoreDocumentFreshness.cs` (linha 17)

**Problema:**  
```csharp
public sealed record Command(string DocumentId) : ICommand<Response>;
```

O `DocumentId` é string quando deveria ser `Guid`, consistente com todos os outros comandos do módulo. Requer parse manual com tratamento de erro.

**Correção:**
```csharp
public sealed record Command(Guid DocumentId) : ICommand<Response>;
// Handler: var doc = await repo.GetByIdAsync(new KnowledgeDocumentId(request.DocumentId), cancellationToken);
```

---

### P-APP-05 — [MÉDIO] BuildKnowledgeGraphSnapshot não marca snapshots "Reviewed" como Stale

**Severidade:** Média  
**Ficheiro afetado:** `Application/Features/BuildKnowledgeGraphSnapshot/BuildKnowledgeGraphSnapshot.cs` (linha 62)

**Problema:**  
```csharp
if (previousSnapshot is not null && previousSnapshot.Status == KnowledgeGraphSnapshotStatus.Generated)
{
    previousSnapshot.MarkAsStale();
}
```

Apenas snapshots com status `Generated` são marcados `Stale`. Snapshots com status `Reviewed` nunca são invalidados, mesmo que já exista um mais recente.

**Correção:**
```csharp
if (previousSnapshot is not null
    && previousSnapshot.Status != KnowledgeGraphSnapshotStatus.Stale)
{
    previousSnapshot.MarkAsStale();
    snapshotRepository.Update(previousSnapshot);
}
```

---

### P-APP-06 — [MÉDIO] GenerateAutoDocumentation retorna texto estático hardcoded

**Severidade:** Média  
**Ficheiro afetado:** `Application/Features/GenerateAutoDocumentation/GenerateAutoDocumentation.cs`

**Problema:**  
A maioria das secções de documentação gerada são texto estático hardcoded que não reflete dados reais do serviço:

```csharp
"Ownership" => new DocSection(
    "Ownership",
    "## Ownership\n\nOwnership is managed in the Service Catalog...",
    "Service Catalog"),
```

Além disso, o texto está em inglês, violando as regras de i18n (deve usar chaves de i18n ou separar texto da lógica).

**Impacto:**  
Esta feature está funcional como esqueleto mas não entrega valor real. Um utilizador que chame `/api/v1/knowledge/auto-documentation/my-service` recebe texto genérico sobre ownership que não é específico do serviço pedido.

**Correção sugerida:**
- Integrar com `IKnowledgeRelationRepository` para buscar relações reais do serviço
- Integrar com módulo Catalog para ownership real (via `ICatalogModule`)
- Usar chaves de i18n para texto de UI
- Documentar claramente que a feature está em estado preview/skeleton

---

### P-APP-07 — [BAIXO] GetFreshnessReport sem Validator

**Severidade:** Baixa  
**Ficheiro afetado:** `Application/Features/GetFreshnessReport/GetFreshnessReport.cs`

**Problema:**  
`GetFreshnessReport.Query` não tem `Validator` class, ao contrário de todos os outros queries/commands do módulo. Mesmo que os parâmetros sejam simples, a consistência do padrão deve ser mantida.

---

### P-APP-08 — [BAIXO] Missing capability check (SaaS licensing)

**Severidade:** Baixa  
**Ficheiros afetados:** Todos os handlers de escrita do módulo

**Problema:**  
Nenhum handler verifica capabilities de licença do tenant (ex: `currentTenant.HasCapability("knowledge_hub")`). Conforme CLAUDE.md, handlers de features premium devem verificar capabilities.

---

## 6. Problemas de Infrastructure Layer

### P-INF-01 — [ALTO] N+1 Query em KnowledgeSearchProvider

**Severidade:** Alta  
**Ficheiro afetado:** `Infrastructure/Search/KnowledgeSearchProvider.cs` (linha 36)

**Problema:**  
Para cada resultado de documentos e notas encontrados, o `BuildRelationContextAsync` executa uma query adicional:

```csharp
foreach (var doc in documents)
{
    var score = CalculateRelevance(searchTerm, doc.Title, doc.Summary);
    var relationContext = await BuildRelationContextAsync(doc.Id.Value, cancellationToken);
    // ↑ Uma query por resultado → N+1
}
```

Com 25 resultados, são executadas até 50 queries adicionais (25 para documentos + 25 para notas).

**Correção:**  
Fazer uma única query para todas as relações dos resultados:

```csharp
var docIds = documents.Select(d => d.Id.Value).ToList();
var noteIds = notes.Select(n => n.Id.Value).ToList();
var allIds = docIds.Concat(noteIds).ToList();
var allRelations = await relationRepository.ListBySourceIdsAsync(allIds, cancellationToken);
var relationsBySource = allRelations.GroupBy(r => r.SourceEntityId)
    .ToDictionary(g => g.Key, g => g.ToList());
```

Requer novo método `ListBySourceIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)` na interface e repositório.

---

### P-INF-02 — [ALTO] N+1 Query em GetKnowledgeGraphOverview.BuildGlobalOverviewAsync

**Severidade:** Alta  
**Ficheiro afetado:** `Application/Features/GetKnowledgeGraphOverview/GetKnowledgeGraphOverview.cs` (linha 122)

**Problema:**  
```csharp
var (documents, _) = await documentRepository.ListAsync(null, null, 1, 500, cancellationToken);
foreach (var doc in documents)
{
    var relations = await relationRepository.ListBySourceAsync(doc.Id.Value, cancellationToken);
    // ↑ 500 queries adicionais no pior caso!
}
```

Com 500 documentos, são executadas 501 queries SQL.

**Correção:**  
Adicionar método `ListAllAsync(CancellationToken ct)` em `IKnowledgeRelationRepository` que retorna todas as relações de uma vez, e fazer join em memória.

---

### P-INF-03 — [ALTO] N+1 Query em RunbookKnowledgeLinkingService.HasExistingRunbookServiceLinkAsync

**Severidade:** Alta  
**Ficheiro afetado:** `Infrastructure/Search/RunbookKnowledgeLinkingService.cs` (linha 77)

**Problema:**  
```csharp
var relations = await relationRepository.ListByTargetAsync(RelationType.Service, serviceId, ct);
foreach (var relation in relations.Where(x => x.SourceEntityType == KnowledgeSourceEntityType.OperationalNote))
{
    var note = await noteRepository.GetByIdAsync(new OperationalNoteId(relation.SourceEntityId), ct);
    // ↑ N queries para N relações
}
```

**Correção:**  
Verificar diretamente se existe nota com `ContextType == "Runbook"` e `ContextEntityId == runbookId`:
```csharp
var noteIds = relations
    .Where(x => x.SourceEntityType == KnowledgeSourceEntityType.OperationalNote)
    .Select(x => x.SourceEntityId)
    .ToList();
// Uma única query:
var existingNote = await noteRepository.FindByContextAsync(
    contextType: "Runbook",
    contextEntityId: runbookId,
    cancellationToken);
return existingNote is not null;
```

---

### P-INF-04 — [ALTO] FTS sem índice materializado (GIN)

**Severidade:** Alta  
**Ficheiros afetados:**
- `Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs` (linha 32-46)
- `Infrastructure/Persistence/Repositories/OperationalNoteRepository.cs` (linha 30-43)

**Problema:**  
```csharp
.Select(d => new {
    Document = d,
    SearchVector = EF.Functions.ToTsVector("simple",
        (d.Title ?? string.Empty) + " " +
        (d.Summary ?? string.Empty) + " " +
        (d.Content ?? string.Empty))
})
```

O `tsvector` é **computado por linha em cada query** (full table scan). Sem coluna materializada e índice GIN, cada pesquisa varre toda a tabela. Para uma tabela com >1000 documentos, a performance degrada significativamente.

**Correção:**  
Adicionar coluna `tsvector` materializada e índice GIN via migration:
```sql
ALTER TABLE knw_documents ADD COLUMN search_vector tsvector
    GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(Title, '') || ' ' ||
            coalesce(Summary, '') || ' ' || Content)
    ) STORED;
CREATE INDEX idx_knw_documents_search_vector ON knw_documents USING GIN (search_vector);
```

---

### P-INF-05 — [MÉDIO] RunbookKnowledgeLinkingService injeta KnowledgeDbContext diretamente

**Severidade:** Média  
**Ficheiro afetado:** `Infrastructure/Search/RunbookKnowledgeLinkingService.cs` (linha 16)

**Problema:**  
```csharp
internal sealed class RunbookKnowledgeLinkingService(
    IOperationalNoteRepository noteRepository,
    IKnowledgeRelationRepository relationRepository,
    KnowledgeDbContext unitOfWork,  // ← Concrete type, não interface
    IDateTimeProvider clock) : IRunbookKnowledgeLinkingService
```

Deveria injetar `IUnitOfWork` em vez de `KnowledgeDbContext` diretamente, para respeitar o princípio de Inversão de Dependência.

**Correção:**
```csharp
internal sealed class RunbookKnowledgeLinkingService(
    IOperationalNoteRepository noteRepository,
    IKnowledgeRelationRepository relationRepository,
    IUnitOfWork unitOfWork,  // ← interface
    IDateTimeProvider clock) : IRunbookKnowledgeLinkingService
```

---

### P-INF-06 — [MÉDIO] KnowledgeModuleService.CountDocumentsByServiceAsync conta relações erradas

**Severidade:** Média  
**Ficheiro afetado:** `Infrastructure/Services/KnowledgeModuleService.cs` (linha 47)

**Problema:**  
```csharp
return await context.KnowledgeRelations
    .AsNoTracking()
    .CountAsync(r => r.TargetEntityId == serviceGuid, cancellationToken);
```

Esta query conta TODAS as relações cujo `TargetEntityId` é igual ao `serviceGuid`, independentemente do `TargetType`. Um documento ligado a um incidente com o mesmo GUID seria erroneamente contado como documento do serviço. Deveria filtrar também por `TargetType == RelationType.Service`.

**Correção:**
```csharp
return await context.KnowledgeRelations
    .AsNoTracking()
    .CountAsync(r => r.TargetEntityId == serviceGuid
        && r.TargetType == RelationType.Service, cancellationToken);
```

---

### P-INF-07 — [BAIXO] Hardcoded SystemAuthorId como magic constant

**Severidade:** Baixa  
**Ficheiro afetado:** `Infrastructure/Search/RunbookKnowledgeLinkingService.cs` (linha 22)

**Problema:**  
```csharp
private static readonly Guid SystemAuthorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
```

Este magic GUID deve ser centralizado numa constante partilhada (ex: `BuildingBlocks.Core.Constants.SystemIdentities.SystemUserId`) para ser reutilizável por outros módulos que precisem de identificar ações do sistema.

---

### P-INF-08 — [MÉDIO] Tags não são incluídas no FTS

**Severidade:** Média  
**Ficheiro afetado:** `Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs`

**Problema:**  
O campo `Tags` (armazenado como JSONB) não é incluído no vector de pesquisa FTS. Um utilizador que pesquise por uma tag específica não encontrará documentos cujo único match seja nas tags.

**Correção:**  
Incluir `Tags` (desserializado) no tsvector, ou adicionar pesquisa separada por tags via operadores JSONB:
```sql
WHERE Tags @> '["runbook"]'::jsonb
```

---

## 7. Problemas de API Endpoints

### P-API-01 — [ALTO] Múltiplos features sem endpoint HTTP

**Severidade:** Alta  
**Ficheiro afetado:** `API/Endpoints/KnowledgeEndpointModule.cs`

**Problema:**  
Os seguintes features Application têm handlers implementados mas **sem endpoint HTTP correspondente**:

| Feature | Handler | Endpoint |
|---|---|---|
| `ScoreDocumentFreshness` | ✅ Existe | ❌ Ausente |
| `GetFreshnessReport` | ✅ Existe | ❌ Ausente |
| `ValidateDocumentReviewGate` | ✅ Existe | ❌ Ausente |
| `GetKnowledgeBaseUtilizationReport` | ✅ Existe | ❌ Ausente |
| `ListKnowledgeGraphSnapshots` | ✅ Existe | ❌ Ausente |
| `GetKnowledgeGraphSnapshot` | ✅ Existe | ❌ Ausente |
| `BuildKnowledgeGraphSnapshot` | ✅ Existe | ❌ Ausente |
| `ProposeRunbookFromIncident` | ✅ Existe | ❌ Ausente |
| `SearchAcrossModules` | ✅ Existe | ⚠️ Parcial (search usa IKnowledgeSearchProvider diretamente) |

---

### P-API-02 — [MÉDIO] Ausência de endpoints DELETE

**Severidade:** Média  
**Ficheiro afetado:** `API/Endpoints/KnowledgeEndpointModule.cs`

**Problema:**  
Não existem endpoints para eliminar/arquivar documentos ou notas:
- `DELETE /api/v1/knowledge/documents/{documentId}` — ausente
- `DELETE /api/v1/knowledge/operational-notes/{noteId}` — ausente
- `DELETE /api/v1/knowledge/relations/{relationId}` — ausente

O domínio suporta `Archive()` e `Deprecate()` em documentos, mas não existe endpoint para invocar estas transições de estado.

---

### P-API-03 — [MÉDIO] Parsing de enums a partir de query string sem fallback adequado

**Severidade:** Média  
**Ficheiro afetado:** `API/Endpoints/KnowledgeEndpointModule.cs` (linha 170, 215)

**Problema:**  
```csharp
if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<DocumentCategory>(category, true, out var cat))
    parsedCategory = cat;
```

Se o utilizador passa um valor inválido (ex: `category=InvalidValue`), o filtro é silenciosamente ignorado e a listagem retorna sem filtro. Seria preferível retornar um erro 400 com mensagem clara.

---

### P-API-04 — [BAIXO] Endpoint /status retorna versão hardcoded

**Severidade:** Baixa  
**Ficheiro afetado:** `API/Endpoints/KnowledgeEndpointModule.cs` (linha 48)

**Problema:**  
```csharp
Results.Ok(new { module = "Knowledge", status = "active", version = "10.3" })
```

A versão `"10.3"` está hardcoded e ficará desatualizada. Deve ser derivada do assembly ou de uma constante centralizada.

---

## 8. Problemas de Base de Dados e Mapeamento

### P-DB-01 — [ALTO] Colunas de auditoria AuditableEntity não configuradas em KnowledgeGraphSnapshotConfiguration

**Severidade:** Alta  
**Ficheiro afetado:** `Infrastructure/Persistence/Configurations/KnowledgeGraphSnapshotConfiguration.cs`

**Problema:**  
`KnowledgeGraphSnapshot` herda `AuditableEntity<KnowledgeGraphSnapshotId>`, que tem as propriedades `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`. A configuração EF não menciona estas colunas explicitamente. O EF Core pode inferi-las, mas a ausência de configuração explícita:
- Não garante o tipo correto das colunas (`timestamp with time zone`)
- Não garante o índice no campo `IsDeleted` para o soft-delete filter

---

### P-DB-02 — [MÉDIO] ProposedRunbookConfiguration sem índices de suporte

**Severidade:** Média  
**Ficheiro afetado:** `Infrastructure/Persistence/Configurations/ProposedRunbookConfiguration.cs`

**Problema:**  
Apenas existe o índice `uix_knw_proposed_runbooks_incident` em `SourceIncidentId`. Faltam:
- Índice em `Status` (para listar por estado de aprovação)
- Índice em `ServiceName` (para listar runbooks por serviço)
- Índice em `ProposedAt` (para ordenação)

---

### P-DB-03 — [BAIXO] Conversão manual de Tags em vez de EF Core JsonSerializerOptions tipadas

**Severidade:** Baixa  
**Ficheiros afetados:**
- `KnowledgeDocumentConfiguration.cs` (linha 55-58)
- `OperationalNoteConfiguration.cs` (linha 59-63)

**Problema:**  
```csharp
.HasConversion(
    tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
    json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
```

A passagem de `null` como `JsonSerializerOptions` usa as opções padrão do sistema, o que pode produzir resultados diferentes em diferentes contextos. Deveria usar uma instância estática de `JsonSerializerOptions` tipada e consistente.

---

### P-DB-04 — [ANÁLISE] Avaliação da escolha PostgreSQL para Knowledge

**Veredicto:** ✅ CORRETO — PostgreSQL é a escolha certa para todas as entidades do módulo Knowledge

**Justificação:**  
Todas as entidades do módulo são **dados de domínio transacionais**:
- `KnowledgeDocument`, `OperationalNote`, `KnowledgeRelation`, `ProposedRunbook` — documentos estruturados, relações, lifecycle states. PostgreSQL correto.
- `KnowledgeGraphSnapshot` — snapshot persistido de métricas calculadas. PostgreSQL correto.

**O que deveria ir para ClickHouse/Elasticsearch:**  
- `IKnowledgeBaseUtilizationReader` (atualmente honest-null) — eventos de pesquisa (search analytics), acessos a documentos, padrões de utilização. Estes são dados **time-series de alta volumetria** que se encaixam perfeitamente em ClickHouse (analytics) ou Elasticsearch (full-text + analytics).
- A implementação real deste reader deve ser um adaptador que consulta Elasticsearch/ClickHouse onde os eventos de pesquisa são ingeridos via `NexTraceOne.Ingestion.Api`.

---

## 9. Problemas de Frontend

### P-FE-01 — [ALTO] AuthorId exibido como UUID bruto

**Severidade:** Alta  
**Ficheiro afetado:** `src/frontend/src/features/knowledge/pages/KnowledgeDocumentPage.tsx` (linha 105)

**Problema:**  
```tsx
<dd className="text-content-primary ml-auto text-right text-xs font-mono truncate max-w-[120px]">
  {document.authorId}
</dd>
```

O `authorId` é exibido como UUID diretamente. As diretrizes proíbem explicitamente expor GUIDs ao utilizador final: *"Não pedir ao utilizador para introduzir GUIDs/IDs técnicos"* (copilot-instructions.md, §18.4).

**Correção:**  
O backend deve retornar o nome/email do autor (resolvido via Identity module ou incluído no response). A curto prazo, omitir ou substituir por "–" até o backend fornecer o dado.

---

### P-FE-02 — [ALTO] Conteúdo Markdown renderizado como texto plano

**Severidade:** Alta  
**Ficheiro afetado:** `src/frontend/src/features/knowledge/pages/KnowledgeDocumentPage.tsx` (linha 79)

**Problema:**  
```tsx
<pre className="whitespace-pre-wrap font-sans text-sm text-content-primary leading-relaxed">
  {document.content}
</pre>
```

O conteúdo é documentado como Markdown mas renderizado como `<pre>` — o utilizador vê a formatação raw (`##`, `**bold**`, etc.) em vez do Markdown renderizado.

**Correção:**  
Usar biblioteca de renderização Markdown (ex: `react-markdown` + `remark-gfm`):
```tsx
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

<div className="prose prose-invert prose-sm max-w-none">
  <ReactMarkdown remarkPlugins={[remarkGfm]}>
    {document.content}
  </ReactMarkdown>
</div>
```

---

### P-FE-03 — [ALTO] Página de detalhe sem ações de edição/publicação

**Severidade:** Alta  
**Ficheiro afetado:** `src/frontend/src/features/knowledge/pages/KnowledgeDocumentPage.tsx`

**Problema:**  
A `KnowledgeDocumentPage` exibe o documento em modo read-only sem qualquer ação de gestão. O backend tem endpoints `PUT /documents/{id}` e suporta transições de estado (Publish, Archive, Deprecate), mas não existe botão "Editar" ou "Publicar" na UI.

**Correção:**  
Adicionar:
- Botão "Editar" (para utilizadores com `knowledge:write`)
- Dropdown de ações: "Publicar", "Arquivar", "Marcar como Obsoleto"
- Verificar permissões no frontend (reflexo de segurança, não fonte de verdade)

---

### P-FE-04 — [MÉDIO] Tipo misto (union) frágil em KnowledgeHubPage

**Severidade:** Média  
**Ficheiro afetado:** `src/frontend/src/features/knowledge/pages/KnowledgeHubPage.tsx` (linha 198)

**Problema:**  
```tsx
const isSearchItem = 'entityId' in item;
const id = isSearchItem ? item.entityId : (item as { documentId: string }).documentId;
```

A distinção entre itens de pesquisa e itens de listagem usa type narrowing com `'entityId' in item`. Esta abordagem é frágil e dificulta a manutenção. Deveria usar discriminated unions ou tipos separados.

**Correção:**  
Criar dois arrays separados ou normalizar os resultados para um tipo comum:
```typescript
interface KnowledgeListItem {
  id: string;
  title: string;
  summary: string | null;
  category: DocumentCategory | null;
  status: DocumentStatus | null;
  tags: string[];
  updatedAt: string | null;
  isSearchResult: boolean;
}
```

---

### P-FE-05 — [MÉDIO] Resultados de pesquisa na hub filtram apenas documentos

**Severidade:** Média  
**Ficheiro afetado:** `src/frontend/src/features/knowledge/pages/KnowledgeHubPage.tsx` (linha 90)

**Problema:**  
```tsx
const displayItems = debouncedSearch.trim().length >= 2 && searchResults
    ? searchResults.items.filter(i => i.entityType === 'KnowledgeDocument')
    : (documents?.items ?? []);
```

A pesquisa retorna documentos E notas operacionais, mas o filtro `entityType === 'KnowledgeDocument'` descarta as notas. Um utilizador que pesquise por uma nota não a encontra na Hub Page.

---

### P-FE-06 — [MÉDIO] Missing i18n keys documentados vs usados

**Severidade:** Média  
**Ficheiros afetados:** Páginas do módulo Knowledge

**Problema:**  
A KnowledgeHubPage usa chaves como `knowledgeHub.recentNotesTitle`, `knowledgeHub.viewAllNotes`, `knowledgeHub.severity.${note.severity}`. Sem validação das i18n keys no build, é possível que algumas estejam ausentes nos ficheiros de tradução, resultando em keys expostas ao utilizador.

**Acção:**  
Auditar `src/frontend/src/i18n/` para confirmar que todas as keys do módulo Knowledge estão presentes em todos os idiomas suportados.

---

### P-FE-07 — [BAIXO] Criação de documentos sem página/formulário dedicado

**Severidade:** Baixa  
**Observação:**  
O botão "New Document" navega para `/knowledge/documents/new` mas a `knowledgeRoutes.tsx` não tem uma rota para esta path. Se não existe página de criação, o utilizador ficará com 404 ou será redirecionado inesperadamente.

---

## 10. Problemas de Testes

### P-TST-01 — [CRÍTICO] Ausência de testes de isolamento multi-tenant

**Severidade:** Crítica  
**Observação:**  
Dado que os repositórios não filtram por tenant (P-SEC-01), os testes existentes não cobrem este cenário porque o problema não é detectado. São necessários testes específicos que validem que documentos de um tenant não são visíveis a outro.

**Testes necessários:**
```csharp
[Fact]
public async Task GetByIdAsync_ShouldNotReturn_DocumentFromOtherTenant()
{
    // Arrange: doc criado com TenantId = tenant-A
    // Act: query com currentTenant = tenant-B
    // Assert: resultado é null
}
```

---

### P-TST-02 — [MÉDIO] Ausência de testes para o DB constraint mismatch

**Severidade:** Média  
**Observação:**  
Não existe teste que tente persistir um documento com `Category = DocumentCategory.ApiDocumentation` e valide que não falha. Dado o mismatch do CHECK constraint (P-DOM-01), este teste falharia revelando o problema.

---

### P-TST-03 — [MÉDIO] Testes de Infrastructure usam InMemory (FTS não testado)

**Severidade:** Média  
**Ficheiro afetado:** `Infrastructure/KnowledgePersistenceAndSearchIntegrationTests.cs`

**Observação:**  
Testes que usam `UseInMemoryDatabase` não testam o comportamento real do FTS (PostgreSQL-specific). O `EF.Functions.ToTsVector` e `PlainToTsQuery` não funcionam com o provider InMemory. Os testes de search passam mas podem não validar o comportamento real.

**Recomendação:**  
Para testes de FTS, usar `Testcontainers.PostgreSQL` ou uma base de dados de teste real.

---

## 11. Avaliação de Base de Dados

### Distribuição atual e recomendada

| Entidade / Data | Atual | Recomendado | Justificação |
|---|---|---|---|
| `KnowledgeDocument` | PostgreSQL ✅ | PostgreSQL | Dados transacionais; lifecycle; versionamento |
| `OperationalNote` | PostgreSQL ✅ | PostgreSQL | Dados transacionais; contexto operacional |
| `KnowledgeRelation` | PostgreSQL ✅ | PostgreSQL | Dados relacionais; integridade referencial |
| `KnowledgeGraphSnapshot` | PostgreSQL ✅ | PostgreSQL | Snapshot periódico; análise histórica leve |
| `ProposedRunbook` | PostgreSQL ✅ | PostgreSQL | Workflow de aprovação; persistência fiável |
| Search analytics (IKnowledgeBaseUtilizationReader) | Null ⚠️ | **Elasticsearch/ClickHouse** | Time-series; alta volumetria; analytics |
| Search events (pesquisas realizadas) | Não implementado ❌ | **Elasticsearch/ClickHouse** | Ingestão via Ingestion.Api; analytics |
| Document access tracking | Não implementado ❌ | **Elasticsearch/ClickHouse** | Time-series; analytics |

### Decisão sobre ClickHouse vs Elasticsearch

Conforme copilot-instructions.md §10.3, o utilizador deve escolher durante a instalação. Para o módulo Knowledge:

- **Elasticsearch** é mais adequado para os dados analíticos do Knowledge porque:
  - FTS nativo superior ao ClickHouse
  - Suporte a aggregations para search analytics
  - Integração natural com o KQL para pesquisa avançada
  
- **ClickHouse** é mais adequado para:
  - Eventos de alta frequência (access logs, search logs)
  - Queries analíticas com GROUP BY e COUNT em janelas temporais
  
**Recomendação:** Implementar `IKnowledgeBaseUtilizationReader` como adaptador com duas implementações:
- `ElasticsearchKnowledgeBaseUtilizationReader`
- `ClickHouseKnowledgeBaseUtilizationReader`

Ativadas via `NexTrace:Analytics:Provider` conforme configuração existente.

---

## 12. Avaliação de Bibliotecas e Boas Práticas 2026

### Backend

| Biblioteca | Versão usada | Status 2026 | Notas |
|---|---|---|---|
| .NET 10 | ✅ 10.x | Excelente | Stack atual e suportada |
| EF Core 10 | ✅ 10.x | Excelente | Versão atual |
| MediatR | ✅ | Bom | Maturidade elevada; considerar alternative nativa se .NET 10 suportar |
| FluentValidation | ✅ | Excelente | Padrão de mercado |
| Ardalis.GuardClauses | ✅ | Bom | Guard clauses simples e legíveis |
| Npgsql | ✅ | Excelente | Melhor provider PostgreSQL para .NET |
| Serilog | ✅ | Excelente | Logging estruturado maduro |
| OpenTelemetry | ✅ | Excelente | Standard de observabilidade |
| Polly | ❌ Não implementado | Recomendado | Falta retry/circuit breaker em chamadas externas |
| `react-markdown` | ❌ Ausente | Necessário | Para renderização de Markdown no frontend |

### Frontend

| Biblioteca | Status | Notas |
|---|---|---|
| React 18 | ✅ | Estável; React 19 existe mas 18 é produção |
| TypeScript | ✅ | Obrigatório |
| TanStack Query | ✅ | Excelente para server state |
| TanStack Router | ✅ | Moderno; type-safe routing |
| Zustand | ✅ | Correto para client state |
| Tailwind CSS | ✅ | Bom para design system consistente |
| Radix UI | ✅ | Acessibilidade e componentes headless |
| `react-markdown` | ❌ Ausente | Necessário para renderização de conteúdo Markdown (P-FE-02) |

---

## 13. Conformidade com CLAUDE.md e copilot-instructions.md

### Verificação por regra

| Regra | Status | Observação |
|---|---|---|
| `sealed` para classes finais | ✅ | Todos os handlers, repositórios e entidades são `sealed` |
| `CancellationToken` em async | ✅ | Presente em todos os métodos async |
| `Result<T>` para falhas | ✅ | Usado consistentemente |
| Strongly typed IDs | ⚠️ | Faltam `New()` e `From()` em alguns IDs (P-DOM-07) |
| `IDateTimeProvider` (sem DateTime.Now) | ✅ | Correto |
| Separação Domain/Application/Infrastructure | ✅ | Bem respeitada |
| IXxxRepository nunca null | ⚠️ | Todos têm implementação real (correto) |
| IXxxReader honest-null aceitável | ✅ | `IKnowledgeBaseUtilizationReader` e `ITeamKnowledgeSharingReader` documentados |
| Tenant isolation em repositórios | ❌ | Ausente em todos os repositórios (P-SEC-01) |
| XML docs em português | ✅ | Boa cobertura |
| Comentários inline em português | ⚠️ | Alguns comentários em inglês (P-DOM-08) |
| UI text via i18n | ✅ | Maioria correto; alguns lapsos |
| Não expor GUIDs ao utilizador | ❌ | authorId exposto como UUID (P-FE-01) |
| Feature CQRS em ficheiro único | ✅ | Padrão seguido |
| Pipeline MediatR para todos os requests | ❌ | Search endpoint bypassa pipeline (P-SEC-03) |
| `HasCapability` check para features premium | ❌ | Ausente em todos os handlers (P-APP-08) |

---

## 14. Plano de Correções Priorizado

### P0 — Blocker de Produção (corrigir antes de qualquer deploy)

| ID | Problema | Esforço | Impacto |
|---|---|---|---|
| P-SEC-01 | Adicionar TenantId a entidades e filtros nos repositórios | Alto | CRÍTICO — data leak entre tenants |
| P-DOM-01 | Corrigir CHECK constraint para incluir novas categorias | Baixo | CRÍTICO — INSERTs falham |
| P-SEC-02 | Remover AuthorId/EditorId do body; usar ICurrentUser | Médio | ALTO — impersonation |
| P-SEC-03 | Refatorar endpoint search para usar MediatR pipeline | Baixo | ALTO — bypassa TenantIsolation |

### P1 — Qualidade e Performance (sprint seguinte)

| ID | Problema | Esforço | Impacto |
|---|---|---|---|
| P-DOM-03 | Corrigir unique constraint KnowledgeRelation | Baixo | ALTO — bloqueia relações válidas |
| P-INF-01 | Eliminar N+1 em KnowledgeSearchProvider | Médio | ALTO — performance search |
| P-INF-02 | Eliminar N+1 em GetKnowledgeGraphOverview | Médio | ALTO — performance graph |
| P-INF-03 | Eliminar N+1 em RunbookKnowledgeLinkingService | Médio | ALTO — performance |
| P-INF-04 | Adicionar coluna tsvector materializada + GIN index | Médio | ALTO — performance FTS |
| P-DOM-02 | Migrar KnowledgeDocument para AuditableEntity | Alto | MÉDIO — soft-delete, auditoria |
| P-APP-01 | Adicionar endpoints para features sem HTTP | Médio | MÉDIO — features inacessíveis |
| P-FE-02 | Renderizar Markdown com react-markdown | Baixo | ALTO — UX quebrada |
| P-API-01 | Mapear endpoints em falta | Médio | MÉDIO — funcionalidades inacessíveis |

### P2 — Melhorias Estruturais

| ID | Problema | Esforço | Impacto |
|---|---|---|---|
| P-APP-03 | Consistência CommitAsync vs TransactionBehavior | Baixo | MÉDIO |
| P-APP-04 | ScoreDocumentFreshness.Command usar Guid | Baixo | BAIXO |
| P-APP-05 | MarkAsStale para snapshots Reviewed | Baixo | MÉDIO |
| P-APP-06 | GenerateAutoDocumentation com dados reais | Alto | MÉDIO |
| P-INF-05 | RunbookKnowledgeLinkingService injetar IUnitOfWork | Baixo | BAIXO |
| P-INF-06 | CountDocumentsByServiceAsync filtrar por TargetType | Baixo | MÉDIO |
| P-DOM-04 | Mover ProposedRunbookStatus para Enums/ | Baixo | BAIXO |
| P-DOM-06 | TenantId não-nullable em KnowledgeGraphSnapshot | Baixo | BAIXO |
| P-DOM-07 | Adicionar New() e From() aos IDs em falta | Baixo | BAIXO |
| P-DOM-09 | Adicionar Domain Events a KnowledgeDocument | Médio | MÉDIO |
| P-SEC-04 | RowVersion setter interno | Baixo | BAIXO |
| P-FE-01 | Resolver authorId como nome/email | Médio | ALTO — UX |
| P-FE-03 | Adicionar ações Edit/Publish à página de detalhe | Médio | ALTO — UX |
| P-FE-04 | Union type frágil na KnowledgeHubPage | Baixo | MÉDIO |
| P-DB-01 | Configurar audit fields em KnowledgeGraphSnapshotConfiguration | Baixo | MÉDIO |
| P-DB-02 | Adicionar índices em ProposedRunbookConfiguration | Baixo | BAIXO |

### P3 — Dívida técnica menor

| ID | Problema | Esforço | Impacto |
|---|---|---|---|
| P-APP-01 | GetFreshnessReport sem Validator | Baixo | BAIXO |
| P-APP-08 | HasCapability checks | Baixo | BAIXO |
| P-INF-07 | Centralizar SystemAuthorId | Baixo | BAIXO |
| P-INF-08 | Tags no FTS | Médio | MÉDIO |
| P-TST-01 | Testes de isolamento tenant | Alto | CRÍTICO (depende de P-SEC-01) |
| P-TST-02 | Testes para DB constraint | Baixo | MÉDIO |
| P-TST-03 | Usar Testcontainers para testes FTS | Médio | MÉDIO |
| P-DOM-08 | Comentários em inglês → português | Baixo | BAIXO |
| P-API-04 | Versão dinâmica no endpoint /status | Baixo | BAIXO |
| P-FE-05 | Pesquisa na hub incluir notas | Baixo | MÉDIO |
| P-FE-07 | Rota /knowledge/documents/new | Baixo | ALTO — UX |

---

## Anexo — Tabela de Rastreabilidade

| Problema | Ficheiro | Linha | Prioridade | Tipo |
|---|---|---|---|---|
| P-SEC-01 | `Domain/Entities/KnowledgeDocument.cs` | — | P0 | Security |
| P-SEC-01 | `Infrastructure/Persistence/Repositories/*.cs` | — | P0 | Security |
| P-SEC-02 | `Application/Features/CreateKnowledgeDocument/` | 16 | P0 | Security |
| P-SEC-02 | `Application/Features/UpdateKnowledgeDocument/` | 19 | P0 | Security |
| P-SEC-03 | `API/Endpoints/KnowledgeEndpointModule.cs` | 54-76 | P0 | Security |
| P-SEC-04 | `Domain/Entities/KnowledgeDocument.cs` | 65 | P2 | Security |
| P-DOM-01 | `Domain/Enums/DocumentCategory.cs` + Migration | — | P0 | Data Integrity |
| P-DOM-02 | `Domain/Entities/KnowledgeDocument.cs` | 23 | P1 | Architecture |
| P-DOM-03 | `Infrastructure/Persistence/Configurations/KnowledgeRelationConfiguration.cs` | 64 | P1 | Data Integrity |
| P-DOM-04 | `Domain/Entities/ProposedRunbook.cs` | 7 | P2 | Organization |
| P-DOM-05 | `Domain/Entities/ProposedRunbook.cs` | — | P2 | Documentation |
| P-DOM-06 | `Domain/Entities/KnowledgeGraphSnapshot.cs` | 70 | P2 | Consistency |
| P-DOM-07 | `Domain/Entities/KnowledgeDocument.cs` | 12 | P3 | Convention |
| P-DOM-08 | `Domain/Entities/KnowledgeDocument.cs` | 194-209 | P3 | Convention |
| P-DOM-09 | `Domain/Entities/KnowledgeDocument.cs` | — | P2 | Architecture |
| P-APP-01 | `Application/Features/GetFreshnessReport/` | — | P1 | Design |
| P-APP-02 | `Application/Features/SearchAcrossModules/` | 38 | P1 | Performance |
| P-APP-03 | `Application/Features/BuildKnowledgeGraphSnapshot/` | 83 | P2 | Consistency |
| P-APP-04 | `Application/Features/ScoreDocumentFreshness/` | 17 | P2 | Convention |
| P-APP-05 | `Application/Features/BuildKnowledgeGraphSnapshot/` | 62 | P2 | Logic |
| P-APP-06 | `Application/Features/GenerateAutoDocumentation/` | — | P2 | Value |
| P-APP-07 | `Application/Features/GetFreshnessReport/` | — | P3 | Convention |
| P-APP-08 | Todos os handlers de escrita | — | P3 | SaaS |
| P-INF-01 | `Infrastructure/Search/KnowledgeSearchProvider.cs` | 36 | P1 | Performance |
| P-INF-02 | `Application/Features/GetKnowledgeGraphOverview/` | 122 | P1 | Performance |
| P-INF-03 | `Infrastructure/Search/RunbookKnowledgeLinkingService.cs` | 77 | P1 | Performance |
| P-INF-04 | `Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs` | 32-46 | P1 | Performance |
| P-INF-05 | `Infrastructure/Search/RunbookKnowledgeLinkingService.cs` | 16 | P2 | Architecture |
| P-INF-06 | `Infrastructure/Services/KnowledgeModuleService.cs` | 47 | P2 | Logic |
| P-INF-07 | `Infrastructure/Search/RunbookKnowledgeLinkingService.cs` | 22 | P3 | Convention |
| P-INF-08 | `Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs` | — | P2 | Feature |
| P-API-01 | `API/Endpoints/KnowledgeEndpointModule.cs` | — | P1 | Feature |
| P-API-02 | `API/Endpoints/KnowledgeEndpointModule.cs` | — | P2 | Feature |
| P-API-03 | `API/Endpoints/KnowledgeEndpointModule.cs` | 170 | P2 | UX |
| P-API-04 | `API/Endpoints/KnowledgeEndpointModule.cs` | 48 | P3 | Maintenance |
| P-DB-01 | `Infrastructure/Persistence/Configurations/KnowledgeGraphSnapshotConfiguration.cs` | — | P2 | Schema |
| P-DB-02 | `Infrastructure/Persistence/Configurations/ProposedRunbookConfiguration.cs` | — | P2 | Performance |
| P-FE-01 | `frontend/features/knowledge/pages/KnowledgeDocumentPage.tsx` | 105 | P2 | UX/Security |
| P-FE-02 | `frontend/features/knowledge/pages/KnowledgeDocumentPage.tsx` | 79 | P1 | UX |
| P-FE-03 | `frontend/features/knowledge/pages/KnowledgeDocumentPage.tsx` | — | P1 | UX |
| P-FE-04 | `frontend/features/knowledge/pages/KnowledgeHubPage.tsx` | 198 | P2 | Code Quality |
| P-FE-05 | `frontend/features/knowledge/pages/KnowledgeHubPage.tsx` | 90 | P2 | UX |
| P-FE-07 | `frontend/src/routes/knowledgeRoutes.tsx` | — | P3 | UX |
| P-TST-01 | Ausente em `tests/modules/knowledge/` | — | P1 | Testing |
| P-TST-02 | Ausente em `tests/modules/knowledge/` | — | P2 | Testing |
| P-TST-03 | `tests/modules/knowledge/Infrastructure/` | — | P3 | Testing |

---

*Relatório gerado em 2026-05-14 por auditoria automatizada assistida via Claude Code.*  
*Próximo passo recomendado: implementar as correções P0 antes de qualquer deploy em ambiente não-sandbox.*
