using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade que representa a associação de um work item (história, task, bug, feature)
/// de um sistema externo (Jira, AzureDevOps, GitHub, etc.) a uma release.
///
/// O PO/PM pode adicionar ou remover work items da release a qualquer momento
/// enquanto ela estiver em estado mutável (Draft/Planned/InDevelopment).
/// O histórico de remoções é preservado via IsActive=false + RemovedAt.
/// </summary>
public sealed class WorkItemAssociation : Entity<WorkItemAssociationId>
{
    private WorkItemAssociation() { }

    /// <summary>Identificador da release a que este work item está vinculado.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>ID do work item no sistema externo (ex: PAY-1234, AB#567, #42).</summary>
    public string ExternalWorkItemId { get; private set; } = string.Empty;

    /// <summary>Sistema externo de onde o work item provém.</summary>
    public ExternalWorkItemSystem ExternalSystem { get; private set; }

    /// <summary>Título/sumário do work item (cacheado do sistema externo).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Tipo do work item (Story, Bug, Feature, Task, Epic).</summary>
    public string WorkItemType { get; private set; } = string.Empty;

    /// <summary>Status cacheado do work item no sistema externo.</summary>
    public string? ExternalStatus { get; private set; }

    /// <summary>URL do work item no sistema externo.</summary>
    public string? ExternalUrl { get; private set; }

    /// <summary>Utilizador que adicionou o work item à release.</summary>
    public string AddedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o work item foi adicionado.</summary>
    public DateTimeOffset AddedAt { get; private set; }

    /// <summary>Utilizador que removeu o work item da release (nullable — preenchido apenas na remoção).</summary>
    public string? RemovedBy { get; private set; }

    /// <summary>Data/hora UTC em que o work item foi removido (nullable — preenchido apenas na remoção).</summary>
    public DateTimeOffset? RemovedAt { get; private set; }

    /// <summary>Indica se o work item está activo na release. false = removido pelo PO/PM (histórico preservado).</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Cria uma nova associação de work item a uma release.
    /// </summary>
    public static WorkItemAssociation Create(
        Guid tenantId,
        ReleaseId releaseId,
        string externalWorkItemId,
        ExternalWorkItemSystem externalSystem,
        string title,
        string workItemType,
        string? externalStatus,
        string? externalUrl,
        string addedBy,
        DateTimeOffset addedAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.NullOrWhiteSpace(externalWorkItemId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(workItemType);
        Guard.Against.NullOrWhiteSpace(addedBy);

        return new WorkItemAssociation
        {
            Id = WorkItemAssociationId.New(),
            TenantId = tenantId,
            ReleaseId = releaseId,
            ExternalWorkItemId = externalWorkItemId,
            ExternalSystem = externalSystem,
            Title = title,
            WorkItemType = workItemType,
            ExternalStatus = externalStatus,
            ExternalUrl = externalUrl,
            AddedBy = addedBy,
            AddedAt = addedAt,
            IsActive = true,
        };
    }

    /// <summary>
    /// Remove o work item da release (soft-delete — histórico preservado).
    /// Só pode remover se IsActive == true.
    /// </summary>
    public void Remove(string removedBy, DateTimeOffset removedAt)
    {
        Guard.Against.NullOrWhiteSpace(removedBy);
        if (!IsActive) return;

        RemovedBy = removedBy;
        RemovedAt = removedAt;
        IsActive = false;
    }
}

/// <summary>Identificador fortemente tipado de WorkItemAssociation.</summary>
public sealed record WorkItemAssociationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkItemAssociationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkItemAssociationId From(Guid id) => new(id);
}
