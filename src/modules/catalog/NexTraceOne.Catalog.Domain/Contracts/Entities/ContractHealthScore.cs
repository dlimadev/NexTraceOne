using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa o score de saúde contínuo de um contrato (API Asset).
/// Combina 6 dimensões de qualidade para produzir um score consolidado de 0 a 100.
/// Recalculado periodicamente ou por evento, serve como base para alertas,
/// badges no catálogo e regras de promotion gates.
/// </summary>
public sealed class ContractHealthScore : AuditableEntity<ContractHealthScoreId>
{
    private ContractHealthScore() { }

    /// <summary>Identificador do API Asset (contrato) avaliado.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Score consolidado de 0 a 100.</summary>
    public int OverallScore { get; private set; }

    /// <summary>Score da dimensão: frequência de breaking changes (0–100). Menor = mais breaking changes.</summary>
    public int BreakingChangeFrequencyScore { get; private set; }

    /// <summary>Score da dimensão: impacto nos consumidores (0–100). Menor = mais consumidores afetados por breaking changes.</summary>
    public int ConsumerImpactScore { get; private set; }

    /// <summary>Score da dimensão: recência da última revisão (0–100). Menor = revisão desatualizada.</summary>
    public int ReviewRecencyScore { get; private set; }

    /// <summary>Score da dimensão: cobertura de exemplos (0–100).</summary>
    public int ExampleCoverageScore { get; private set; }

    /// <summary>Score da dimensão: conformidade com políticas de linting (0–100).</summary>
    public int PolicyComplianceScore { get; private set; }

    /// <summary>Score da dimensão: presença e qualidade da documentação (0–100).</summary>
    public int DocumentationScore { get; private set; }

    /// <summary>Momento em que o score foi calculado.</summary>
    public DateTimeOffset CalculatedAt { get; private set; }

    /// <summary>Indica se o score está abaixo do threshold configurado.</summary>
    public bool IsDegraded { get; private set; }

    /// <summary>Threshold configurado para o momento do cálculo.</summary>
    public int DegradationThreshold { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo score de saúde para um contrato, calculando o score consolidado
    /// como média ponderada das 6 dimensões.
    /// </summary>
    public static ContractHealthScore Create(
        Guid apiAssetId,
        int breakingChangeFrequencyScore,
        int consumerImpactScore,
        int reviewRecencyScore,
        int exampleCoverageScore,
        int policyComplianceScore,
        int documentationScore,
        int degradationThreshold,
        DateTimeOffset calculatedAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.OutOfRange(breakingChangeFrequencyScore, nameof(breakingChangeFrequencyScore), 0, 100);
        Guard.Against.OutOfRange(consumerImpactScore, nameof(consumerImpactScore), 0, 100);
        Guard.Against.OutOfRange(reviewRecencyScore, nameof(reviewRecencyScore), 0, 100);
        Guard.Against.OutOfRange(exampleCoverageScore, nameof(exampleCoverageScore), 0, 100);
        Guard.Against.OutOfRange(policyComplianceScore, nameof(policyComplianceScore), 0, 100);
        Guard.Against.OutOfRange(documentationScore, nameof(documentationScore), 0, 100);
        Guard.Against.OutOfRange(degradationThreshold, nameof(degradationThreshold), 0, 100);

        var overall = ComputeOverall(
            breakingChangeFrequencyScore, consumerImpactScore, reviewRecencyScore,
            exampleCoverageScore, policyComplianceScore, documentationScore);

        return new ContractHealthScore
        {
            Id = ContractHealthScoreId.New(),
            ApiAssetId = apiAssetId,
            OverallScore = overall,
            BreakingChangeFrequencyScore = breakingChangeFrequencyScore,
            ConsumerImpactScore = consumerImpactScore,
            ReviewRecencyScore = reviewRecencyScore,
            ExampleCoverageScore = exampleCoverageScore,
            PolicyComplianceScore = policyComplianceScore,
            DocumentationScore = documentationScore,
            CalculatedAt = calculatedAt,
            DegradationThreshold = degradationThreshold,
            IsDegraded = overall < degradationThreshold
        };
    }

    /// <summary>
    /// Recalcula o score consolidado e atualiza o estado de degradação.
    /// </summary>
    public void Recalculate(
        int breakingChangeFrequencyScore,
        int consumerImpactScore,
        int reviewRecencyScore,
        int exampleCoverageScore,
        int policyComplianceScore,
        int documentationScore,
        int degradationThreshold,
        DateTimeOffset calculatedAt)
    {
        Guard.Against.OutOfRange(breakingChangeFrequencyScore, nameof(breakingChangeFrequencyScore), 0, 100);
        Guard.Against.OutOfRange(consumerImpactScore, nameof(consumerImpactScore), 0, 100);
        Guard.Against.OutOfRange(reviewRecencyScore, nameof(reviewRecencyScore), 0, 100);
        Guard.Against.OutOfRange(exampleCoverageScore, nameof(exampleCoverageScore), 0, 100);
        Guard.Against.OutOfRange(policyComplianceScore, nameof(policyComplianceScore), 0, 100);
        Guard.Against.OutOfRange(documentationScore, nameof(documentationScore), 0, 100);
        Guard.Against.OutOfRange(degradationThreshold, nameof(degradationThreshold), 0, 100);

        BreakingChangeFrequencyScore = breakingChangeFrequencyScore;
        ConsumerImpactScore = consumerImpactScore;
        ReviewRecencyScore = reviewRecencyScore;
        ExampleCoverageScore = exampleCoverageScore;
        PolicyComplianceScore = policyComplianceScore;
        DocumentationScore = documentationScore;
        DegradationThreshold = degradationThreshold;

        OverallScore = ComputeOverall(
            breakingChangeFrequencyScore, consumerImpactScore, reviewRecencyScore,
            exampleCoverageScore, policyComplianceScore, documentationScore);

        IsDegraded = OverallScore < degradationThreshold;
        CalculatedAt = calculatedAt;
    }

    /// <summary>
    /// Calcula o score consolidado como média ponderada das 6 dimensões.
    /// Pesos: breaking (20%) + consumer (20%) + review (15%) + examples (15%) + policy (15%) + docs (15%).
    /// </summary>
    private static int ComputeOverall(
        int breakingChangeFrequencyScore,
        int consumerImpactScore,
        int reviewRecencyScore,
        int exampleCoverageScore,
        int policyComplianceScore,
        int documentationScore)
    {
        var overall = (int)Math.Round(
            breakingChangeFrequencyScore * 0.20 +
            consumerImpactScore * 0.20 +
            reviewRecencyScore * 0.15 +
            exampleCoverageScore * 0.15 +
            policyComplianceScore * 0.15 +
            documentationScore * 0.15);

        return Math.Clamp(overall, 0, 100);
    }
}

/// <summary>Identificador fortemente tipado de ContractHealthScore.</summary>
public sealed record ContractHealthScoreId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractHealthScoreId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractHealthScoreId From(Guid id) => new(id);
}
