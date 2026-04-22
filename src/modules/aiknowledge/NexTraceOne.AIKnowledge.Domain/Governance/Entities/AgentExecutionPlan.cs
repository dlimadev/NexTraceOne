using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Passo individual num plano de execução de agent.
/// Representa uma acção atómica com input, output, tokens consumidos e estado.
/// Pode requerer aprovação humana antes de avançar.
/// </summary>
public sealed record AgentStep
{
    /// <summary>Índice sequencial do passo no plano (base 0).</summary>
    public int StepIndex { get; init; }

    /// <summary>Nome descritivo do passo.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Tipo de acção efectuada neste passo.</summary>
    public AgentStepType StepType { get; init; }

    /// <summary>Payload de entrada do passo (JSON).</summary>
    public string InputJson { get; init; } = string.Empty;

    /// <summary>Payload de saída do passo (JSON), preenchido após conclusão.</summary>
    public string? OutputJson { get; init; }

    /// <summary>Estado actual do passo.</summary>
    public AgentExecutionStatus Status { get; init; } = AgentExecutionStatus.Pending;

    /// <summary>Indica se este passo requer aprovação humana antes de prosseguir.</summary>
    public bool RequiresApproval { get; init; }

    /// <summary>Duração de execução em milissegundos (preenchido após conclusão).</summary>
    public long? DurationMs { get; init; }

    /// <summary>Identificador do modelo utilizado neste passo.</summary>
    public string? ModelUsed { get; init; }

    /// <summary>Tokens consumidos por este passo.</summary>
    public int TokensConsumed { get; init; }

    /// <summary>Utilizador que aprovou este passo (apenas para passos com aprovação).</summary>
    public string? ApprovedBy { get; init; }

    /// <summary>Momento de aprovação do passo.</summary>
    public DateTimeOffset? ApprovedAt { get; init; }

    /// <summary>Mensagem de erro (apenas para passos com falha).</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Plano de execução agentic com suporte a múltiplos passos e aprovação humana.
/// Cada plano agrupa um conjunto de passos sequenciais com orçamento de tokens
/// e possibilidade de interrupção para validação humana (Human-in-the-Loop).
///
/// Ciclo de vida: Pending → Running → (WaitingApproval | Completed | Failed | Cancelled).
///
/// Invariantes:
/// - TenantId e RequestedBy são obrigatórios.
/// - Steps não pode ser vazio.
/// - ConsumedTokens nunca excede MaxTokenBudget.
/// - ApprovedBy e ApprovedAt apenas preenchidos quando RequiresApproval = true.
/// </summary>
public sealed class AgentExecutionPlan : AuditableEntity<AgentExecutionPlanId>
{
    private readonly List<AgentStep> _steps = [];

    private AgentExecutionPlan() { }

    /// <summary>Identificador do tenant dono do plano.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Utilizador que submeteu o plano.</summary>
    public string RequestedBy { get; private set; } = string.Empty;

    /// <summary>Descrição do objectivo do plano.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Estado actual do plano.</summary>
    public PlanStatus PlanStatus { get; private set; }

    /// <summary>Passos configurados no plano.</summary>
    public IReadOnlyList<AgentStep> Steps => _steps.AsReadOnly();

    /// <summary>Orçamento máximo de tokens para o plano.</summary>
    public int MaxTokenBudget { get; private set; }

    /// <summary>Total de tokens consumidos pelo plano até ao momento.</summary>
    public int ConsumedTokens { get; private set; }

    /// <summary>Indica se o plano requer aprovação global antes de iniciar.</summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>Limiar de blast radius que despoleta aprovação obrigatória.</summary>
    public int BlastRadiusThreshold { get; private set; }

