using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade que representa o score de risco computado para uma release,
/// combinando pesos de breaking change, blast radius e ambiente alvo.
/// </summary>
public sealed class ChangeIntelligenceScore : AuditableEntity<ChangeIntelligenceScoreId>
{
    private ChangeIntelligenceScore() { }

    /// <summary>Identificador da release avaliada.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Score de risco normalizado entre 0.0 e 1.0.</summary>
    public decimal Score { get; private set; }

    /// <summary>Peso atribuído à presença de breaking changes (0.0–1.0).</summary>
    public decimal BreakingChangeWeight { get; private set; }

    /// <summary>Peso atribuído ao tamanho do blast radius (0.0–1.0).</summary>
    public decimal BlastRadiusWeight { get; private set; }

    /// <summary>Peso atribuído ao ambiente de destino do deployment (0.0–1.0).</summary>
    public decimal EnvironmentWeight { get; private set; }

    /// <summary>Momento em que o score foi computado.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    private const int ScorePrecision = 4;
    private const decimal NumberOfFactors = 3m;

    /// <summary>
    /// Computa e cria o score de risco como média ponderada dos três fatores.
    /// </summary>
    public static ChangeIntelligenceScore Compute(
        ReleaseId releaseId,
        decimal breakingChangeWeight,
        decimal blastRadiusWeight,
        decimal environmentWeight,
        DateTimeOffset computedAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.OutOfRange(breakingChangeWeight, nameof(breakingChangeWeight), 0m, 1m);
        Guard.Against.OutOfRange(blastRadiusWeight, nameof(blastRadiusWeight), 0m, 1m);
        Guard.Against.OutOfRange(environmentWeight, nameof(environmentWeight), 0m, 1m);

        var score = Math.Round((breakingChangeWeight + blastRadiusWeight + environmentWeight) / NumberOfFactors, ScorePrecision);

        return new ChangeIntelligenceScore
        {
            Id = ChangeIntelligenceScoreId.New(),
            ReleaseId = releaseId,
            BreakingChangeWeight = breakingChangeWeight,
            BlastRadiusWeight = blastRadiusWeight,
            EnvironmentWeight = environmentWeight,
            Score = score,
            ComputedAt = computedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ChangeIntelligenceScore.</summary>
public sealed record ChangeIntelligenceScoreId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ChangeIntelligenceScoreId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ChangeIntelligenceScoreId From(Guid id) => new(id);
}
