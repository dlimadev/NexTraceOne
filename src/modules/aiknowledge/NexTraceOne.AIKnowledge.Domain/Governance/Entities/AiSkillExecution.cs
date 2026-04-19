using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Registo de uma execução de skill de IA.
/// Captura input/output, tokens, duração, modelo usado e resultado.
/// Imutável após criação — serve como log auditável de cada invocação.
/// </summary>
public sealed class AiSkillExecution : AuditableEntity<AiSkillExecutionId>
{
    private AiSkillExecution() { }

    /// <summary>Skill que foi executada.</summary>
    public AiSkillId SkillId { get; private set; } = null!;

    /// <summary>Agent que originou a execução (opcional).</summary>
    public AiAgentId? AgentId { get; private set; }

    /// <summary>Utilizador que disparou a execução.</summary>
    public string ExecutedBy { get; private set; } = string.Empty;

    /// <summary>Modelo utilizado na execução.</summary>
    public string ModelUsed { get; private set; } = string.Empty;

    /// <summary>Input fornecido à skill (JSON).</summary>
    public string InputJson { get; private set; } = string.Empty;

    /// <summary>Output produzido pela skill (JSON).</summary>
    public string OutputJson { get; private set; } = string.Empty;

    /// <summary>Duração da execução em milissegundos.</summary>
    public long DurationMs { get; private set; }

    /// <summary>Tokens de prompt consumidos.</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Tokens de completion consumidos.</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Total de tokens consumidos.</summary>
    public int TotalTokens { get; private set; }

    /// <summary>Indica se a execução foi bem-sucedida.</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>Mensagem de erro quando a execução falhou.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Tenant no qual a execução ocorreu.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Timestamp da execução.</summary>
    public DateTimeOffset ExecutedAt { get; private set; }

    /// <summary>Regista uma execução de skill.</summary>
    public static AiSkillExecution Log(
        AiSkillId skillId,
        string executedBy,
        string modelUsed,
        string inputJson,
        string outputJson,
        long durationMs,
        int promptTokens,
        int completionTokens,
        bool isSuccess,
        string? errorMessage,
        Guid tenantId,
        DateTimeOffset executedAt,
        AiAgentId? agentId = null)
    {
        Guard.Against.Null(skillId);
        Guard.Against.NullOrWhiteSpace(executedBy);

        return new AiSkillExecution
        {
            Id = AiSkillExecutionId.New(),
            SkillId = skillId,
            AgentId = agentId,
            ExecutedBy = executedBy,
            ModelUsed = modelUsed ?? string.Empty,
            InputJson = inputJson ?? string.Empty,
            OutputJson = outputJson ?? string.Empty,
            DurationMs = durationMs,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = promptTokens + completionTokens,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            TenantId = tenantId,
            ExecutedAt = executedAt,
        };
    }
}

/// <summary>Identificador fortemente tipado de AiSkillExecution.</summary>
public sealed record AiSkillExecutionId(Guid Value) : TypedIdBase(Value)
{
    public static AiSkillExecutionId New() => new(Guid.NewGuid());
    public static AiSkillExecutionId From(Guid id) => new(id);
}
