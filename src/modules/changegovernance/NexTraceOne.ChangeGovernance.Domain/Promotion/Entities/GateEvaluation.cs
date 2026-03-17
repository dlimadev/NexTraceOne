using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

/// <summary>
/// Entidade que registra o resultado da avaliação de um gate para uma solicitação de promoção.
/// Cada avaliação indica se o gate foi satisfeito e por quem, com possibilidade de override justificado.
/// </summary>
public sealed class GateEvaluation : AuditableEntity<GateEvaluationId>
{
    private GateEvaluation() { }

    /// <summary>Identificador da solicitação de promoção avaliada.</summary>
    public PromotionRequestId PromotionRequestId { get; private set; } = default!;

    /// <summary>Identificador do gate que foi avaliado.</summary>
    public PromotionGateId PromotionGateId { get; private set; } = default!;

    /// <summary>Indica se o gate foi satisfeito (true) ou falhou (false).</summary>
    public bool Passed { get; private set; }

    /// <summary>Identificador do usuário ou sistema que realizou a avaliação.</summary>
    public string EvaluatedBy { get; private set; } = string.Empty;

    /// <summary>Detalhes adicionais sobre o resultado da avaliação.</summary>
    public string? EvaluationDetails { get; private set; }

    /// <summary>Justificativa para override do resultado do gate, quando aplicável.</summary>
    public string? OverrideJustification { get; private set; }

    /// <summary>Data/hora UTC em que a avaliação foi realizada.</summary>
    public DateTimeOffset EvaluatedAt { get; private set; }

    /// <summary>
    /// Cria uma nova avaliação de gate para uma solicitação de promoção.
    /// </summary>
    public static GateEvaluation Create(
        PromotionRequestId requestId,
        PromotionGateId gateId,
        bool passed,
        string evaluatedBy,
        string? details,
        DateTimeOffset evaluatedAt)
    {
        Guard.Against.Null(requestId);
        Guard.Against.Null(gateId);
        Guard.Against.NullOrWhiteSpace(evaluatedBy);
        Guard.Against.StringTooLong(evaluatedBy, 500);

        if (details is not null)
            Guard.Against.StringTooLong(details, 4000);

        return new GateEvaluation
        {
            Id = GateEvaluationId.New(),
            PromotionRequestId = requestId,
            PromotionGateId = gateId,
            Passed = passed,
            EvaluatedBy = evaluatedBy,
            EvaluationDetails = details,
            EvaluatedAt = evaluatedAt
        };
    }

    /// <summary>
    /// Registra um override na avaliação do gate, alterando o resultado para aprovado.
    /// Requer justificativa obrigatória e identifica quem realizou o override.
    /// </summary>
    public void Override(string justification, string overriddenBy, DateTimeOffset at)
    {
        Guard.Against.NullOrWhiteSpace(justification);
        Guard.Against.StringTooLong(justification, 4000);
        Guard.Against.NullOrWhiteSpace(overriddenBy);

        OverrideJustification = justification;
        Passed = true;
        EvaluatedBy = overriddenBy;
        EvaluatedAt = at;
    }
}

/// <summary>Identificador fortemente tipado de GateEvaluation.</summary>
public sealed record GateEvaluationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static GateEvaluationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static GateEvaluationId From(Guid id) => new(id);
}
