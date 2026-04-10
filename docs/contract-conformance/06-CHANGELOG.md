# Contract Conformance — Plano de Implementação do Changelog

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## 1. O que é o Changelog de Contratos

O Changelog de Contratos é uma **linha do tempo imutável e auditável** de todos os eventos relevantes que aconteceram a um contrato ao longo da sua vida. Não é uma tabela de auditoria técnica — é um registo de negócio orientado ao contrato como entidade central do produto.

**Diferença do audit trail genérico:**

| Audit Trail (`CreatedBy/UpdatedBy`) | Changelog de Contratos |
|--------------------------------------|------------------------|
| Registo técnico de mutations | Registo de eventos de negócio |
| "O campo X foi alterado de A para B" | "Breaking change detectado no CI" |
| Focado em campos e tabelas | Focado em eventos do domínio |
| Não tem semântica de contrato | Tem tipo, impacto, consumers, links |
| Não é consultável por evento | Filtros por tipo, breaking, período |

---

## 2. Eventos que geram entradas de Changelog

### Eventos de Design (NexTraceOne Studio)

| Evento | `ContractChangelogEventType` | Gerado quando |
|--------|------------------------------|---------------|
| Draft criado | `DraftCreated` | `CreateDraft` command executado |
| Draft submetido para review | `DraftSubmittedForReview` | `SubmitDraftForReview` command |
| Draft aprovado | `DraftApproved` | `ApproveDraft` command |
| Draft rejeitado | `DraftRejected` | `RejectDraft` command |
| Contrato publicado | `ContractPublished` | `PublishDraft` command |
| Contrato bloqueado | `ContractLocked` | `LockContractVersion` command |
| Breaking change detectado | `BreakingChangeDetected` | `ClassifyBreakingChange` — se Breaking |
| Non-breaking change | `NonBreakingChangeDetected` | `ClassifyBreakingChange` — se NonBreaking |
| Versão sugerida actualizada | `VersionBumped` | `SuggestSemanticVersion` aplicada |

### Eventos de Conformance CI

| Evento | `ContractChangelogEventType` | Gerado quando |
|--------|------------------------------|---------------|
| Check passou | `ConformanceCheckPassed` | `ValidateImplementation` → Compliant |
| Check falhou | `ConformanceCheckFailed` | `ValidateImplementation` → Breaking |
| Drift detectado | `ConformanceDriftDetected` | `ValidateImplementation` → Drifted |
| Check ignorado | `ConformanceCheckSkipped` | Ambiente não obrigatório / policy=disabled |

### Eventos de Ciclo de Vida

| Evento | `ContractChangelogEventType` | Gerado quando |
|--------|------------------------------|---------------|
| Contrato depreciado | `ContractDeprecated` | `DeprecateContractVersion` command |
| Sunset agendado | `ContractSunset` | Data de sunset atingida / agendada |
| Contrato retirado | `ContractRetired` | Transição para estado Retired |

### Eventos de Runtime

| Evento | `ContractChangelogEventType` | Gerado quando |
|--------|------------------------------|---------------|
| Drift em runtime detectado | `RuntimeDriftDetected` | `DetectContractDrift` — desvio encontrado |
| Drift em runtime resolvido | `RuntimeDriftResolved` | `DetectContractDrift` — desvio desapareceu |

### Eventos de Consumers

| Evento | `ContractChangelogEventType` | Gerado quando |
|--------|------------------------------|---------------|
| Consumer notificado | `ConsumerNotified` | Webhook/notificação enviada ao consumer |
| Consumer aceitou mudança | `ConsumerAcceptedChange` | Consumer fez acknowledge do breaking change |

### Eventos de Governança

| Evento | `ContractChangelogEventType` | Gerado quando |
|--------|------------------------------|---------------|
| Violação de política | `PolicyViolationDetected` | Lint/ruleset violation em publicação |
| Excepção de política concedida | `PolicyExceptionGranted` | Override de política aprovado |

---

## 3. Arquitectura de geração do Changelog

### Princípio: domain events → changelog handler

O changelog **não deve ser gerado inline** nos command handlers. Deve ser gerado através de domain events — seguindo o padrão já estabelecido no codebase.

```
Command Handler executa lógica de negócio
        │
        └─ Domain Event raised (ex: ContractPublishedDomainEvent)
                │
                └─ ContractChangelogDomainEventHandler
                        │
                        └─ Verifica config: changelog.auto_generate = true?
                                │
                                └─ Cria ContractChangelogEntry + persiste
```

### Handlers a criar

```csharp
// Application/Contracts/EventHandlers/

ContractPublishedChangelogHandler          : IDomainEventHandler<ContractPublishedDomainEvent>
BreakingChangeDetectedChangelogHandler     : IDomainEventHandler<BreakingChangeDetectedDomainEvent>
DraftApprovedChangelogHandler              : IDomainEventHandler<DraftApprovedDomainEvent>
DraftRejectedChangelogHandler              : IDomainEventHandler<DraftRejectedDomainEvent>
ContractDeprecatedChangelogHandler         : IDomainEventHandler<ContractDeprecatedDomainEvent>
ConformanceCheckResultChangelogHandler     : IDomainEventHandler<ConformanceCheckCompletedDomainEvent>
RuntimeDriftChangelogHandler               : IDomainEventHandler<ContractDriftDetectedDomainEvent>
```

### Domain Events novos a declarar

```csharp
// Domain/Contracts/Events/
public sealed record ConformanceCheckCompletedDomainEvent(
    ContractConformanceCheckId CheckId,
    ApiAssetId ApiAssetId,
    ContractVersionId? ContractVersionId,
    ConformanceStatus Status,
    int BreakingDeviationCount,
    string SourceSystem,
    TenantId TenantId
) : IDomainEvent;
```

