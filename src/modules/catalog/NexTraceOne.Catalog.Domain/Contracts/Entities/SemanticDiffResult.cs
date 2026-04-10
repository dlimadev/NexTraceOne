using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa o resultado de uma análise semântica de diff assistida por IA
/// entre duas versões de um contrato (API Asset). Armazena o sumário em linguagem natural,
/// classificação de impacto, consumidores afetados, sugestões de mitigação e score de compatibilidade.
/// Suporta os pilares de Contract Governance e Change Intelligence do NexTraceOne.
/// </summary>
public sealed class SemanticDiffResult : AuditableEntity<SemanticDiffResultId>
{
    private SemanticDiffResult() { }

    /// <summary>Identificador da versão de origem do diff (semver ou Id textual).</summary>
    public string ContractVersionFromId { get; private set; } = string.Empty;

    /// <summary>Identificador da versão de destino do diff (semver ou Id textual).</summary>
    public string ContractVersionToId { get; private set; } = string.Empty;

    /// <summary>Sumário em linguagem natural gerado pela IA descrevendo as diferenças semânticas.</summary>
    public string NaturalLanguageSummary { get; private set; } = string.Empty;

    /// <summary>Classificação do impacto do diff: Breaking, NonBreaking ou Enhancement.</summary>
    public SemanticDiffClassification Classification { get; private set; }

    /// <summary>Lista de consumidores afetados pela mudança (JSONB).</summary>
    public string? AffectedConsumers { get; private set; }

    /// <summary>Sugestões de mitigação geradas pela IA (JSONB).</summary>
    public string? MitigationSuggestions { get; private set; }

    /// <summary>Score de compatibilidade de 0 a 100 gerado pela IA.</summary>
    public int CompatibilityScore { get; private set; }

    /// <summary>Nome ou identificador do modelo de IA que gerou o resultado.</summary>
    public string GeneratedByModel { get; private set; } = string.Empty;

    /// <summary>Momento em que o resultado foi gerado pela IA.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Identificador do tenant (multi-tenancy).</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo resultado de diff semântico assistido por IA entre duas versões de contrato,
    /// validando todos os parâmetros obrigatórios e limites de negócio.
    /// </summary>
    public static SemanticDiffResult Generate(
        string contractVersionFromId,
        string contractVersionToId,
        string naturalLanguageSummary,
        SemanticDiffClassification classification,
        string? affectedConsumers,
        string? mitigationSuggestions,
        int compatibilityScore,
        string generatedByModel,
        DateTimeOffset generatedAt,
        string? tenantId = null)
    {
        Guard.Against.NullOrWhiteSpace(contractVersionFromId);
        Guard.Against.StringTooLong(contractVersionFromId, 200);
        Guard.Against.NullOrWhiteSpace(contractVersionToId);
        Guard.Against.StringTooLong(contractVersionToId, 200);
        Guard.Against.NullOrWhiteSpace(naturalLanguageSummary);
        Guard.Against.StringTooLong(naturalLanguageSummary, 8000);
        Guard.Against.EnumOutOfRange(classification);
        Guard.Against.OutOfRange(compatibilityScore, nameof(compatibilityScore), 0, 100);
        Guard.Against.NullOrWhiteSpace(generatedByModel);
        Guard.Against.StringTooLong(generatedByModel, 200);

        return new SemanticDiffResult
        {
            Id = SemanticDiffResultId.New(),
            ContractVersionFromId = contractVersionFromId.Trim(),
            ContractVersionToId = contractVersionToId.Trim(),
            NaturalLanguageSummary = naturalLanguageSummary.Trim(),
            Classification = classification,
            AffectedConsumers = affectedConsumers,
            MitigationSuggestions = mitigationSuggestions,
            CompatibilityScore = compatibilityScore,
            GeneratedByModel = generatedByModel.Trim(),
            GeneratedAt = generatedAt,
            TenantId = tenantId?.Trim()
        };
    }
}

/// <summary>Identificador fortemente tipado de SemanticDiffResult.</summary>
public sealed record SemanticDiffResultId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SemanticDiffResultId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SemanticDiffResultId From(Guid id) => new(id);
}
