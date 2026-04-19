using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Feedback de utilizador sobre uma execução de skill.
/// Alimenta o ciclo de melhoria contínua (Agent Lightning RL).
/// Inclui rating, outcome observado e flag de correção para ajuste de comportamento.
/// </summary>
public sealed class AiSkillFeedback : AuditableEntity<AiSkillFeedbackId>
{
    private AiSkillFeedback() { }

    /// <summary>Execução de skill avaliada.</summary>
    public AiSkillExecutionId SkillExecutionId { get; private set; } = null!;

    /// <summary>Classificação numérica de 1 a 5.</summary>
    public int Rating { get; private set; }

    /// <summary>Outcome observado: "resolved", "partial" ou "incorrect".</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>Comentário textual opcional do utilizador.</summary>
    public string? Comment { get; private set; }

    /// <summary>Outcome real descrito pelo utilizador (para RL).</summary>
    public string? ActualOutcome { get; private set; }

    /// <summary>Indica se a resposta da skill estava correta.</summary>
    public bool WasCorrect { get; private set; }

    /// <summary>Utilizador que submeteu o feedback.</summary>
    public string SubmittedBy { get; private set; } = string.Empty;

    /// <summary>Tenant no qual o feedback foi submetido.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Timestamp da submissão do feedback.</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>Regista feedback sobre uma execução de skill.</summary>
    public static AiSkillFeedback Submit(
        AiSkillExecutionId skillExecutionId,
        int rating,
        string outcome,
        string? comment,
        string? actualOutcome,
        bool wasCorrect,
        string submittedBy,
        Guid tenantId,
        DateTimeOffset submittedAt)
    {
        Guard.Against.Null(skillExecutionId);
        Guard.Against.NullOrWhiteSpace(outcome);
        Guard.Against.NullOrWhiteSpace(submittedBy);
        Guard.Against.OutOfRange(rating, nameof(rating), 1, 5);

        return new AiSkillFeedback
        {
            Id = AiSkillFeedbackId.New(),
            SkillExecutionId = skillExecutionId,
            Rating = rating,
            Outcome = outcome.Trim(),
            Comment = comment?.Trim(),
            ActualOutcome = actualOutcome?.Trim(),
            WasCorrect = wasCorrect,
            SubmittedBy = submittedBy,
            TenantId = tenantId,
            SubmittedAt = submittedAt,
        };
    }
}

/// <summary>Identificador fortemente tipado de AiSkillFeedback.</summary>
public sealed record AiSkillFeedbackId(Guid Value) : TypedIdBase(Value)
{
    public static AiSkillFeedbackId New() => new(Guid.NewGuid());
    public static AiSkillFeedbackId From(Guid id) => new(id);
}
