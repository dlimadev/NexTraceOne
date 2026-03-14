using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Avaliação de viabilidade de rollback para uma release.
/// O módulo não executa rollback, mas informa se é viável e com qual impacto.
/// Considera: versão anterior disponível, migrations reversíveis, consumers migrados,
/// dependências do rollback e readiness geral.
/// </summary>
public sealed class RollbackAssessment : AuditableEntity<RollbackAssessmentId>
{
    /// <summary>Release avaliada para rollback.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Indica se rollback é tecnicamente viável.</summary>
    public bool IsViable { get; private set; }

    /// <summary>Score de readiness para rollback (0.0 a 1.0).</summary>
    public decimal ReadinessScore { get; private set; }

    /// <summary>Versão anterior disponível para rollback.</summary>
    public string? PreviousVersion { get; private set; }

    /// <summary>Indica se há migrations que podem ser revertidas.</summary>
    public bool HasReversibleMigrations { get; private set; }

    /// <summary>Número de consumers que já migraram para a nova versão.</summary>
    public int ConsumersAlreadyMigrated { get; private set; }

    /// <summary>Total de consumers impactados pelo rollback.</summary>
    public int TotalConsumersImpacted { get; private set; }

    /// <summary>Motivo de inviabilidade do rollback, quando aplicável.</summary>
    public string? InviabilityReason { get; private set; }

    /// <summary>Recomendação de ação (rollback, fix-forward, wait, etc.).</summary>
    public string Recommendation { get; private set; } = string.Empty;

    /// <summary>Momento da avaliação.</summary>
    public DateTimeOffset AssessedAt { get; private set; }

    private RollbackAssessment() { }

    /// <summary>
    /// Cria uma avaliação de rollback para a release especificada.
    /// </summary>
    public static RollbackAssessment Create(
        ReleaseId releaseId,
        bool isViable,
        decimal readinessScore,
        string? previousVersion,
        bool hasReversibleMigrations,
        int consumersAlreadyMigrated,
        int totalConsumersImpacted,
        string? inviabilityReason,
        string recommendation,
        DateTimeOffset assessedAt)
    {
        Guard.Against.Null(releaseId, nameof(releaseId));
        Guard.Against.NullOrWhiteSpace(recommendation, nameof(recommendation));

        return new RollbackAssessment
        {
            Id = RollbackAssessmentId.New(),
            ReleaseId = releaseId,
            IsViable = isViable,
            ReadinessScore = Math.Clamp(readinessScore, 0m, 1m),
            PreviousVersion = previousVersion,
            HasReversibleMigrations = hasReversibleMigrations,
            ConsumersAlreadyMigrated = consumersAlreadyMigrated,
            TotalConsumersImpacted = totalConsumersImpacted,
            InviabilityReason = inviabilityReason,
            Recommendation = recommendation,
            AssessedAt = assessedAt
        };
    }
}

/// <summary>Identificador fortemente tipado para RollbackAssessment.</summary>
public sealed record RollbackAssessmentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static RollbackAssessmentId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static RollbackAssessmentId From(Guid id) => new(id);
}
