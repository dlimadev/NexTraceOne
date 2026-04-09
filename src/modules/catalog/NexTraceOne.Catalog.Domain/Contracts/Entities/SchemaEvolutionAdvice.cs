using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa o resultado de uma análise de evolução de schema entre duas versões
/// de um contrato (API Asset). Produz um relatório estruturado com score de compatibilidade,
/// campos adicionados/removidos/modificados, consumidores afetados e estratégia de migração recomendada.
/// Suporta o pilar de Contract Governance e Change Intelligence do NexTraceOne.
/// </summary>
public sealed class SchemaEvolutionAdvice : AuditableEntity<SchemaEvolutionAdviceId>
{
    private SchemaEvolutionAdvice() { }

    /// <summary>Identificador do API Asset (contrato) analisado.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome do contrato para exibição.</summary>
    public string ContractName { get; private set; } = string.Empty;

    /// <summary>Versão de origem da análise.</summary>
    public string SourceVersion { get; private set; } = string.Empty;

    /// <summary>Versão de destino da análise.</summary>
    public string TargetVersion { get; private set; } = string.Empty;

    /// <summary>Nível de compatibilidade detectado entre as versões.</summary>
    public CompatibilityLevel CompatibilityLevel { get; private set; }

    /// <summary>Score de compatibilidade de 0 a 100.</summary>
    public int CompatibilityScore { get; private set; }

    /// <summary>Campos adicionados na versão de destino (JSONB).</summary>
    public string? FieldsAdded { get; private set; }

    /// <summary>Campos removidos na versão de destino (JSONB).</summary>
    public string? FieldsRemoved { get; private set; }

    /// <summary>Campos modificados entre as versões (JSONB).</summary>
    public string? FieldsModified { get; private set; }

    /// <summary>Campos em uso por consumidores que podem ser afetados (JSONB).</summary>
    public string? FieldsInUseByConsumers { get; private set; }

    /// <summary>Lista de consumidores afetados pela evolução (JSONB).</summary>
    public string? AffectedConsumers { get; private set; }

    /// <summary>Número total de consumidores afetados.</summary>
    public int AffectedConsumerCount { get; private set; }

    /// <summary>Estratégia de migração recomendada.</summary>
    public MigrationStrategy RecommendedStrategy { get; private set; }

    /// <summary>Detalhes da estratégia de migração com plano detalhado (JSONB).</summary>
    public string? StrategyDetails { get; private set; }

    /// <summary>Lista de recomendações para a evolução (JSONB).</summary>
    public string? Recommendations { get; private set; }

    /// <summary>Lista de avisos e riscos identificados (JSONB).</summary>
    public string? Warnings { get; private set; }

    /// <summary>Momento em que a análise foi realizada.</summary>
    public DateTimeOffset AnalyzedAt { get; private set; }

    /// <summary>Nome do agente de IA que realizou a análise, quando aplicável.</summary>
    public string? AnalyzedByAgentName { get; private set; }

    /// <summary>Identificador do tenant (multi-tenancy).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova análise de evolução de schema para um contrato,
    /// validando parâmetros e produzindo o relatório estruturado.
    /// </summary>
    public static SchemaEvolutionAdvice Analyze(
        Guid apiAssetId,
        string contractName,
        string sourceVersion,
        string targetVersion,
        CompatibilityLevel compatibilityLevel,
        int compatibilityScore,
        string? fieldsAdded,
        string? fieldsRemoved,
        string? fieldsModified,
        string? fieldsInUseByConsumers,
        string? affectedConsumers,
        int affectedConsumerCount,
        MigrationStrategy recommendedStrategy,
        string? strategyDetails,
        string? recommendations,
        string? warnings,
        DateTimeOffset analyzedAt,
        string? analyzedByAgentName = null,
        Guid? tenantId = null)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(contractName);
        Guard.Against.NullOrWhiteSpace(sourceVersion);
        Guard.Against.NullOrWhiteSpace(targetVersion);
        Guard.Against.OutOfRange(compatibilityScore, nameof(compatibilityScore), 0, 100);
        Guard.Against.Negative(affectedConsumerCount);

        return new SchemaEvolutionAdvice
        {
            Id = SchemaEvolutionAdviceId.New(),
            ApiAssetId = apiAssetId,
            ContractName = contractName,
            SourceVersion = sourceVersion,
            TargetVersion = targetVersion,
            CompatibilityLevel = compatibilityLevel,
            CompatibilityScore = compatibilityScore,
            FieldsAdded = fieldsAdded,
            FieldsRemoved = fieldsRemoved,
            FieldsModified = fieldsModified,
            FieldsInUseByConsumers = fieldsInUseByConsumers,
            AffectedConsumers = affectedConsumers,
            AffectedConsumerCount = affectedConsumerCount,
            RecommendedStrategy = recommendedStrategy,
            StrategyDetails = strategyDetails,
            Recommendations = recommendations,
            Warnings = warnings,
            AnalyzedAt = analyzedAt,
            AnalyzedByAgentName = analyzedByAgentName,
            TenantId = tenantId
        };
    }
}

/// <summary>Identificador fortemente tipado de SchemaEvolutionAdvice.</summary>
public sealed record SchemaEvolutionAdviceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SchemaEvolutionAdviceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SchemaEvolutionAdviceId From(Guid id) => new(id);
}
