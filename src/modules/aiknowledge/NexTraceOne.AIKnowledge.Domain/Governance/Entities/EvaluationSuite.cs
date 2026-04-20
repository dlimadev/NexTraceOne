using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Suite de avaliação de IA — coleção nomeada e versionada de casos de teste para um caso de uso específico.
/// Exemplos: "contract-review-suite-v1", "incident-summary-suite-v1".
/// </summary>
public sealed class EvaluationSuite : AuditableEntity<EvaluationSuiteId>
{
    private EvaluationSuite() { }

    /// <summary>Nome técnico da suite (ex: "contract-review-suite-v1").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação da suite na UI.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do objetivo da suite.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Caso de uso canónico avaliado (ex: "contract-review", "incident-summary").</summary>
    public string UseCase { get; private set; } = string.Empty;

    /// <summary>Modelo alvo da avaliação. Null = modelo default do tenant.</summary>
    public Guid? TargetModelId { get; private set; }

    /// <summary>Estado do ciclo de vida da suite.</summary>
    public EvaluationSuiteStatus Status { get; private set; }

    /// <summary>Versão semântica da suite.</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Tenant proprietário desta suite.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Cria uma nova suite de avaliação no estado Draft.</summary>
    public static EvaluationSuite Create(
        string name,
        string displayName,
        string description,
        string useCase,
        string version,
        Guid tenantId,
        Guid? targetModelId = null) => new()
    {
        Id = EvaluationSuiteId.New(),
        Name = Guard.Against.NullOrWhiteSpace(name),
        DisplayName = Guard.Against.NullOrWhiteSpace(displayName),
        Description = description ?? string.Empty,
        UseCase = Guard.Against.NullOrWhiteSpace(useCase),
        Version = Guard.Against.NullOrWhiteSpace(version),
        TenantId = tenantId,
        TargetModelId = targetModelId,
        Status = EvaluationSuiteStatus.Draft
    };

    /// <summary>Activa a suite para execução.</summary>
    public void Activate() => Status = EvaluationSuiteStatus.Active;

    /// <summary>Arquiva a suite — histórico preservado, sem novas execuções.</summary>
    public void Archive() => Status = EvaluationSuiteStatus.Archived;
}

/// <summary>Identificador fortemente tipado de EvaluationSuite.</summary>
public sealed record EvaluationSuiteId(Guid Value) : TypedIdBase(Value)
{
    public static EvaluationSuiteId New() => new(Guid.NewGuid());
    public static EvaluationSuiteId From(Guid id) => new(id);
}
