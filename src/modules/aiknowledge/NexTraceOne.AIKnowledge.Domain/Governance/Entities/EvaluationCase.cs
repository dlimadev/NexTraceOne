using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Caso de teste individual dentro de uma suite de avaliação.
/// Contém prompt de entrada, contexto de grounding e critérios de avaliação.
/// </summary>
public sealed class EvaluationCase : AuditableEntity<EvaluationCaseId>
{
    private EvaluationCase() { }

    /// <summary>Suite à qual este caso pertence.</summary>
    public EvaluationSuiteId SuiteId { get; private set; } = null!;

    /// <summary>Nome descritivo do caso.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Prompt de entrada enviado ao modelo.</summary>
    public string InputPrompt { get; private set; } = string.Empty;

    /// <summary>Contexto de grounding serializado como JSON.</summary>
    public string GroundingContext { get; private set; } = string.Empty;

    /// <summary>Padrão esperado no output (regex ou string exacta).</summary>
    public string ExpectedOutputPattern { get; private set; } = string.Empty;

    /// <summary>Critérios de avaliação separados por vírgula (ex: "ExactMatch,JsonSchemaValidity").</summary>
    public string EvaluationCriteria { get; private set; } = string.Empty;

    /// <summary>Indica se o caso está activo para execução.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Cria um novo caso de avaliação.</summary>
    public static EvaluationCase Create(
        EvaluationSuiteId suiteId,
        string name,
        string inputPrompt,
        string groundingContext,
        string expectedOutputPattern,
        string evaluationCriteria) => new()
    {
        Id = EvaluationCaseId.New(),
        SuiteId = Guard.Against.Null(suiteId),
        Name = Guard.Against.NullOrWhiteSpace(name),
        InputPrompt = Guard.Against.NullOrWhiteSpace(inputPrompt),
        GroundingContext = groundingContext ?? string.Empty,
        ExpectedOutputPattern = expectedOutputPattern ?? string.Empty,
        EvaluationCriteria = evaluationCriteria ?? string.Empty,
        IsActive = true
    };
}

/// <summary>Identificador fortemente tipado de EvaluationCase.</summary>
public sealed record EvaluationCaseId(Guid Value) : TypedIdBase(Value)
{
    public static EvaluationCaseId New() => new(Guid.NewGuid());
    public static EvaluationCaseId From(Guid id) => new(id);
}