---

## 4. Título e descrição das entradas (legibilidade)

Cada entrada de changelog deve ter título e descrição legíveis por humanos (não técnicos):

```csharp
// Application/Contracts/Services/ContractChangelogTitleGenerator.cs
public static class ContractChangelogTitleGenerator
{
    public static string Generate(ContractChangelogEventType type, ChangelogContext ctx)
        => type switch
        {
            ContractPublished      => $"Version {ctx.Version} published",
            BreakingChangeDetected => $"Breaking change detected — {ctx.BreakingCount} issue(s)",
            ConformanceCheckPassed => $"CI conformance passed — score {ctx.Score:F1}",
            ConformanceCheckFailed => $"CI conformance failed — {ctx.BreakingCount} breaking deviation(s)",
            ConformanceDriftDetected => $"CI drift detected — {ctx.DriftCount} deviation(s)",
            ContractDeprecated     => $"Contract deprecated — sunset {ctx.SunsetDate:d}",
            RuntimeDriftDetected   => $"Runtime drift detected in {ctx.EnvironmentName}",
            _                      => type.ToString()
        };
}
```

---

## 5. Queries do Changelog

### Feature: `GetContractChangelog`

```csharp
// Application/Contracts/Features/GetContractChangelog/
public sealed record GetContractChangelogQuery(
    ApiAssetId ApiAssetId,
    ContractChangelogEventType[]? EventTypes,
    bool? IsBreakingOnly,
    DateTime? From,
    DateTime? To,
    int PageNumber,
    int PageSize,
    CancellationToken CancellationToken
) : IQuery<PagedResult<ContractChangelogEntryDto>>;
```

### Feature: `GetContractChangelogFeed`

Feed global para dashboards de equipa e governança — agrega changelog de múltiplos contratos do tenant, com filtros por equipa, tipo de evento e período.

```csharp
public sealed record GetContractChangelogFeedQuery(
    TeamId? TeamId,
    bool? IsBreakingOnly,
    ContractChangelogEventType[]? EventTypes,
    DateTime? From,
    DateTime? To,
    int PageNumber,
    int PageSize,
    CancellationToken CancellationToken
) : IQuery<PagedResult<ContractChangelogEntryDto>>;
```

---

## 6. Retenção e limpeza

O changelog tem retenção configurável via `contracts.changelog.retention_days`.

A limpeza é feita por um Quartz.NET job:

```csharp
// Infrastructure/Contracts/Jobs/ContractChangelogRetentionJob.cs
[DisallowConcurrentExecution]
public sealed class ContractChangelogRetentionJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // Para cada tenant: apaga entradas com HappenedAt < (agora - retention_days)
        // Excepto entradas IsBreaking = true (retidas sempre por 5 anos, valor fixo)
    }
}
```

**Regra especial de retenção:** Entradas com `IsBreaking = true` têm retenção mínima de **5 anos**, independentemente da configuração de tenant — por razões de auditoria e conformidade.

---

## 7. Exposição no Frontend

### Tab "Changelog" no ContractWorkspacePage

A tab mostra a linha do tempo do contrato com filtros:

```
ContractWorkspacePage → Tab "Changelog"
  ├─ Timeline visual (mais recente primeiro)
  ├─ Filtros: Tipo de evento | Breaking | Período
  ├─ Cada entrada:
  │   ├─ Ícone por tipo (Design / Conformance / Lifecycle / Runtime)
  │   ├─ Título legível
  │   ├─ Data/hora relativa ("há 2 horas")
  │   ├─ Badge "BREAKING" se isBreaking = true
  │   ├─ Link para ConformanceCheck ou Release associados
  │   └─ Expande para ver descrição e desvios
  └─ Export para CSV (para auditores)
```

### Feed de Changelog no Dashboard (por equipa)

No dashboard de Team Lead / Architect:
- Widget "Recent Contract Events" — últimas N entradas de changelog da equipa
- Filtro por "Breaking only" com badge de alerta

---

## 8. Tabela de base de dados

```sql
-- ctr_contract_changelog
CREATE TABLE ctr_contract_changelog (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_asset_id UUID NOT NULL,
    contract_version_id UUID NULL,
    event_type SMALLINT NOT NULL,           -- enum ContractChangelogEventType
    change_level SMALLINT NULL,             -- enum ChangeLevel
    title VARCHAR(500) NOT NULL,
    description TEXT NULL,
    triggered_by_system VARCHAR(100) NULL,
    diff_summary_json TEXT NULL,
    affected_consumers_json TEXT NULL,
    linked_conformance_check_id UUID NULL,
    linked_release_id UUID NULL,
    is_breaking BOOLEAN NOT NULL DEFAULT FALSE,
    requires_consumer_action BOOLEAN NOT NULL DEFAULT FALSE,
    tenant_id UUID NOT NULL,
    happened_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    created_by VARCHAR(256) NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    updated_by VARCHAR(256) NOT NULL
);

CREATE INDEX idx_ctr_changelog_asset_happened
    ON ctr_contract_changelog (api_asset_id, happened_at DESC);

CREATE INDEX idx_ctr_changelog_tenant_breaking
    ON ctr_contract_changelog (tenant_id, is_breaking, happened_at DESC)
    WHERE is_breaking = TRUE;

CREATE INDEX idx_ctr_changelog_tenant_feed
    ON ctr_contract_changelog (tenant_id, happened_at DESC);
```

**Nota:** Não tem `IsDeleted` — entradas de changelog são imutáveis por design. Para remoção, usa-se apenas o job de retenção por data.
