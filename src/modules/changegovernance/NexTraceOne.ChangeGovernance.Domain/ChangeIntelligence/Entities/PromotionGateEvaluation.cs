using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Resultado da avaliação de um gate de promoção contra uma mudança específica.
/// Entidade imutável (append-only) que registra o resultado e os detalhes
/// individuais de cada regra avaliada, permitindo auditoria completa.
/// </summary>
public sealed class PromotionGateEvaluation : Entity<PromotionGateEvaluationId>
{
    private PromotionGateEvaluation() { }

    /// <summary>Identificador do gate de promoção avaliado.</summary>
    public PromotionGateId GateId { get; private set; } = null!;

    /// <summary>Identificador da mudança avaliada.</summary>
    public string ChangeId { get; private set; } = string.Empty;

    /// <summary>Resultado consolidado da avaliação.</summary>
    public GateEvaluationResult Result { get; private set; }

    /// <summary>Resultados individuais das regras em formato JSON (JSONB no banco).</summary>
    public string? RuleResults { get; private set; }

    /// <summary>Momento em que a avaliação foi realizada.</summary>
    public DateTimeOffset EvaluatedAt { get; private set; }

    /// <summary>Identificador do utilizador ou sistema que realizou a avaliação.</summary>
    public string? EvaluatedBy { get; private set; }

    /// <summary>Identificador do tenant ao qual a avaliação pertence.</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (xmin no PostgreSQL).</summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Cria uma nova avaliação de gate de promoção com validação dos campos obrigatórios.
    /// </summary>
    public static PromotionGateEvaluation Evaluate(
        PromotionGateId gateId,
        string changeId,
        GateEvaluationResult result,
        string? ruleResults,
        DateTimeOffset evaluatedAt,
        string? evaluatedBy,
        string? tenantId)
    {
        Guard.Against.Null(gateId, nameof(gateId));
        Guard.Against.NullOrWhiteSpace(changeId, nameof(changeId));
        Guard.Against.StringTooLong(changeId, 200, nameof(changeId));
        Guard.Against.EnumOutOfRange(result, nameof(result));

        if (evaluatedBy is not null)
            Guard.Against.StringTooLong(evaluatedBy, 200, nameof(evaluatedBy));

        return new PromotionGateEvaluation
        {
            Id = PromotionGateEvaluationId.New(),
            GateId = gateId,
            ChangeId = changeId,
            Result = result,
            RuleResults = ruleResults,
            EvaluatedAt = evaluatedAt,
            EvaluatedBy = evaluatedBy,
            TenantId = tenantId
        };
    }
}

/// <summary>Identificador fortemente tipado de PromotionGateEvaluation.</summary>
public sealed record PromotionGateEvaluationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromotionGateEvaluationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromotionGateEvaluationId From(Guid id) => new(id);
}
