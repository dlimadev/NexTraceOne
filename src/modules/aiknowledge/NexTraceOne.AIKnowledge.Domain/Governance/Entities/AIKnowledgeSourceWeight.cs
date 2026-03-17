using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Define o peso configurável de uma fonte de conhecimento para um caso de uso específico.
/// Permite controlar quais fontes são priorizadas na composição de contexto
/// para cada tipo de consulta de IA.
///
/// Invariantes:
/// - Peso deve estar entre 0 e 100 (percentual de relevância).
/// - Cada combinação sourceType + useCaseType deve ser única por convenção.
/// - Fonte pode ter nível de confiança configurado.
/// </summary>
public sealed class AIKnowledgeSourceWeight : AuditableEntity<AIKnowledgeSourceWeightId>
{
    private AIKnowledgeSourceWeight() { }

    /// <summary>Tipo da fonte de conhecimento ponderada.</summary>
    public KnowledgeSourceType SourceType { get; private set; }

    /// <summary>Caso de uso ao qual este peso se aplica.</summary>
    public AIUseCaseType UseCaseType { get; private set; }

    /// <summary>Relevância da fonte para o caso de uso.</summary>
    public AISourceRelevance Relevance { get; private set; }

    /// <summary>Peso numérico (0-100) — maior valor indica maior prioridade na composição.</summary>
    public int Weight { get; private set; }

    /// <summary>Nível de confiança da fonte (1-5).</summary>
    public int TrustLevel { get; private set; }

    /// <summary>Indica se esta configuração de peso está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC da configuração.</summary>
    public DateTimeOffset ConfiguredAt { get; private set; }

    /// <summary>
    /// Configura o peso de uma fonte de conhecimento para um caso de uso.
    /// </summary>
    public static AIKnowledgeSourceWeight Configure(
        KnowledgeSourceType sourceType,
        AIUseCaseType useCaseType,
        AISourceRelevance relevance,
        int weight,
        int trustLevel,
        DateTimeOffset configuredAt)
    {
        Guard.Against.OutOfRange(weight, nameof(weight), 0, 100);
        Guard.Against.OutOfRange(trustLevel, nameof(trustLevel), 1, 5);

        return new AIKnowledgeSourceWeight
        {
            Id = AIKnowledgeSourceWeightId.New(),
            SourceType = sourceType,
            UseCaseType = useCaseType,
            Relevance = relevance,
            Weight = weight,
            TrustLevel = trustLevel,
            IsActive = true,
            ConfiguredAt = configuredAt
        };
    }

    /// <summary>Atualiza peso e relevância da fonte para o caso de uso.</summary>
    public Result<Unit> UpdateWeight(AISourceRelevance relevance, int weight, int trustLevel)
    {
        Guard.Against.OutOfRange(weight, nameof(weight), 0, 100);
        Guard.Against.OutOfRange(trustLevel, nameof(trustLevel), 1, 5);

        Relevance = relevance;
        Weight = weight;
        TrustLevel = trustLevel;
        return Unit.Value;
    }

    /// <summary>Ativa a configuração de peso. Operação idempotente.</summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>Desativa a configuração de peso. Operação idempotente.</summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AIKnowledgeSourceWeight.</summary>
public sealed record AIKnowledgeSourceWeightId(Guid Value) : TypedIdBase(Value)
{
    public static AIKnowledgeSourceWeightId New() => new(Guid.NewGuid());
    public static AIKnowledgeSourceWeightId From(Guid id) => new(id);
}
