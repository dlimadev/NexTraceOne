using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Feedback de trajectória para Agent Lightning — permite RL nos agents.
/// Captura outcome real, rating 1-5, se o resultado estava correcto,
/// e outcome específico do domínio para cálculo de reward no trainer externo.
///
/// Diferente de AiFeedback (simples), esta entidade inclui:
/// - campo ActualOutcome para fechar o loop de aprendizagem
/// - ExportedForTraining para controlo de exportação batch
/// - TimeToResolveMinutes para cálculo de reward
/// </summary>
public sealed class AiAgentTrajectoryFeedback : AuditableEntity<AiAgentTrajectoryFeedbackId>
{
    private AiAgentTrajectoryFeedback() { }

    /// <summary>Execução de agent à qual este feedback se refere.</summary>
    public AiAgentExecutionId ExecutionId { get; private set; } = null!;

    /// <summary>Classificação de 1 a 5 atribuída pelo utilizador.</summary>
    public int Rating { get; private set; }

    /// <summary>Outcome da execução: "resolved" | "partial" | "incorrect".</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>Comentário opcional do utilizador.</summary>
    public string? Comment { get; private set; }

    /// <summary>O que realmente aconteceu (para fechar o loop de feedback).</summary>
    public string? ActualOutcome { get; private set; }

    /// <summary>Indica se o resultado produzido pelo agent estava correcto.</summary>
    public bool WasCorrect { get; private set; }

    /// <summary>Tempo para resolução em minutos (usado no cálculo de reward).</summary>
    public int? TimeToResolveMinutes { get; private set; }

    /// <summary>Utilizador que submeteu o feedback.</summary>
    public string SubmittedBy { get; private set; } = string.Empty;

    /// <summary>Tenant no qual o feedback foi submetido.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Momento em que o feedback foi submetido.</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>Indica se este feedback já foi exportado para o trainer externo.</summary>
    public bool ExportedForTraining { get; private set; }

    /// <summary>Momento em que foi exportado para treino (null se ainda não exportado).</summary>
    public DateTimeOffset? ExportedAt { get; private set; }

    /// <summary>
    /// Submete um novo feedback de trajectória para uma execução de agent.
    /// </summary>
    public static AiAgentTrajectoryFeedback Submit(
        AiAgentExecutionId executionId,
        int rating,
        string outcome,
        string? comment,
        string? actualOutcome,
        bool wasCorrect,
        int? timeToResolveMinutes,
        string submittedBy,
        Guid tenantId,
        DateTimeOffset submittedAt)
    {
        Guard.Against.Null(executionId);
        Guard.Against.OutOfRange(rating, nameof(rating), 1, 5);
        Guard.Against.NullOrWhiteSpace(outcome);
        Guard.Against.NullOrWhiteSpace(submittedBy);

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        return new AiAgentTrajectoryFeedback
        {
            Id = AiAgentTrajectoryFeedbackId.New(),
            ExecutionId = executionId,
            Rating = rating,
            Outcome = outcome,
            Comment = comment,
            ActualOutcome = actualOutcome,
            WasCorrect = wasCorrect,
            TimeToResolveMinutes = timeToResolveMinutes,
            SubmittedBy = submittedBy,
            TenantId = tenantId,
            SubmittedAt = submittedAt,
            ExportedForTraining = false,
            ExportedAt = null,
        };
    }

    /// <summary>Marca este feedback como exportado para o trainer externo.</summary>
    public void MarkExported(DateTimeOffset exportedAt)
    {
        ExportedForTraining = true;
        ExportedAt = exportedAt;
    }
}

/// <summary>Identificador fortemente tipado de AiAgentTrajectoryFeedback.</summary>
public sealed record AiAgentTrajectoryFeedbackId(Guid Value) : TypedIdBase(Value)
{
    public static AiAgentTrajectoryFeedbackId New() => new(Guid.NewGuid());
    public static AiAgentTrajectoryFeedbackId From(Guid id) => new(id);
}
