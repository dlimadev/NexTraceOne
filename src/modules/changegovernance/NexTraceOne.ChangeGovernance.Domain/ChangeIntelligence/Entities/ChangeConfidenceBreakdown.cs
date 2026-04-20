using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Agregado que representa o breakdown detalhado do Change Confidence Score 2.0 para uma release.
/// Composto por sub-scores auditáveis com citações de fontes, pesos configuráveis e notas de simulação.
/// Este agregado é paralelo e independente do <see cref="ChangeIntelligenceScore"/> legado.
/// </summary>
public sealed class ChangeConfidenceBreakdown : AuditableEntity<ChangeConfidenceBreakdownId>
{
    private readonly List<ChangeConfidenceSubScore> _subScores = [];

    private ChangeConfidenceBreakdown() { }

    /// <summary>Identificador da release avaliada.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Score agregado ponderado (0–100) calculado a partir dos sub-scores.</summary>
    public decimal AggregatedScore { get; private set; }

    /// <summary>Momento em que o breakdown foi computado (UTC).</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>Versão do algoritmo de score utilizado (ex.: "2.0").</summary>
    public string ScoreVersion { get; private set; } = "2.0";

    /// <summary>Sub-scores individuais que compõem o breakdown.</summary>
    public IReadOnlyCollection<ChangeConfidenceSubScore> SubScores => _subScores.AsReadOnly();

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo breakdown de confiança, calculando o score agregado como média ponderada dos sub-scores.
    /// </summary>
    public static ChangeConfidenceBreakdown Create(
        ReleaseId releaseId,
        IEnumerable<ChangeConfidenceSubScore> subScores,
        DateTimeOffset computedAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.Null(subScores);

        var subScoreList = subScores.ToList();
        Guard.Against.NullOrEmpty(subScoreList);

        var totalWeight = subScoreList.Sum(s => s.Weight);
        var aggregatedScore = totalWeight > 0m
            ? Math.Round(subScoreList.Sum(s => s.Weight * s.Value) / totalWeight, 2)
            : 0m;

        var breakdown = new ChangeConfidenceBreakdown
        {
            Id = ChangeConfidenceBreakdownId.New(),
            ReleaseId = releaseId,
            AggregatedScore = aggregatedScore,
            ComputedAt = computedAt,
            ScoreVersion = "2.0"
        };

        breakdown._subScores.AddRange(subScoreList);
        return breakdown;
    }
}

/// <summary>Identificador fortemente tipado de ChangeConfidenceBreakdown.</summary>
public sealed record ChangeConfidenceBreakdownId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ChangeConfidenceBreakdownId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ChangeConfidenceBreakdownId From(Guid id) => new(id);
}

/// <summary>
/// Objeto de valor que representa um sub-score individual no breakdown do Change Confidence Score 2.0.
/// Armazenado como tipo pertencente (owned type) na tabela chg_confidence_sub_scores.
/// </summary>
public sealed class ChangeConfidenceSubScore
{
    private ChangeConfidenceSubScore() { }

    /// <summary>Tipo de dimensão avaliada por este sub-score.</summary>
    public ConfidenceSubScoreType SubScoreType { get; private set; }

    /// <summary>Valor do sub-score (0–100).</summary>
    public decimal Value { get; private set; }

    /// <summary>Peso proporcional deste sub-score na média ponderada.</summary>
    public decimal Weight { get; private set; }

    /// <summary>Qualidade dos dados utilizados no cálculo deste sub-score.</summary>
    public ConfidenceDataQuality Confidence { get; private set; }

    /// <summary>Justificativa legível explicando o valor calculado.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Lista de URIs internas resolvíveis que citam as fontes de dados utilizadas.</summary>
    public IReadOnlyList<string> Citations { get; private set; } = [];

    /// <summary>
    /// Nota de simulação preenchida quando dados reais não estão disponíveis.
    /// Nulo quando dados reais foram utilizados.
    /// </summary>
    public string? SimulatedNote { get; private set; }

    /// <summary>
    /// Cria um novo sub-score com os valores fornecidos.
    /// </summary>
    public static ChangeConfidenceSubScore Create(
        ConfidenceSubScoreType subScoreType,
        decimal value,
        decimal weight,
        ConfidenceDataQuality confidence,
        string reason,
        IReadOnlyList<string> citations,
        string? simulatedNote = null)
    {
        Guard.Against.OutOfRange(value, nameof(value), 0m, 100m);
        Guard.Against.NegativeOrZero(weight, nameof(weight));
        Guard.Against.NullOrWhiteSpace(reason);
        Guard.Against.Null(citations);

        return new ChangeConfidenceSubScore
        {
            SubScoreType = subScoreType,
            Value = value,
            Weight = weight,
            Confidence = confidence,
            Reason = reason,
            Citations = citations,
            SimulatedNote = simulatedNote
        };
    }
}
