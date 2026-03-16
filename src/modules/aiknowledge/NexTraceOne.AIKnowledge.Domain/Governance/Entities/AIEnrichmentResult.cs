using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Domain.Entities;

/// <summary>
/// Captura o resultado de uma execução do pipeline de enriquecimento de contexto.
/// Regista quais fontes foram consultadas, o contexto agregado e metadados
/// de qualidade e confiança do bundle de conhecimento produzido.
///
/// Invariantes:
/// - CorrelationId é obrigatório.
/// - Entidade imutável após criação.
/// </summary>
public sealed class AIEnrichmentResult : AuditableEntity<AIEnrichmentResultId>
{
    private AIEnrichmentResult() { }

    /// <summary>Identificador de correlação com a execução de IA.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Query original utilizada para enriquecimento.</summary>
    public string InputQuery { get; private set; } = string.Empty;

    /// <summary>Persona do utilizador.</summary>
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Caso de uso classificado.</summary>
    public AIUseCaseType UseCaseType { get; private set; }

    /// <summary>Fontes consultadas, separadas por vírgula.</summary>
    public string QueriedSources { get; private set; } = string.Empty;

    /// <summary>Fontes que retornaram dados válidos, separadas por vírgula.</summary>
    public string ResolvedSources { get; private set; } = string.Empty;

    /// <summary>Número total de itens de contexto agregados.</summary>
    public int TotalContextItems { get; private set; }

    /// <summary>Nível de confiança do bundle de conhecimento produzido.</summary>
    public AIConfidenceLevel ConfidenceLevel { get; private set; }

    /// <summary>Resumo do contexto agregado para inspeção.</summary>
    public string ContextSummary { get; private set; } = string.Empty;

    /// <summary>Tempo de processamento do enrichment em milissegundos.</summary>
    public int ProcessingTimeMs { get; private set; }

    /// <summary>Data/hora UTC da execução.</summary>
    public DateTimeOffset EnrichedAt { get; private set; }

    /// <summary>
    /// Regista o resultado de uma execução do pipeline de enriquecimento.
    /// </summary>
    public static AIEnrichmentResult Record(
        string correlationId,
        string inputQuery,
        string persona,
        AIUseCaseType useCaseType,
        string queriedSources,
        string resolvedSources,
        int totalContextItems,
        AIConfidenceLevel confidenceLevel,
        string contextSummary,
        int processingTimeMs,
        DateTimeOffset enrichedAt)
    {
        Guard.Against.NullOrWhiteSpace(correlationId);
        Guard.Against.NullOrWhiteSpace(inputQuery);
        Guard.Against.NullOrWhiteSpace(persona);
        Guard.Against.Negative(totalContextItems);
        Guard.Against.Negative(processingTimeMs);

        return new AIEnrichmentResult
        {
            Id = AIEnrichmentResultId.New(),
            CorrelationId = correlationId,
            InputQuery = inputQuery,
            Persona = persona,
            UseCaseType = useCaseType,
            QueriedSources = queriedSources ?? string.Empty,
            ResolvedSources = resolvedSources ?? string.Empty,
            TotalContextItems = totalContextItems,
            ConfidenceLevel = confidenceLevel,
            ContextSummary = contextSummary ?? string.Empty,
            ProcessingTimeMs = processingTimeMs,
            EnrichedAt = enrichedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de AIEnrichmentResult.</summary>
public sealed record AIEnrichmentResultId(Guid Value) : TypedIdBase(Value)
{
    public static AIEnrichmentResultId New() => new(Guid.NewGuid());
    public static AIEnrichmentResultId From(Guid id) => new(id);
}
