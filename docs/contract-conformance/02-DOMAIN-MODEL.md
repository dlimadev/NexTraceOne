# Contract Conformance — Modelo de Domínio

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## 1. Novas Entidades de Domínio

Todas as novas entidades seguem os padrões estabelecidos no codebase:
- `sealed record TypedId(Guid Value)` para IDs fortemente tipados
- `AuditableEntity` base com `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- `RowVersion` (uint via xmin do PostgreSQL) para concorrência optimista
- Soft-delete via `IsDeleted`
- Prefixo `ctr_` nas tabelas (módulo Contracts)

---

## 2. `ContractConformanceCheck`

**Propósito:** Registo persistido do resultado de uma validação CI. Serve como evidência auditável de que a spec implementada foi verificada contra a spec desenhada.

**Aggregate Root:** Sim

```csharp
// Domain/Contracts/Entities/ContractConformanceCheck.cs
public sealed class ContractConformanceCheck : AuditableEntity
{
    public ContractConformanceCheckId Id { get; private set; }
    public ContractVersionId? ContractVersionId { get; private set; }   // Resolvido internamente
    public ServiceId ServiceId { get; private set; }
    public EnvironmentId EnvironmentId { get; private set; }
    public ContractCiTokenId? CiTokenId { get; private set; }           // Token usado, se aplicável
    public ConformanceStatus Status { get; private set; }               // Compliant | Drifted | Breaking | Error
    public ConformanceRecommendation Recommendation { get; private set; } // Approve | Warn | Block
    public int DeviationCount { get; private set; }
    public int BreakingDeviationCount { get; private set; }
    public int DriftDeviationCount { get; private set; }
    public decimal ConformanceScore { get; private set; }               // 0-100
    public string SourceSystem { get; private set; }                    // github-actions | jenkins | gitlab-ci | manual
    public string? PipelineRunId { get; private set; }
    public string? CommitSha { get; private set; }
    public string? BranchName { get; private set; }
    public string ImplementedSpecContent { get; private set; }          // Snapshot da spec validada
    public ContractProtocol ImplementedSpecFormat { get; private set; }
    public string? DeviationsSummaryJson { get; private set; }         // JSON com lista de desvios
    public ReleaseId? LinkedReleaseId { get; private set; }             // Ligação ao Change Intelligence
    public bool PolicyEnforced { get; private set; }                    // Se a política bloqueou
    public string? PolicyDecisionReason { get; private set; }
    public TenantId TenantId { get; private set; }
    public uint RowVersion { get; private set; }
}

// Enums
public enum ConformanceStatus { Compliant, Drifted, Breaking, Error, Skipped }
public enum ConformanceRecommendation { Approve, Warn, Block, Inconclusive }
```

**Tabela:** `ctr_conformance_checks`

**Índices relevantes:**
- `(ServiceId, EnvironmentId, CreatedAt DESC)` — histórico por serviço/ambiente
- `(ContractVersionId, CreatedAt DESC)` — histórico por versão de contrato
- `(TenantId, Status, CreatedAt DESC)` — dashboard por tenant

---

## 3. `ContractChangelogEntry`

**Propósito:** Linha do tempo imutável de eventos relevantes de um contrato. Não é uma tabela de auditoria genérica — é um log de negócio orientado ao contrato como entidade central.

**Tipo:** Entity (pertence ao aggregate `ApiAsset` existente via `apiAssetId`)

```csharp
// Domain/Contracts/Entities/ContractChangelogEntry.cs
public sealed class ContractChangelogEntry : AuditableEntity
{
    public ContractChangelogEntryId Id { get; private set; }
    public ApiAssetId ApiAssetId { get; private set; }                  // Contrato (asset estável)
    public ContractVersionId? ContractVersionId { get; private set; }   // Versão específica, se aplicável
    public ContractChangelogEventType EventType { get; private set; }
    public ChangeLevel? ChangeLevel { get; private set; }               // Breaking | NonBreaking | Patch
    public string Title { get; private set; }                           // Título legível do evento
    public string? Description { get; private set; }
    public string? TriggeredBySystem { get; private set; }              // github-actions | nextraceone-ui | api
    public string? DiffSummaryJson { get; private set; }                // Resumo do diff para o changelog
    public string? AffectedConsumersJson { get; private set; }          // Consumers impactados
    public ContractConformanceCheckId? LinkedConformanceCheckId { get; private set; }
    public ReleaseId? LinkedReleaseId { get; private set; }
    public bool IsBreaking { get; private set; }
    public bool RequiresConsumerAction { get; private set; }
    public TenantId TenantId { get; private set; }
    public DateTime HappenedAt { get; private set; }                    // Quando o evento ocorreu
}

// Enum de tipos de evento de changelog
public enum ContractChangelogEventType
{
    // Design
    DraftCreated,
    DraftUpdated,
    DraftSubmittedForReview,
    DraftApproved,
    DraftRejected,
    ContractPublished,
    ContractLocked,

    // Versionamento
    VersionBumped,
    BreakingChangeDetected,
    NonBreakingChangeDetected,

    // Conformance (CI)
    ConformanceCheckPassed,
    ConformanceCheckFailed,
    ConformanceDriftDetected,
    ConformanceCheckSkipped,

    // Ciclo de vida
    ContractDeprecated,
    ContractSunset,
    ContractRetired,

    // Consumers
    ConsumerNotified,
    ConsumerAcceptedChange,
    ConsumerRejectedChange,

    // Runtime
    RuntimeDriftDetected,
    RuntimeDriftResolved,

