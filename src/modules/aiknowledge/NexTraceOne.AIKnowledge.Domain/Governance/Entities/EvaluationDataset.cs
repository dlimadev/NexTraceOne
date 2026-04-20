using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Dataset de avaliação reutilizável — fonte de casos partilhada entre suites.
/// Pode ser curado manualmente, gerado a partir de trajectórias reais ou sintético.
/// </summary>
public sealed class EvaluationDataset : AuditableEntity<EvaluationDatasetId>
{
    private EvaluationDataset() { }

    /// <summary>Nome do dataset.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do conteúdo e propósito do dataset.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Caso de uso canónico coberto pelo dataset.</summary>
    public string UseCase { get; private set; } = string.Empty;

    /// <summary>Tipo de origem dos dados.</summary>
    public EvaluationDatasetSourceType SourceType { get; private set; }

    /// <summary>Número de casos incluídos.</summary>
    public int CaseCount { get; private set; }

    /// <summary>Tenant proprietário deste dataset.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Cria um novo dataset de avaliação.</summary>
    public static EvaluationDataset Create(
        string name,
        string description,
        string useCase,
        EvaluationDatasetSourceType sourceType,
        Guid tenantId) => new()
    {
        Id = EvaluationDatasetId.New(),
        Name = Guard.Against.NullOrWhiteSpace(name),
        Description = description ?? string.Empty,
        UseCase = Guard.Against.NullOrWhiteSpace(useCase),
        SourceType = sourceType,
        TenantId = tenantId,
        CaseCount = 0
    };
}

/// <summary>Identificador fortemente tipado de EvaluationDataset.</summary>
public sealed record EvaluationDatasetId(Guid Value) : TypedIdBase(Value)
{
    public static EvaluationDatasetId New() => new(Guid.NewGuid());
    public static EvaluationDatasetId From(Guid id) => new(id);
}