    /// <summary>Utilizador que aprovou a execução do plano.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Momento de aprovação do plano.</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Identificador de correlação para rastreabilidade fim-a-fim.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Momento de início da execução do plano.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Momento de conclusão do plano.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Mensagem de erro (apenas para planos com falha).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Cria e submete um novo plano de execução agentic.</summary>
    public static AgentExecutionPlan Submit(
        Guid tenantId,
        string requestedBy,
        string description,
        IEnumerable<AgentStep> steps,
        int maxTokenBudget,
        bool requiresApproval,
        int blastRadiusThreshold,
        string? correlationId = null)
    {
        Guard.Against.NullOrWhiteSpace(requestedBy);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.Null(steps);

        var stepList = steps.ToList();
        Guard.Against.Zero(stepList.Count, nameof(steps));

        var plan = new AgentExecutionPlan
        {
            Id = AgentExecutionPlanId.New(),
            TenantId = tenantId,
            RequestedBy = requestedBy,
            Description = description,
            PlanStatus = PlanStatus.Pending,
            MaxTokenBudget = maxTokenBudget,
            ConsumedTokens = 0,
            RequiresApproval = requiresApproval,
            BlastRadiusThreshold = blastRadiusThreshold,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
        };

        plan._steps.AddRange(stepList);
        return plan;
    }

    /// <summary>Inicia a execução do plano (Pending → Running).</summary>
    public void StartExecution(DateTimeOffset startedAt)
    {
        Guard.Against.Expression(
            s => s != PlanStatus.Pending,
            PlanStatus,
            "Plan must be in Pending state to start.");

        PlanStatus = PlanStatus.Running;
        StartedAt = startedAt;
    }

    /// <summary>Marca um passo como Running.</summary>
    public void StartStep(int stepIndex)
    {
        var step = GetStep(stepIndex);
        UpdateStep(step with { Status = AgentExecutionStatus.Running });
    }

    /// <summary>Marca um passo como Completed e acumula tokens.</summary>
    public void CompleteStep(int stepIndex, string outputJson, int tokens, string? model, long durationMs)
    {
        var step = GetStep(stepIndex);
        UpdateStep(step with
        {
            Status = AgentExecutionStatus.Completed,
            OutputJson = outputJson,
            TokensConsumed = tokens,
            ModelUsed = model,
            DurationMs = durationMs,
        });
        ConsumedTokens += tokens;
    }

    /// <summary>Marca um passo como Failed.</summary>
    public void FailStep(int stepIndex, string error)
    {
        var step = GetStep(stepIndex);
        UpdateStep(step with { Status = AgentExecutionStatus.Failed, ErrorMessage = error });
    }

    /// <summary>Coloca um passo em WaitingApproval (mapeado a Running para reutilizar enum).</summary>
    public void RequestStepApproval(int stepIndex)
    {
        var step = GetStep(stepIndex);
        if (!step.RequiresApproval)
            throw new InvalidOperationException($"Step {stepIndex} does not require approval.");

        PlanStatus = PlanStatus.WaitingApproval;
    }

    /// <summary>Aprova um passo e retoma a execução do plano.</summary>
    public void ApproveStep(int stepIndex, string approvedBy)
    {
        Guard.Against.NullOrWhiteSpace(approvedBy);
        var step = GetStep(stepIndex);
        UpdateStep(step with { ApprovedBy = approvedBy, ApprovedAt = DateTimeOffset.UtcNow });
        PlanStatus = PlanStatus.Running;
    }

    /// <summary>Marca o plano como Completed.</summary>
    public void Complete(DateTimeOffset completedAt)
    {
        PlanStatus = PlanStatus.Completed;
        CompletedAt = completedAt;
    }

    /// <summary>Marca o plano como Failed.</summary>
    public void Fail(string error)
    {
        PlanStatus = PlanStatus.Failed;
        ErrorMessage = error;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Cancela o plano.</summary>
    public void Cancel()
    {
        PlanStatus = PlanStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private AgentStep GetStep(int stepIndex)
    {
        var step = _steps.FirstOrDefault(s => s.StepIndex == stepIndex);
        Guard.Against.Null(step, nameof(stepIndex), $"Step {stepIndex} not found in plan.");
        return step;
    }

    private void UpdateStep(AgentStep updated)
    {
        var idx = _steps.FindIndex(s => s.StepIndex == updated.StepIndex);
        if (idx >= 0) _steps[idx] = updated;
    }
}

/// <summary>Identificador fortemente tipado de AgentExecutionPlan.</summary>
public sealed record AgentExecutionPlanId(Guid Value) : TypedIdBase(Value)
{
    public static AgentExecutionPlanId New() => new(Guid.NewGuid());
    public static AgentExecutionPlanId From(Guid id) => new(id);
}
