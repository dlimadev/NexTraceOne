using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AuditCompliance.Domain.Entities;

/// <summary>
/// Entidade que representa uma campanha de auditoria (periódica, ad-hoc ou regulatória).
/// Agrupa avaliações de compliance realizadas num período ou contexto específico.
/// </summary>
public sealed class AuditCampaign : Entity<AuditCampaignId>
{
    private AuditCampaign() { }

    /// <summary>Nome da campanha.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional da campanha.</summary>
    public string? Description { get; private set; }

    /// <summary>Tipo da campanha (e.g. Periodic, AdHoc, Regulatory).</summary>
    public string CampaignType { get; private set; } = string.Empty;

    /// <summary>Estado atual da campanha.</summary>
    public CampaignStatus Status { get; private set; }

    /// <summary>Data/hora agendada para início.</summary>
    public DateTimeOffset? ScheduledStartAt { get; private set; }

    /// <summary>Data/hora de início efetivo.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Data/hora de conclusão.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Tenant proprietário da campanha.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Utilizador que criou a campanha.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Cria uma nova campanha de auditoria.</summary>
    public static AuditCampaign Create(
        string name,
        string? description,
        string campaignType,
        DateTimeOffset? scheduledStartAt,
        Guid tenantId,
        string createdBy,
        DateTimeOffset createdAt)
    {
        return new AuditCampaign
        {
            Id = AuditCampaignId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Description = description,
            CampaignType = Guard.Against.NullOrWhiteSpace(campaignType),
            Status = CampaignStatus.Planned,
            ScheduledStartAt = scheduledStartAt,
            TenantId = tenantId,
            CreatedBy = Guard.Against.NullOrWhiteSpace(createdBy),
            CreatedAt = createdAt
        };
    }

    /// <summary>Inicia a campanha.</summary>
    public void Start(DateTimeOffset startedAt)
    {
        if (Status != CampaignStatus.Planned)
        {
            throw new InvalidOperationException($"Cannot start campaign in status '{Status}'.");
        }

        Status = CampaignStatus.InProgress;
        StartedAt = startedAt;
    }

    /// <summary>Conclui a campanha.</summary>
    public void Complete(DateTimeOffset completedAt)
    {
        if (Status != CampaignStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete campaign in status '{Status}'.");
        }

        Status = CampaignStatus.Completed;
        CompletedAt = completedAt;
    }

    /// <summary>Cancela a campanha.</summary>
    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel campaign in status '{Status}'.");
        }

        Status = CampaignStatus.Cancelled;
        CompletedAt = cancelledAt;
    }
}

/// <summary>Identificador fortemente tipado de AuditCampaign.</summary>
public sealed record AuditCampaignId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AuditCampaignId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AuditCampaignId From(Guid id) => new(id);
}