    // Governança
    PolicyViolationDetected,
    PolicyExceptionGranted
}
```

**Tabela:** `ctr_contract_changelog`

**Índices relevantes:**
- `(ApiAssetId, HappenedAt DESC)` — changelog por contrato
- `(TenantId, EventType, HappenedAt DESC)` — feed global por tenant
- `(ContractVersionId)` — eventos por versão
- `(IsBreaking, TenantId, HappenedAt DESC)` — feed de breaking changes

---

## 4. `ContractCiToken`

**Propósito:** Token de CI com binding a serviço. Permite que pipelines CI identifiquem o serviço sem precisar de passar GUIDs manualmente.

**Aggregate Root:** Sim

```csharp
// Domain/Contracts/Entities/ContractCiToken.cs
public sealed class ContractCiToken : AuditableEntity
{
    public ContractCiTokenId Id { get; private set; }
    public ServiceId ServiceId { get; private set; }
    public string Name { get; private set; }                            // Nome descritivo, ex: "payment-api-ci"
    public string KeyHash { get; private set; }                         // SHA-256 do token (nunca raw)
    public string KeyPrefix { get; private set; }                       // Primeiros 8 chars para identificação
    public IReadOnlyList<string> AllowedEnvironments { get; private set; } // Ambientes permitidos
    public IReadOnlyList<string> AllowedScopes { get; private set; }    // contracts:validate | contracts:read
    public bool IsActive { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByUserId { get; private set; }
    public string? RevokedReason { get; private set; }
    public int UsageCount { get; private set; }
    public TenantId TenantId { get; private set; }
    public uint RowVersion { get; private set; }

    // Domain methods
    public static (ContractCiToken token, string rawKey) Create(...) { }
    public void Revoke(string revokedByUserId, string reason) { }
    public void RecordUsage() { }
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    public bool IsValid() => IsActive && !IsExpired() && RevokedAt is null;
}
```

**Tabela:** `ctr_ci_tokens`

**Segurança:**
- O token raw **nunca é persistido** — apenas o hash SHA-256 e o prefixo
- O token raw é retornado **uma única vez** na criação
- Segue o mesmo padrão do `ApiKey` existente em `cat_api_keys`

---

## 5. `ContractConformancePolicy`

**Propósito:** Define as regras de conformance aplicáveis a um serviço, equipa ou tenant. Determina quando bloquear, quando avisar e quais desvios são aceitáveis.

**Nota de implementação:** Esta entidade **não deve ser uma nova tabela de domínio**. Deve ser implementada como `ConfigurationDefinition` + `ConfigurationEntry` no módulo `Configuration` existente, respeitando a arquitectura estabelecida. Ver [03-CONFIGURATION-PARAMETERS.md](03-CONFIGURATION-PARAMETERS.md) para o detalhe completo.

O serviço de aplicação `ContractConformancePolicyService` lê as configurações via o módulo `Configuration` e constrói um objecto de política em memória:

```csharp
// Application/Contracts/Services/ContractConformancePolicyService.cs
public sealed class ContractConformancePolicyService
{
    public Task<ConformancePolicy> ResolveForServiceAsync(
        ServiceId serviceId,
        EnvironmentId environmentId,
        CancellationToken ct);
}

// Value Object (em memória, não persistido directamente)
public sealed record ConformancePolicy
{
    public BlockingPolicy BlockingPolicy { get; init; }
    public decimal ScoreThreshold { get; init; }
    public bool AllowAdditionalEndpoints { get; init; }
    public bool AllowAdditionalFields { get; init; }
    public IReadOnlyList<string> IgnoredPaths { get; init; }
    public bool IsRequired { get; init; }
    public ResolutionStrategy ResolutionStrategy { get; init; }
}

public enum BlockingPolicy { BreakingOnly, AnyDrift, ScoreBelowThreshold, WarnOnly, Disabled }
public enum ResolutionStrategy { Auto, SlugPlusEnvironment, CiTokenBound, ExplicitId }
```

---

## 6. `IActiveContractResolver`

**Propósito:** Domain service que resolve qual `ContractVersion` está activa para um dado serviço + ambiente. Centraliza a lógica de resolução usada em múltiplos contextos.

```csharp
// Application/Contracts/Abstractions/IActiveContractResolver.cs
public interface IActiveContractResolver
{
    Task<ContractVersion?> ResolveAsync(
        ContractResolutionContext context,
        CancellationToken ct);
}

public sealed record ContractResolutionContext
{
    // Opção 1 — Token de CI (binding directo)
    public ContractCiTokenId? CiTokenId { get; init; }

    // Opção 2 — Slug + Ambiente (mais comum em CI)
    public string? ServiceSlug { get; init; }
    public string? EnvironmentName { get; init; }

    // Opção 3 — ApiAsset ID (estável, não muda por versão)
    public ApiAssetId? ApiAssetId { get; init; }

    // Opção 4 — ID de versão explícito (fallback administrativo)
    public ContractVersionId? ContractVersionId { get; init; }
}
```

**Hierarquia de resolução interna:**

```
1. ContractVersionId explícito → acesso directo
2. ApiAssetId → versão com estado Locked mais recente
            → fallback: versão Approved mais recente (com warning)
3. ServiceSlug + EnvironmentName
            → lookup Service por slug
            → ContractDeployment registado para esse ambiente
            → fallback: versão Locked mais recente do serviço
4. CiTokenId → resolve ServiceId a partir do token
            → aplica lógica do ponto 3
5. Nenhum encontrado → Result.Failure com mensagem explicativa
```

---

## 7. Sumário das tabelas novas

| Tabela | Entidade | Módulo |
|--------|----------|--------|
| `ctr_conformance_checks` | `ContractConformanceCheck` | Contracts |
| `ctr_contract_changelog` | `ContractChangelogEntry` | Contracts |
| `ctr_ci_tokens` | `ContractCiToken` | Contracts |

As políticas de conformance são persistidas no módulo `Configuration` via `cfg_configuration_entries` — sem nova tabela.
