# Auditoria do Módulo ChangeGovernance — NexTraceOne
**Data:** 2026-05-14  
**Auditor:** Claude Code (Automated Review)  
**Âmbito:** End-to-end — Domain, Application, Infrastructure, API, Frontend  
**Branch:** `claude/code-review-audit-i0rFs`

---

## Sumário Executivo

O módulo ChangeGovernance é o mais extenso do sistema — 503 ficheiros C#, 4 sub-contextos (ChangeIntelligence, Promotion, Workflow, RulesetGovernance/Compliance), 4 DbContexts, 92 ficheiros de teste e 31 páginas frontend. A funcionalidade central (ingestão de releases, avaliação de gates, aprovação externa, DORA, calendário) está implementada e parcialmente testada. Contudo, foram identificados problemas graves de multi-tenancy, uma vulnerabilidade de segurança SSRF activa, duas entidades `PromotionGate` conflituantes em bounded contexts distintos, e repositórios sem filtro de tenant que expõem dados cross-tenant. Estes problemas colocam em risco a conformidade SaaS e a integridade dos dados em produção.

**Total de problemas identificados:** 26  
**P0 (bloqueadores de produção):** 6  
**P1 (alta prioridade):** 8  
**P2 (média prioridade):** 8  
**P3 (baixa prioridade):** 4  

---

## Índice

