using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Iteração individual numa sessão de reflexão agentic.
/// Cada iteração representa um ciclo Plan → Execute → Reflect.
/// </summary>
public sealed record ReflectionIteration(
    int IterationNumber,
    string Plan,
    string ExecutionOutput,
    string Reflection,
    int Score,
    ReflectionDecision Decision,
    long DurationMs);

/// <summary>
/// Decisão tomada após a fase de reflexão.
/// </summary>
public enum ReflectionDecision
{
    Continue,
    Revise,
    Complete
}

/// <summary>
/// Estado de uma sessão de reflexão agentic.
/// </summary>
public enum ReflectionSessionStatus
{
    Planning,
    Executing,
    Reflecting,
    Completed,
    Failed
}

/// <summary>
/// Sessão de execução reflexiva de um agente.
/// Agrupa múltiplas iterações de Plan → Execute → Reflect até critério de parada.
///
/// Invariantes:
/// - OriginalTask não pode ser vazio.
/// - MaxIterations entre 1 e 10.
/// - Score de cada iteração entre 0 e 100.
/// </summary>
public sealed class AgentReflectionSession : AuditableEntity<AgentReflectionSessionId>
{
    private readonly List<ReflectionIteration> _iterations = [];

    private AgentReflectionSession() { }

    /// <summary>Tarefa original que deu origem à sessão.</summary>
    public string OriginalTask { get; private set; } = string.Empty;

    /// <summary>Estado actual da sessão.</summary>
    public ReflectionSessionStatus Status { get; private set; }

    /// <summary>Iterações executadas até ao momento.</summary>
    public IReadOnlyList<ReflectionIteration> Iterations => _iterations.AsReadOnly();

    /// <summary>Número máximo de iterações permitidas.</summary>
    public int MaxIterations { get; private set; }

    /// <summary>Output final após conclusão.</summary>
    public string? FinalOutput { get; private set; }

    /// <summary>Score final (0-100).</summary>
    public int FinalScore { get; private set; }

    /// <summary>Indica se houve revisão (mais de uma iteração).</summary>
    public bool WasRevised { get; private set; }

    /// <summary>Total de tokens consumidos pela sessão.</summary>
    public int TotalTokensConsumed { get; private set; }

    /// <summary>Duração total em milissegundos.</summary>
    public long DurationMs { get; private set; }

    /// <summary>Identificador de correlação para rastreabilidade.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Momento de início da sessão.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Momento de conclusão da sessão.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Mensagem de erro (apenas para sessões com falha).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Inicia uma nova sessão de reflexão.</summary>
    public static AgentReflectionSession Start(
        string originalTask,
        int maxIterations,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalTask);
        if (maxIterations is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(maxIterations), "Must be between 1 and 10.");

        return new AgentReflectionSession
        {
            Id = AgentReflectionSessionId.New(),
            OriginalTask = originalTask,
            Status = ReflectionSessionStatus.Planning,
            MaxIterations = maxIterations,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            StartedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Regista uma iteração completa.</summary>
    public void RecordIteration(ReflectionIteration iteration)
    {
        ArgumentNullException.ThrowIfNull(iteration);
        if (iteration.Score is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(iteration), "Score must be between 0 and 100.");

        _iterations.Add(iteration);
        TotalTokensConsumed += 0; // Token tracking delegated to caller
        WasRevised = _iterations.Count > 1;

        Status = iteration.Decision switch
        {
            ReflectionDecision.Complete => ReflectionSessionStatus.Completed,
            ReflectionDecision.Revise => ReflectionSessionStatus.Executing,
            _ => ReflectionSessionStatus.Reflecting,
        };
    }

    /// <summary>Marca a sessão como concluída com output final.</summary>
    public void Complete(string finalOutput, int finalScore, long durationMs)
    {
        FinalOutput = finalOutput;
        FinalScore = finalScore is >= 0 and <= 100 ? finalScore : 0;
        DurationMs = durationMs;
        Status = ReflectionSessionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Marca a sessão como falhada.</summary>
    public void Fail(string errorMessage, long durationMs)
    {
        ErrorMessage = errorMessage;
        DurationMs = durationMs;
        Status = ReflectionSessionStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de AgentReflectionSession.</summary>
public sealed record AgentReflectionSessionId(Guid Value) : TypedIdBase(Value)
{
    public static AgentReflectionSessionId New() => new(Guid.NewGuid());
    public static AgentReflectionSessionId From(Guid id) => new(id);
}
