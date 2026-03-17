using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa o scorecard técnico de uma versão de contrato.
/// Avalia qualidade, completude, compatibilidade e risco técnico do contrato
/// para alimentar decisões de workflow, comunicação de mudança e IA.
/// Cada dimensão é pontuada de 0.0 a 1.0 com justificativas individuais.
/// </summary>
public sealed class ContractScorecard : Entity<ContractScorecardId>
{
    private ContractScorecard() { }

    /// <summary>Identificador da versão de contrato avaliada.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = ContractVersionId.New();

    /// <summary>Protocolo do contrato avaliado.</summary>
    public ContractProtocol Protocol { get; private set; }

    /// <summary>Score de qualidade geral do contrato (0.0 a 1.0).</summary>
    public decimal QualityScore { get; private set; }

    /// <summary>Score de completude — cobertura de descrições, exemplos, tipos, segurança (0.0 a 1.0).</summary>
    public decimal CompletenessScore { get; private set; }

    /// <summary>Score de compatibilidade com consumers existentes (0.0 a 1.0).</summary>
    public decimal CompatibilityScore { get; private set; }

    /// <summary>Score de risco técnico — inversão do score de segurança/estabilidade (0.0 a 1.0).</summary>
    public decimal RiskScore { get; private set; }

    /// <summary>Score consolidado ponderado de todas as dimensões (0.0 a 1.0).</summary>
    public decimal OverallScore { get; private set; }

    /// <summary>Número total de operações analisadas.</summary>
    public int OperationCount { get; private set; }

    /// <summary>Número total de schemas/tipos analisados.</summary>
    public int SchemaCount { get; private set; }

    /// <summary>Indica se o contrato define esquemas de segurança.</summary>
    public bool HasSecurityDefinitions { get; private set; }

    /// <summary>Indica se o contrato contém exemplos nas operações.</summary>
    public bool HasExamples { get; private set; }

    /// <summary>Indica se o contrato contém descrições nas operações.</summary>
    public bool HasDescriptions { get; private set; }

    /// <summary>Justificativa detalhada do score de qualidade.</summary>
    public string QualityJustification { get; private set; } = string.Empty;

    /// <summary>Justificativa detalhada do score de completude.</summary>
    public string CompletenessJustification { get; private set; } = string.Empty;

    /// <summary>Justificativa detalhada do score de compatibilidade.</summary>
    public string CompatibilityJustification { get; private set; } = string.Empty;

    /// <summary>Justificativa detalhada do score de risco.</summary>
    public string RiskJustification { get; private set; } = string.Empty;

    /// <summary>Data/hora em que o scorecard foi computado.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>
    /// Cria um novo scorecard técnico para uma versão de contrato.
    /// </summary>
    public static ContractScorecard Create(
        ContractVersionId contractVersionId,
        ContractProtocol protocol,
        decimal qualityScore,
        decimal completenessScore,
        decimal compatibilityScore,
        decimal riskScore,
        int operationCount,
        int schemaCount,
        bool hasSecurityDefinitions,
        bool hasExamples,
        bool hasDescriptions,
        string qualityJustification,
        string completenessJustification,
        string compatibilityJustification,
        string riskJustification,
        DateTimeOffset computedAt)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.NullOrWhiteSpace(qualityJustification);
        Guard.Against.NullOrWhiteSpace(completenessJustification);
        Guard.Against.NullOrWhiteSpace(compatibilityJustification);
        Guard.Against.NullOrWhiteSpace(riskJustification);

        var overall = Math.Clamp(
            (qualityScore * 0.30m) + (completenessScore * 0.25m) + (compatibilityScore * 0.25m) + ((1.0m - riskScore) * 0.20m),
            0m, 1m);

        return new ContractScorecard
        {
            Id = ContractScorecardId.New(),
            ContractVersionId = contractVersionId,
            Protocol = protocol,
            QualityScore = Math.Clamp(qualityScore, 0m, 1m),
            CompletenessScore = Math.Clamp(completenessScore, 0m, 1m),
            CompatibilityScore = Math.Clamp(compatibilityScore, 0m, 1m),
            RiskScore = Math.Clamp(riskScore, 0m, 1m),
            OverallScore = overall,
            OperationCount = operationCount,
            SchemaCount = schemaCount,
            HasSecurityDefinitions = hasSecurityDefinitions,
            HasExamples = hasExamples,
            HasDescriptions = hasDescriptions,
            QualityJustification = qualityJustification,
            CompletenessJustification = completenessJustification,
            CompatibilityJustification = compatibilityJustification,
            RiskJustification = riskJustification,
            ComputedAt = computedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractScorecard.</summary>
public sealed record ContractScorecardId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractScorecardId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractScorecardId From(Guid id) => new(id);
}
