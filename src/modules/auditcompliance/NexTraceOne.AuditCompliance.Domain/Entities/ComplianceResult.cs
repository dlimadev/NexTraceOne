using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AuditCompliance.Domain.Entities;

/// <summary>
/// Entidade que representa o resultado de uma avaliação de compliance de um recurso
/// contra uma política específica, opcionalmente no contexto de uma campanha.
/// </summary>
public sealed class ComplianceResult : Entity<ComplianceResultId>
{
    private ComplianceResult() { }

    /// <summary>Política avaliada.</summary>
    public CompliancePolicyId PolicyId { get; private set; } = default!;

    /// <summary>Campanha associada (opcional).</summary>
    public AuditCampaignId? CampaignId { get; private set; }

    /// <summary>Tipo do recurso avaliado.</summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>Identificador do recurso avaliado.</summary>
    public string ResourceId { get; private set; } = string.Empty;

    /// <summary>Resultado da avaliação.</summary>
    public ComplianceOutcome Outcome { get; private set; }

    /// <summary>Detalhes da avaliação em formato JSON.</summary>
    public string? Details { get; private set; }

    /// <summary>Utilizador ou sistema que executou a avaliação.</summary>
    public string EvaluatedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora da avaliação.</summary>
    public DateTimeOffset EvaluatedAt { get; private set; }

    /// <summary>Tenant onde a avaliação ocorreu.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Cria um novo resultado de compliance.</summary>
    public static ComplianceResult Create(
        CompliancePolicyId policyId,
        AuditCampaignId? campaignId,
        string resourceType,
        string resourceId,
        ComplianceOutcome outcome,
        string? details,
        string evaluatedBy,
        DateTimeOffset evaluatedAt,
        Guid tenantId)
    {
        Guard.Against.Null(policyId);

        return new ComplianceResult
        {
            Id = ComplianceResultId.New(),
            PolicyId = policyId,
            CampaignId = campaignId,
            ResourceType = Guard.Against.NullOrWhiteSpace(resourceType),
            ResourceId = Guard.Against.NullOrWhiteSpace(resourceId),
            Outcome = outcome,
            Details = details,
            EvaluatedBy = Guard.Against.NullOrWhiteSpace(evaluatedBy),
            EvaluatedAt = evaluatedAt,
            TenantId = tenantId
        };
    }
}

/// <summary>Identificador fortemente tipado de ComplianceResult.</summary>
public sealed record ComplianceResultId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ComplianceResultId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ComplianceResultId From(Guid id) => new(id);
}
