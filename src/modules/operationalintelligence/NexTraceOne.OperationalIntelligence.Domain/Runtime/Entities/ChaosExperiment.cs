using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Aggregate Root que representa um experimento de chaos engineering.
/// Permite planear, executar e registar resultados de experimentos de resiliência
/// sobre serviços em ambientes controlados.
///
/// Invariantes:
/// - ServiceName e Environment não podem ser nulos ou vazios.
/// - ExperimentType não pode ser nulo ou vazio.
/// - DurationSeconds deve estar entre 10 e 3600.
/// - TargetPercentage deve estar entre 1 e 100.
/// - Transições de estado: Planned → Running → Completed/Failed; Planned/Running → Cancelled.
/// </summary>
public sealed class ChaosExperiment : AuditableEntity<ChaosExperimentId>
{
    private const int MaxNameLength = 200;
    private const int MaxEnvironmentLength = 100;
    private const int MaxDescriptionLength = 2000;
    private const int MinDuration = 10;
    private const int MaxDuration = 3600;

    private ChaosExperiment() { }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço alvo do experimento.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde o experimento será executado.</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Tipo de experimento (latency-injection, pod-kill, cpu-stress, etc.).</summary>
    public string ExperimentType { get; private set; } = string.Empty;

    /// <summary>Descrição opcional do experimento.</summary>
    public string? Description { get; private set; }

    /// <summary>Nível de risco avaliado (Low, Medium, High).</summary>
    public string RiskLevel { get; private set; } = string.Empty;

    /// <summary>Estado atual do experimento.</summary>
    public ExperimentStatus Status { get; private set; }

    /// <summary>Duração planeada em segundos.</summary>
    public int DurationSeconds { get; private set; }

    /// <summary>Percentagem de alvos afetados.</summary>
    public decimal TargetPercentage { get; private set; }

    /// <summary>Passos do plano de execução em JSON.</summary>
    public List<string> Steps { get; private set; } = new();

    /// <summary>Verificações de segurança obrigatórias.</summary>
    public List<string> SafetyChecks { get; private set; } = new();

    /// <summary>Resultado/notas da execução.</summary>
    public string? ExecutionNotes { get; private set; }

    /// <summary>Data de início da execução.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Data de conclusão da execução.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Cria um novo experimento de chaos engineering no estado Planned.</summary>
    public static ChaosExperiment Create(
        string tenantId,
        string serviceName,
        string environment,
        string experimentType,
        string? description,
        string riskLevel,
        int durationSeconds,
        decimal targetPercentage,
        IReadOnlyList<string> steps,
        IReadOnlyList<string> safetyChecks,
        DateTimeOffset createdAt,
        string createdBy)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.OutOfRange(serviceName.Length, nameof(serviceName), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.OutOfRange(environment.Length, nameof(environment), 1, MaxEnvironmentLength);
        Guard.Against.NullOrWhiteSpace(experimentType);
        Guard.Against.NullOrWhiteSpace(riskLevel);
        Guard.Against.OutOfRange(durationSeconds, nameof(durationSeconds), MinDuration, MaxDuration);
        Guard.Against.OutOfRange(targetPercentage, nameof(targetPercentage), 1m, 100m);

        if (description is { Length: > MaxDescriptionLength })
            throw new ArgumentException($"Description must not exceed {MaxDescriptionLength} characters.", nameof(description));

        var experiment = new ChaosExperiment
        {
            Id = ChaosExperimentId.New(),
            TenantId = tenantId,
            ServiceName = serviceName,
            Environment = environment,
            ExperimentType = experimentType,
            Description = description,
            RiskLevel = riskLevel,
            Status = ExperimentStatus.Planned,
            DurationSeconds = durationSeconds,
            TargetPercentage = targetPercentage,
            Steps = new List<string>(steps),
            SafetyChecks = new List<string>(safetyChecks),
        };
        experiment.SetCreated(createdAt, createdBy);

        return experiment;
    }

    /// <summary>Inicia a execução do experimento.</summary>
    public void Start(DateTimeOffset startedAt)
    {
        if (Status != ExperimentStatus.Planned)
            throw new InvalidOperationException($"Cannot start experiment in status {Status}. Expected Planned.");

        Status = ExperimentStatus.Running;
        StartedAt = startedAt;
        SetUpdated(startedAt, CreatedBy);
    }

    /// <summary>Marca o experimento como concluído com sucesso.</summary>
    public void Complete(DateTimeOffset completedAt, string? notes = null)
    {
        if (Status != ExperimentStatus.Running)
            throw new InvalidOperationException($"Cannot complete experiment in status {Status}. Expected Running.");

        Status = ExperimentStatus.Completed;
        CompletedAt = completedAt;
        ExecutionNotes = notes;
        SetUpdated(completedAt, CreatedBy);
    }

    /// <summary>Marca o experimento como falhado.</summary>
    public void Fail(DateTimeOffset failedAt, string? notes = null)
    {
        if (Status != ExperimentStatus.Running)
            throw new InvalidOperationException($"Cannot fail experiment in status {Status}. Expected Running.");

        Status = ExperimentStatus.Failed;
        CompletedAt = failedAt;
        ExecutionNotes = notes;
        SetUpdated(failedAt, CreatedBy);
    }

    /// <summary>Cancela o experimento.</summary>
    public void Cancel(DateTimeOffset cancelledAt, string? notes = null)
    {
        if (Status is ExperimentStatus.Completed or ExperimentStatus.Failed or ExperimentStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel experiment in terminal status {Status}.");

        Status = ExperimentStatus.Cancelled;
        CompletedAt = cancelledAt;
        ExecutionNotes = notes;
        SetUpdated(cancelledAt, CreatedBy);
    }
}

/// <summary>Identificador fortemente tipado para ChaosExperiment.</summary>
public sealed record ChaosExperimentId(Guid Value) : TypedIdBase(Value)
{
    public static ChaosExperimentId New() => new(Guid.NewGuid());

    public static ChaosExperimentId From(Guid id) => new(id);
}
