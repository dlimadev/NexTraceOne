using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Registo de revisão de um draft de contrato.
/// Captura a decisão do revisor (aprovação ou rejeição), comentários e timestamps.
/// Garante rastreabilidade completa do fluxo de aprovação para auditoria.
/// </summary>
public sealed class ContractReview : Entity<ContractReviewId>
{
    private ContractReview() { }

    /// <summary>Identificador do draft que foi revisado.</summary>
    public ContractDraftId DraftId { get; private set; } = null!;

    /// <summary>Usuário que realizou a revisão.</summary>
    public string ReviewedBy { get; private set; } = string.Empty;

    /// <summary>Decisão da revisão: Approved ou Rejected.</summary>
    public ReviewDecision Decision { get; private set; }

    /// <summary>Comentário do revisor.</summary>
    public string Comment { get; private set; } = string.Empty;

    /// <summary>Timestamp da revisão.</summary>
    public DateTimeOffset ReviewedAt { get; private set; }

    /// <summary>
    /// Cria um registro de revisão para um draft de contrato.
    /// </summary>
    public static ContractReview Create(
        ContractDraftId draftId,
        string reviewedBy,
        ReviewDecision decision,
        string comment,
        DateTimeOffset reviewedAt)
    {
        Guard.Against.Null(draftId);
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        return new ContractReview
        {
            Id = ContractReviewId.New(),
            DraftId = draftId,
            ReviewedBy = reviewedBy,
            Decision = decision,
            Comment = comment ?? string.Empty,
            ReviewedAt = reviewedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractReview.</summary>
public sealed record ContractReviewId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractReviewId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractReviewId From(Guid id) => new(id);
}