1. [Estrutura do Módulo](#1-estrutura-do-módulo)
2. [Domain Layer — ChangeIntelligence](#2-domain-layer--changeintelligence)
3. [Domain Layer — Promotion & Workflow](#3-domain-layer--promotion--workflow)
4. [Application Layer](#4-application-layer)
5. [Infrastructure — Repositórios e Persistência](#5-infrastructure--repositórios-e-persistência)
6. [Infrastructure — Serviços Externos](#6-infrastructure--serviços-externos)
7. [API Layer](#7-api-layer)
8. [Frontend](#8-frontend)
9. [Testes](#9-testes)
10. [Bibliotecas e Dependências](#10-bibliotecas-e-dependências)
11. [Banco de Dados — Placement PostgreSQL vs Analítico](#11-banco-de-dados--placement-postgresql-vs-analítico)
12. [Conformidade com CLAUDE.md e copilot-instructions.md](#12-conformidade-com-claudemd-e-copilot-instructionsmd)
13. [Plano de Correção por Prioridade](#13-plano-de-correção-por-prioridade)

---

## 1. Estrutura do Módulo

```
src/modules/changegovernance/
├── NexTraceOne.ChangeGovernance.Domain/
│   ├── ChangeIntelligence/Entities/     ← Release, PromotionGate (CI), BlastRadius, ...
│   ├── Promotion/Entities/              ← PromotionRequest, PromotionGate (Promo), DeploymentEnvironment
│   ├── Workflow/Entities/               ← WorkflowInstance, WorkflowTemplate
│   ├── RulesetGovernance/
│   └── Compliance/
├── NexTraceOne.ChangeGovernance.Application/
│   ├── ChangeIntelligence/Features/     ← ~50 features
│   ├── Promotion/Features/
│   ├── Workflow/Features/
│   └── Compliance/
├── NexTraceOne.ChangeGovernance.Infrastructure/
│   ├── ChangeIntelligence/Persistence/  ← ChangeIntelligenceDbContext
│   ├── Promotion/Persistence/           ← PromotionDbContext
│   ├── Workflow/Persistence/            ← WorkflowDbContext
│   └── RulesetGovernance/Persistence/   ← RulesetGovernanceDbContext
├── NexTraceOne.ChangeGovernance.API/
│   ├── ChangeIntelligence/Endpoints/    ← 12 endpoint ficheiros
│   ├── Promotion/Endpoints/
│   ├── Workflow/Endpoints/
│   └── RulesetGovernance/Endpoints/
└── NexTraceOne.ChangeGovernance.Contracts/
```

A estrutura multi-sub-contexto com 4 DbContexts é coerente com a divisão por bounded context. O prefixo `chg_` é consistente em todas as tabelas verificadas.

---

## 2. Domain Layer — ChangeIntelligence

### Ficheiro: `NexTraceOne.ChangeGovernance.Domain/ChangeIntelligence/Entities/Release.cs`

**Aspectos correctos:**
- Estende `AggregateRoot<ReleaseId>` — correcto
- `TenantId` é `Guid` — correcto
- State machine documentada: `Pending → Running → (Succeeded|Failed) → RolledBack`
- Campos SLSA Level 3: `SlsaProvenanceUri`, `ArtifactDigest`, `SbomUri` — bom
- `ExternalReleaseId` + `ExternalSystem` como chave natural para roteamento externo
- ID fortemente tipado com `New()` e `From()` — correcto

---

### [P2-CG-001] `Release.ApprovalStatus` é `string?` em vez de enum

**Ficheiro:** `Domain/ChangeIntelligence/Entities/Release.cs`  
**Severidade:** P2

`ApprovalStatus` é declarado como `string?`, quebrando a segurança de tipos do domínio. Outros campos de estado (ex.: `Status`, `ConfidenceStatus`) usam enums tipados correctamente.

**Correcção:**
```csharp
// Criar em Domain/ChangeIntelligence/Enums/ReleaseApprovalStatus.cs:
public enum ReleaseApprovalStatus { Pending, Approved, Rejected, NotRequired }

// Em Release.cs:
public ReleaseApprovalStatus? ApprovalStatus { get; private set; }
```

---

### [P2-CG-002] `Release.RowVersion` com setter público

**Ficheiro:** `Domain/ChangeIntelligence/Entities/Release.cs`  
**Severidade:** P2

```csharp
// Actual (bug):
public uint RowVersion { get; set; }

// Correcto:
public uint RowVersion { get; internal set; }
```

Setter público expõe o token de concorrência otimista a modificação directa fora do EF Core.

---

### [P0-CG-003] Duas entidades `PromotionGate` em bounded contexts distintos — colisão de nomes e duplicação de conceito

**Ficheiros:**
- `Domain/ChangeIntelligence/Entities/PromotionGate.cs` (namespace: `NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities`)
- `Domain/Promotion/Entities/PromotionGate.cs` (namespace: `NexTraceOne.ChangeGovernance.Domain.Promotion.Entities`)

**Severidade:** P0 — violação de bounded context, confusão arquitectural, inconsistência de dados

As duas entidades têm nomes idênticos mas conceitos distintos:

| Propriedade | ChangeIntelligence.PromotionGate | Promotion.PromotionGate |
|---|---|---|
| Base | `Entity<PromotionGateId>` | `AuditableEntity<PromotionGateId>` |
| TenantId | `string?` | via AuditableEntity |
| Campos | Name, EnvironmentFrom, EnvironmentTo, Rules (JSONB), IsActive, BlockOnFailure | GateName, GateType, IsRequired, IsActive |
| DbContext | `ChangeIntelligenceDbContext` | `PromotionDbContext` |
| Propósito | Gate configurável com regras JSONB para promoção entre ambientes | Gate vinculado a DeploymentEnvironment com critério específico |

Ambas estão registadas nos respectivos DbContexts. Dois repositórios com o mesmo nome (`IPromotionGateRepository`, `PromotionGateRepository`) coexistem — um em cada sub-contexto — tornando injecções de dependência ambíguas.

**Correcção:**
1. Renomear `ChangeIntelligence.PromotionGate` para `PromotionPolicy` ou `ReleaseGatePolicy` — reflecte o conceito de "configuração de política de gate" vs. "gate de avaliação vinculado a ambiente"
2. Migrar tabela correspondente com `ALTER TABLE` + nova migration
3. Atualizar repositório, configuração EF e todos os handlers que referenciam a entidade

---

### [P0-CG-004] `ChangeIntelligence.PromotionGate.TenantId` é `string?` em vez de `Guid`

**Ficheiro:** `Domain/ChangeIntelligence/Entities/PromotionGate.cs:46`  
**Severidade:** P0

```csharp
// Actual (bug):
public string? TenantId { get; private set; }

// Correcto:
public Guid TenantId { get; private set; }
```

`string?` para TenantId impede filtros de tenant correctos, viola a convenção de todo o sistema (todos os outros TenantId são `Guid`), e permite que `TenantId` seja nulo — tornando a entidade opaca para o `TenantRlsInterceptor`.

---

## 3. Domain Layer — Promotion & Workflow

### Ficheiro: `Domain/Promotion/Entities/PromotionRequest.cs`

**Aspectos correctos:**
- Estende `AggregateRoot<PromotionRequestId>` — correcto
- State machine completa e bem documentada
- ID fortemente tipado com `New()` e `From()` — correcto
- `ValidateTransition` centraliza transições (DRY) — bom design

---

### [P0-CG-005] `PromotionRequest` não tem `TenantId`

**Ficheiro:** `Domain/Promotion/Entities/PromotionRequest.cs`  
**Severidade:** P0

A entidade `PromotionRequest` não possui campo `TenantId`. Não há filtro de tenant no repositório correspondente. Num sistema SaaS multi-tenant, isto significa que qualquer tenant pode ver todas as solicitações de promoção de todos os outros tenants.

**Impacto:**
- `PromotionRequestRepository.ListByStatusAsync` retorna solicitações de TODOS os tenants
- `PromotionRequestRepository.ListByReleaseIdAsync` idem
- Violação grave de isolamento de dados

**Correcção:**
```csharp
// Em PromotionRequest.cs — adicionar propriedade:
public Guid TenantId { get; private set; }

// Em PromotionRequest.Create():
TenantId = tenantId, // recebido do ICurrentTenant

// Em PromotionRequestConfiguration.cs:
builder.Property(x => x.TenantId).IsRequired();
builder.HasIndex(x => x.TenantId);

// Em PromotionRequestRepository — todos os métodos:
.Where(r => r.TenantId == currentTenant.Id)
```

---

### [P0-CG-006] `WorkflowInstance` não tem `TenantId`

**Ficheiro:** `Domain/Workflow/Entities/WorkflowInstance.cs`  
**Severidade:** P0

Mesma falha que PromotionRequest. `WorkflowInstance` não possui TenantId, e o `WorkflowInstanceRepository` não filtra por tenant em nenhum dos seus métodos (`GetByIdAsync`, `GetByReleaseIdAsync`, `ListByStatusAsync`, `ListAsync`, `CountAsync`, `CountByStatusAsync`).

**Correcção:** idêntica à de PromotionRequest — adicionar `TenantId` à entidade e filtros no repositório.

---

### [P2-CG-007] `PromotionRequest.RowVersion` e `WorkflowInstance.RowVersion` com setter público

**Ficheiros:**
- `Domain/Promotion/Entities/PromotionRequest.cs:46`
- `Domain/Workflow/Entities/WorkflowInstance.cs:43`

**Severidade:** P2

```csharp
// Actual (bug em ambos):
public uint RowVersion { get; set; }

// Correcto:
public uint RowVersion { get; internal set; }
```

---

### [P2-CG-008] `PromotionRequest.RequestedBy` e `WorkflowInstance.SubmittedBy` recebem string do utilizador

**Ficheiros:** `PromotionRequest.cs:31`, `WorkflowInstance.cs:28`  
**Severidade:** P2

`RequestedBy` e `SubmittedBy` são strings passadas no Create factory. Os handlers de criação devem injectar `ICurrentUser` e usar `currentUser.Id` ou `currentUser.Email` — nunca aceitar do request body.

---

### [P2-CG-009] `PromotionGate` (Promotion) estende `AuditableEntity` mas `PromotionGate` (CI) estende `Entity` — inconsistência

**Ficheiro:** `Domain/ChangeIntelligence/Entities/PromotionGate.cs`  
**Severidade:** P2

O PromotionGate do sub-contexto ChangeIntelligence estende `Entity<PromotionGateId>`, perdendo os benefícios de `AuditableEntity` (CreatedAt/By automático, soft-delete). Em vez disso, tem campos manuais `CreatedAt` e `CreatedBy`. Após a renomeação para `ReleaseGatePolicy` (P0-CG-003), deve estender `AuditableEntity<>`.

---

## 4. Application Layer

### [P1-CG-010] `IngestExternalRelease` ignora `WorkItems` e descarta múltiplos `CommitShas`

**Ficheiro:** `Application/ChangeIntelligence/Features/IngestExternalRelease/IngestExternalRelease.cs:112`  
**Severidade:** P1

A documentação do handler afirma: *"Commits e work items fornecidos são associados automaticamente."*  
O código faz:

```csharp
commitSha: request.CommitShas is { Count: > 0 } shas ? shas[0] : "external",
```

- Apenas o primeiro commit SHA é utilizado; os restantes são silenciosamente descartados
- O parâmetro `WorkItems` é completamente ignorado — não há associação de work items implementada
- O campo `TriggerPromotion` também não produz nenhum efeito no handler

**Correcção:**
1. Criar `CommitAssociation` e `WorkItemAssociation` após `releaseRepository.Add(release)`
2. Usar os repositórios `ICommitAssociationRepository` e `IWorkItemAssociationRepository` já registados no DI
3. Actualizar a documentação se a associação for intencional numa fase posterior

---

### [P1-CG-011] `IngestExternalRelease` chama `unitOfWork.CommitAsync()` directamente — duplo commit

**Ficheiro:** `Application/ChangeIntelligence/Features/IngestExternalRelease/IngestExternalRelease.cs:118`  
**Severidade:** P1

O handler chama `await unitOfWork.CommitAsync(cancellationToken)` explicitamente. O pipeline MediatR inclui `TransactionBehavior` que também commita após Commands com sucesso. Isto resulta em dois commits na mesma transação.

Outros handlers do módulo (ex.: `CreatePromotionRequest`) não chamam `CommitAsync` explicitamente, confiando no `TransactionBehavior`. O padrão deve ser uniforme.

**Correcção:** Remover a chamada explícita a `unitOfWork.CommitAsync()` do handler. O `TransactionBehavior` trata disso automaticamente.

---

### [P1-CG-012] `GetByExternalKeyAsync` sem filtro de tenant — idempotência cross-tenant

**Ficheiro:** `Infrastructure/ChangeIntelligence/Persistence/Repositories/ReleaseRepository.cs:33`  
**Severidade:** P1

```csharp
// Actual (bug):
.SingleOrDefaultAsync(r => r.ExternalReleaseId == externalReleaseId
    && r.ExternalSystem == externalSystem, cancellationToken);

// Correcto — adicionar filtro de tenant:
.SingleOrDefaultAsync(r => r.TenantId == currentTenant.Id
    && r.ExternalReleaseId == externalReleaseId
    && r.ExternalSystem == externalSystem, cancellationToken);
```

Dois tenants diferentes podem partilhar o mesmo `ExternalReleaseId` + `ExternalSystem` (ex.: duas empresas a usar o mesmo prefixo de release no Jenkins). Sem filtro de tenant, o segundo tenant recebe a release do primeiro como se fosse a sua, ignorando a ingestão.

---

## 5. Infrastructure — Repositórios e Persistência

### [P1-CG-013] `ReleaseRepository.GetSummaryCountsAsync` executa 5 COUNT queries separadas

**Ficheiro:** `Infrastructure/ChangeIntelligence/Persistence/Repositories/ReleaseRepository.cs:141-145`  
**Severidade:** P1

```csharp
// Actual (5 queries ao banco):
var total = await query.CountAsync(cancellationToken);
var validated = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.Validated, cancellationToken);
var needsAttention = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.NeedsAttention, cancellationToken);
var suspectedRegressions = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.SuspectedRegression, cancellationToken);
var correlatedWithIncidents = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.CorrelatedWithIncident, cancellationToken);
```

**Correcção — 1 query com GROUP BY:**
```csharp
var counts = await query
    .GroupBy(r => r.ConfidenceStatus)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

var total = counts.Values.Sum();
var validated = counts.GetValueOrDefault(ConfidenceStatus.Validated);
var needsAttention = counts.GetValueOrDefault(ConfidenceStatus.NeedsAttention);
var suspectedRegressions = counts.GetValueOrDefault(ConfidenceStatus.SuspectedRegression);
var correlatedWithIncidents = counts.GetValueOrDefault(ConfidenceStatus.CorrelatedWithIncident);
```

---

### [P0-CG-014] `PromotionRequestRepository` sem filtro de tenant em todos os métodos

**Ficheiro:** `Infrastructure/Promotion/Persistence/Repositories/PromotionRequestRepository.cs`  
**Severidade:** P0

Todos os métodos de leitura não filtram por tenant:

```csharp
// Exemplo — ListByStatusAsync sem tenant:
.Where(r => r.Status == status)

// Correcto (após adicionar TenantId à entidade):
.Where(r => r.TenantId == currentTenant.Id && r.Status == status)
```

Métodos afectados: `GetByIdAsync`, `ListByStatusAsync`, `ListByReleaseIdAsync`, `CountByStatusAsync`.

---

### [P0-CG-015] `WorkflowInstanceRepository` sem filtro de tenant em todos os métodos

**Ficheiro:** `Infrastructure/Workflow/Persistence/Repositories/WorkflowInstanceRepository.cs`  
**Severidade:** P0

Todos os métodos de leitura não filtram por tenant:

Métodos afectados: `GetByIdAsync`, `GetByReleaseIdAsync`, `ListByStatusAsync`, `ListAsync`, `CountByStatusAsync`, `CountAsync`.

---

### [P1-CG-016] Métodos de `ReleaseRepository` sem filtro de tenant

**Ficheiro:** `Infrastructure/ChangeIntelligence/Persistence/Repositories/ReleaseRepository.cs`  
**Severidade:** P1

Os seguintes métodos aceitam e/ou retornam releases sem filtrar pelo tenant actual:

- `ListByApiAssetAsync` — filtra apenas por `apiAssetId`
- `CountByApiAssetAsync` — idem
- `ListByServiceNameAsync` — filtra apenas por `serviceName`
- `CountByServiceNameAsync` — idem
- `ListSimilarReleasesAsync` — filtra por serviceName, environment, changeLevel mas não por tenant
- `GetByIdAsync` — sem filtro de tenant (depende exclusivamente do TenantRlsInterceptor)

`ListFilteredAsync` recebe `tenantId` explicitamente e filtra correctamente — servir como referência para os restantes.

---

## 6. Infrastructure — Serviços Externos

### [P0-CG-017] SSRF em `ExternalApprovalWebhookSender` — URL não validada contra redes internas

**Ficheiro:** `Infrastructure/ChangeIntelligence/Services/ExternalApprovalWebhookSender.cs`  
**Handler:** `Application/ChangeIntelligence/Features/RequestExternalApproval/RequestExternalApproval.cs`  
**Severidade:** P0 — vulnerabilidade de segurança activa

O `webhookUrl` vem directamente do request body e o Validator apenas verifica:

```csharp
.Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
```

Isto permite a qualquer utilizador com permissão `change-intelligence:write` especificar URLs internas como:
- `http://localhost:5432/` (PostgreSQL)
- `http://169.254.169.254/latest/meta-data/` (AWS Instance Metadata)
- `http://10.0.0.1/admin` (redes internas)
- `http://127.0.0.1:8080/actuator/env` (Spring Boot management)

O HttpClient executará o pedido sem restrições.

**Correcção — adicionar validação no Validator:**
```csharp
RuleFor(x => x.WebhookUrl)
    .NotEmpty()
    .Must(IsAllowedWebhookUrl)
    .When(x => x.ApprovalType == "ExternalWebhook")
    .WithMessage("WebhookUrl must be a valid HTTPS URL pointing to an external host.");

private static bool IsAllowedWebhookUrl(string? url)
{
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
    if (uri.Scheme != Uri.UriSchemeHttps) return false;
    
    var host = uri.Host.ToLowerInvariant();
    if (host is "localhost" or "127.0.0.1" or "::1") return false;
    
    // Bloquear RFC1918 e link-local
    if (IsPrivateAddress(uri.Host)) return false;
    
    return true;
}
```

Adicionalmente, configurar o `HttpClient` "ExternalApprovalWebhook" com um handler que rejeite endereços privados a nível de socket (defense-in-depth).

---

### [P2-CG-018] Callback token enviado como parâmetro de URL — exposição em logs

**Ficheiro:** `API/ChangeIntelligence/Endpoints/Endpoints/ApprovalGatewayEndpoints.cs:55`  
**Severidade:** P2

O endpoint de callback é:
```
POST /api/v1/releases/{releaseId}/approvals/{callbackToken}/respond
```

O `callbackToken` aparece em:
- Access logs do servidor (nginx, IIS, Kestrel)
- Histórico de URLs nos sistemas externos
- Headers de referrer se houver redirects

O handler armazena correctamente o hash SHA-256 do token (não o token em claro), mas o token percorre a rede como parâmetro de URL.

**Correcção:** Mover o token para o corpo do request:
```
POST /api/v1/releases/{releaseId}/approvals/respond
Body: { "callbackToken": "...", "decision": "..." }
```

---

## 7. API Layer

### [P2-CG-019] Inconsistência no nível de autorização entre endpoints

**Ficheiro:** `API/ChangeIntelligence/Endpoints/Endpoints/ApprovalGatewayEndpoints.cs:66`  
**Severidade:** P2

O endpoint `POST .../approvals/{callbackToken}/respond` usa `.AllowAnonymous()` porque é autenticado pelo callback token. Contudo, o handler não valida que o `releaseId` da URL corresponde à release associada ao token — um token válido para Release A poderia ser submetido com `releaseId` de Release B.

**Correcção:** No `RespondToApprovalRequest` handler, após obter a aprovação pelo tokenHash, verificar que `approvalRequest.ReleaseId == request.ReleaseId`.

---

### [P3-CG-020] Nenhum endpoint DELETE para releases, promotion requests ou workflow instances

**Severidade:** P3

Não existem endpoints de cancelamento/remoção acessíveis via API para:
- Cancelar uma promoção (via endpoint dedicado — o método `Cancel` existe no domínio)
- Cancelar um workflow instance
- Remover um PromotionGate obsoleto

O método `Cancel` em `PromotionRequest` e `WorkflowInstance` existe no domínio mas não há endpoint que o exponha.

---

## 8. Frontend

### [P2-CG-021] Tipos TypeScript não são gerados a partir dos contratos OpenAPI

**Ficheiros:** `src/frontend/src/features/change-governance/api/changeIntelligence.ts`, `promotion.ts`, `workflow.ts`  
**Severidade:** P2

Os tipos TypeScript são definidos manualmente. Quando os contratos backend mudam (ex.: adicionar campo `TenantId`, renomear `ApprovalStatus`), o frontend não detecta a divergência em tempo de compilação. Silently wrong data.

**Correcção:** Usar `openapi-typescript` ou `@hey-api/openapi-ts` com a spec OpenAPI do backend para geração automática. Adicionar ao pipeline CI uma verificação de que os tipos gerados estão sincronizados.

---

### [P2-CG-022] `ChangeDetailPage.tsx` usa `useParams` do React Router em vez de TanStack Router

**Ficheiro:** `src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx:2`  
**Severidade:** P2

```tsx
import { useParams, Link } from 'react-router-dom';
```

O projecto usa TanStack Router (conforme CLAUDE.md e copilot-instructions). O uso de `react-router-dom` é um desvio arquitectural — cria dependências inconsistentes e pode conflituar com o roteador principal.

**Correcção:** Migrar para `useParams` do `@tanstack/react-router`:
```tsx
import { useParams } from '@tanstack/react-router';
const { releaseId } = useParams({ from: '/change-governance/releases/$releaseId' });
```

---

### [P3-CG-023] Status strings hardcoded no frontend sem mapeamento via i18n

**Ficheiro:** `src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx:37-49`  
**Severidade:** P3

```tsx
case 'Validated': return 'success';
case 'NeedsAttention': return 'warning';
```

As strings de status como `'Validated'`, `'NeedsAttention'` são comparadas directamente com strings do backend sem i18n. Se o backend renomear um valor de enum, o frontend quebra silenciosamente. Adicionalmente, os labels visuais para estes status não passam por `useTranslation()`.

---

## 9. Testes

### [P1-CG-024] Ausência de testes para `IngestExternalRelease`, `PromotionRequest` e `WorkflowInstance`

**Severidade:** P1

**Cobertura identificada em 92 ficheiros de teste:**
- `ReleaseTests.cs` ✅ (domínio da Release)
- `PromotionGateTests.cs` ✅ (ChangeIntelligence.PromotionGate)
- `PromotionApplicationTests.cs` ✅ (parcial)
- `ListReleasesTests.cs` ✅

**Sem cobertura:**
- `IngestExternalRelease` — sem teste unitário do handler (idempotência, gate de ingestão desabilitada, CommitShas, WorkItems)
- `PromotionRequest` — sem teste de transições de estado (domínio)
- `WorkflowInstance` — sem teste de transições de estado (domínio)
- `ExternalApprovalWebhookSender` — sem teste de URL bloqueada (SSRF scenarios)
- `RespondToApprovalRequest` — sem teste de token expirado, releaseId mismatch
- Repositórios — sem testes de isolamento de tenant

**Testes mínimos obrigatórios a adicionar:**

```csharp
// PromotionRequest domain tests:
[Fact]
public void Create_ShouldSetPendingStatus()

[Fact]  
public void Approve_FromPending_ShouldFail_MustBeInEvaluationFirst()

[Fact]
public void Approve_FromInEvaluation_ShouldSucceed()

[Fact]
public void Cancel_FromTerminalState_ShouldFail()

// IngestExternalRelease handler tests:
[Fact]
public async Task Handle_ExistingExternalKey_ShouldReturnExistingRelease_IsNewFalse()

[Fact]
public async Task Handle_IngestDisabled_ShouldReturnBusinessError()
```

---

## 10. Bibliotecas e Dependências

| Biblioteca | Versão usada | Status 2026 | Notas |
|---|---|---|---|
| `Ardalis.GuardClauses` | Recente | ✅ Adequado | Usado extensivamente em Guards |
| `MediatR` | 12.x | ✅ Adequado | CQRS pipeline correcto |
| `FluentValidation` | 11.x | ✅ Adequado | Validators bem estruturados |
| `Microsoft.EntityFrameworkCore` | 10.x | ✅ Adequado | EF Core 10 estável |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.x | ✅ Adequado | |
| `Microsoft.Extensions.Http.Resilience` | Recente | ✅ Adequado | `AddStandardResilienceHandler` usado |
| `System.Text.Json` | nativo .NET 10 | ✅ Adequado | |
| `openapi-typescript` (frontend) | ❌ Ausente | Recomendado | Geração de tipos TypeScript |
| `@tanstack/router` | Configurado | ✅ | Mas ChangeDetailPage usa react-router-dom |

---

## 11. Banco de Dados — Placement PostgreSQL vs Analítico

### Entidades em PostgreSQL (correcto)

| Tabela | Bounded Context | Justificação |
|---|---|---|
| `chg_releases` | ChangeIntelligence | Transaccional — state machine, concorrência optimista, tenant isolation |
| `chg_promotion_requests` | Promotion | Transaccional — ciclo de vida, aprovação |
| `chg_workflow_instances` | Workflow | Transaccional — aprovação por estágios |
| `chg_promotion_gates` (CI) | ChangeIntelligence | Configuração de gates — relacional |
| `chg_promotion_gates` (Promo) | Promotion | Gates de ambiente — relacional |
| `chg_approval_requests` | ChangeIntelligence | Pedidos de aprovação externa — transaccional |
| `chg_blast_radius` | ChangeIntelligence | Análise de impacto por release |
| `chg_freeze_windows` | ChangeIntelligence | Configuração de freeze periods |
| `chg_release_baselines` | ChangeIntelligence | Baseline de performance por release |

### Candidatos para Analytics (ClickHouse/Elasticsearch)

| Dado | Placement actual | Recomendação |
|---|---|---|
| DORA Metrics (frequência, lead time, MTTR, CFR) | Calculado on-demand via queries PostgreSQL | Pré-calcular e armazenar em ClickHouse/ES para dashboards de alto volume |
| `TraceCorrelationAnalyticsWriter` | PostgreSQL (via ChangeIntelligenceDbContext) | Já usa `IAnalyticsWriter` — correcto para ClickHouse/ES |
| Histórico de `ChangeConfidenceEvent` | PostgreSQL | Candidato a time-series analytics (ClickHouse) para tendências longas |
| `BenchmarkSnapshot` | PostgreSQL | Série temporal — candidato a ClickHouse para cross-tenant benchmarks |

O módulo está bem arquitectado neste aspecto: `IAnalyticsWriter` é injectado e permite alternar entre ClickHouse e Elasticsearch via configuração, sem alterar o código de negócio.

---

## 12. Conformidade com CLAUDE.md e copilot-instructions.md

| Regra | Estado | Notas |
|---|---|---|
| Comandos com `ICommand<Response>` | ✅ Compliant | |
| Handlers como `ICommandHandler<C, R>` | ✅ Compliant | |
| Result<T> / Error sem excepções | ✅ Compliant | |
| `IPublicRequest` para endpoints públicos | ❌ Ausente | Endpoint AllowAnonymous não usa IPublicRequest no Command |
| ID fortemente tipado com `New()` e `From()` | ✅ Parcial | PromotionGate CI usa factory método mas sem `From()` |
| `AuditableEntity` para entidades auditáveis | ❌ Parcial | PromotionGate CI usa Entity manualmente |
| Comentários XML em Português | ✅ Compliant | |
| Comentários inline em Português | ✅ Compliant | |
| Código em Inglês | ✅ Compliant | |
| TenantId como Guid | ❌ Falha | PromotionGate CI tem `string?`, PR e WI sem TenantId |
| Repositórios com filtro de tenant | ❌ Falha | PromotionRequestRepository, WorkflowInstanceRepository, ReleaseRepository (parcial) |
| Frontend i18n | ❌ Parcial | Status strings sem useTranslation |
| TanStack Router | ❌ Parcial | ChangeDetailPage usa react-router-dom |
| Backend é autoridade final | ✅ Compliant | Permissões via RequirePermission |
| RowVersion com setter restrito | ❌ Falha | Público em Release, PromotionRequest, WorkflowInstance |

---

## 13. Plano de Correção por Prioridade

### P0 — Bloqueadores de Produção (resolver antes do próximo deploy)

| ID | Título | Esforço |
|---|---|---|
| P0-CG-003 | Duas entidades PromotionGate — renomear ChangeIntelligence.PromotionGate | Alto (migration + rename) |
| P0-CG-004 | PromotionGate CI: TenantId é `string?` — deve ser `Guid` | Médio |
| P0-CG-005 | PromotionRequest sem TenantId — exposição cross-tenant | Alto |
| P0-CG-006 | WorkflowInstance sem TenantId — exposição cross-tenant | Alto |
| P0-CG-014 | PromotionRequestRepository sem filtro de tenant | Médio |
| P0-CG-015 | WorkflowInstanceRepository sem filtro de tenant | Médio |
| P0-CG-017 | SSRF em ExternalApprovalWebhookSender | Médio |

### P1 — Alta Prioridade (resolver no próximo sprint)

| ID | Título | Esforço |
|---|---|---|
| P1-CG-010 | IngestExternalRelease: CommitShas e WorkItems ignorados | Alto |
| P1-CG-011 | IngestExternalRelease: duplo commit (explicit + TransactionBehavior) | Baixo |
| P1-CG-012 | GetByExternalKeyAsync sem filtro de tenant | Baixo |
| P1-CG-013 | GetSummaryCountsAsync: 5 COUNT queries → 1 GROUP BY | Médio |
| P1-CG-016 | ReleaseRepository: métodos sem filtro de tenant | Médio |
| P1-CG-024 | Ausência de testes: IngestExternalRelease, PromotionRequest, WorkflowInstance | Alto |

### P2 — Média Prioridade (backlog prioritário)

| ID | Título | Esforço |
|---|---|---|
| P2-CG-001 | Release.ApprovalStatus: `string?` → enum `ReleaseApprovalStatus` | Médio |
| P2-CG-002 | Release.RowVersion: setter público → internal | Baixo |
| P2-CG-007 | PromotionRequest e WorkflowInstance RowVersion: setter público → internal | Baixo |
| P2-CG-008 | RequestedBy/SubmittedBy: usar ICurrentUser em vez de string de request | Médio |
| P2-CG-009 | PromotionGate CI → AuditableEntity após renomeação | Médio |
| P2-CG-018 | Callback token em URL → mover para body | Médio |
| P2-CG-019 | Endpoint respond: validar releaseId contra token | Baixo |
| P2-CG-021 | TypeScript types gerados via openapi-typescript | Alto |
| P2-CG-022 | ChangeDetailPage: migrar react-router-dom → TanStack Router | Baixo |

### P3 — Baixa Prioridade (melhorias incrementais)

| ID | Título | Esforço |
|---|---|---|
| P3-CG-020 | Endpoints DELETE/Cancel para releases, promotion requests | Médio |
| P3-CG-023 | Status strings no frontend → i18n keys | Baixo |

---

## Apêndice — Ficheiros-Chave Analisados

| Ficheiro | Problemas identificados |
|---|---|
| `Domain/ChangeIntelligence/Entities/Release.cs` | P2-CG-001, P2-CG-002 |
| `Domain/ChangeIntelligence/Entities/PromotionGate.cs` | P0-CG-003, P0-CG-004, P2-CG-009 |
| `Domain/Promotion/Entities/PromotionRequest.cs` | P0-CG-005, P2-CG-007, P2-CG-008 |
| `Domain/Promotion/Entities/PromotionGate.cs` | P0-CG-003 (duplicate) |
| `Domain/Workflow/Entities/WorkflowInstance.cs` | P0-CG-006, P2-CG-007, P2-CG-008 |
| `Application/ChangeIntelligence/Features/IngestExternalRelease/IngestExternalRelease.cs` | P1-CG-010, P1-CG-011 |
| `Application/ChangeIntelligence/Features/RequestExternalApproval/RequestExternalApproval.cs` | P0-CG-017 |
| `Infrastructure/ChangeIntelligence/Persistence/Repositories/ReleaseRepository.cs` | P1-CG-012, P1-CG-013, P1-CG-016 |
| `Infrastructure/ChangeIntelligence/Services/ExternalApprovalWebhookSender.cs` | P0-CG-017 |
| `Infrastructure/Promotion/Persistence/Repositories/PromotionRequestRepository.cs` | P0-CG-014 |
| `Infrastructure/Workflow/Persistence/Repositories/WorkflowInstanceRepository.cs` | P0-CG-015 |
| `API/ChangeIntelligence/Endpoints/Endpoints/ApprovalGatewayEndpoints.cs` | P2-CG-018, P2-CG-019 |
| `src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx` | P2-CG-022, P3-CG-023 |
| `src/frontend/src/features/change-governance/api/changeIntelligence.ts` | P2-CG-021 |
