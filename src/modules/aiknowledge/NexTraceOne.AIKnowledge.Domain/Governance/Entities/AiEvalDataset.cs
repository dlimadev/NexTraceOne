using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Identificador fortemente tipado para AiEvalDataset.
/// </summary>
public sealed record AiEvalDatasetId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Dataset de avaliação de modelos IA — conjunto de pares (input, expected_output)
/// para um caso de uso específico de agente ou assistente.
///
/// Utilizado pelo RunAiEvaluation command para executar avaliações comparativas
/// de modelos sobre casos de teste estruturados.
///
/// Referência: CC-05, ADR-009.
/// Owner: módulo AIKnowledge (Governance).
/// </summary>
public sealed class AiEvalDataset : Entity<AiEvalDatasetId>
{
    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome do dataset (ex: "contract-change-confidence-v1").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Caso de uso ao qual o dataset pertence (ex: "change-confidence", "incident-summary").</summary>
    public string UseCase { get; private set; } = string.Empty;

    /// <summary>Descrição do dataset e propósito da avaliação.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Casos de teste em JSON — array de {id, input, expectedOutput, tags}.
    /// Permite avaliar exact match, semântica e tool call accuracy.
    /// </summary>
    public string TestCasesJson { get; private set; } = "[]";

    /// <summary>Número de casos de teste no dataset.</summary>
    public int TestCaseCount { get; private set; }

    /// <summary>Indica se o dataset está activo (pode ser usado em eval runs).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private AiEvalDataset() { }

    /// <summary>Cria um novo dataset de avaliação.</summary>
    public static AiEvalDataset Create(
        string tenantId,
        string name,
        string useCase,
        string? description,
        string testCasesJson,
        int testCaseCount,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 200, nameof(name));
        Guard.Against.NullOrWhiteSpace(useCase, nameof(useCase));
        Guard.Against.NullOrWhiteSpace(testCasesJson, nameof(testCasesJson));
        Guard.Against.Negative(testCaseCount, nameof(testCaseCount));

        return new AiEvalDataset
        {
            Id = new AiEvalDatasetId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name.Trim(),
            UseCase = useCase.Trim(),
            Description = description?.Trim(),
            TestCasesJson = testCasesJson,
            TestCaseCount = testCaseCount,
            IsActive = true,
            CreatedAt = utcNow
        };
    }

    /// <summary>Actualiza o dataset com novos casos de teste.</summary>
    public void UpdateTestCases(string testCasesJson, int testCaseCount, DateTimeOffset utcNow)
    {
        TestCasesJson = testCasesJson;
        TestCaseCount = testCaseCount;
        UpdatedAt = utcNow;
    }

    /// <summary>Desactiva o dataset.</summary>
    public void Deactivate(DateTimeOffset utcNow) { IsActive = false; UpdatedAt = utcNow; }
}
