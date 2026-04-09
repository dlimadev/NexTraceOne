using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Registo de execução de um playbook operacional.
/// Captura resultados por passo, evidências recolhidas e notas do operador.
/// Pode estar ligado opcionalmente a um incidente.
///
/// Ciclo de vida: InProgress → Completed | Failed | Aborted.
///
/// Invariantes:
/// - PlaybookId não pode ser vazio.
/// - PlaybookName não pode ser nulo ou vazio.
/// - ExecutedByUserId não pode ser nulo ou vazio.
/// - TenantId não pode ser nulo ou vazio.
/// </summary>
public sealed class PlaybookExecution : AuditableEntity<PlaybookExecutionId>
{
    private PlaybookExecution() { }

    /// <summary>ID do playbook executado.</summary>
    public Guid PlaybookId { get; private set; }

    /// <summary>Nome do playbook (desnormalizado para exibição).</summary>
    public string PlaybookName { get; private set; } = string.Empty;

    /// <summary>ID do incidente associado (opcional).</summary>
    public Guid? IncidentId { get; private set; }

    /// <summary>ID do utilizador que executou o playbook.</summary>
    public string ExecutedByUserId { get; private set; } = string.Empty;

    /// <summary>Estado atual da execução.</summary>
    public PlaybookExecutionStatus Status { get; private set; }

    /// <summary>Resultados por passo (JSONB).</summary>
    public string? StepResults { get; private set; }

    /// <summary>Evidências recolhidas durante a execução (JSONB).</summary>
    public string? Evidence { get; private set; }

    /// <summary>Notas do operador.</summary>
    public string? Notes { get; private set; }

    /// <summary>Data/hora de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Data/hora de conclusão da execução.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista.</summary>
    public uint RowVersion { get; private set; }

    /// <summary>Inicia uma nova execução de playbook no estado InProgress.</summary>
    public static PlaybookExecution Start(
        Guid playbookId,
        string playbookName,
        Guid? incidentId,
        string executedByUserId,
        string tenantId,
        DateTimeOffset startedAt)
    {
        Guard.Against.Default(playbookId);
        Guard.Against.NullOrWhiteSpace(playbookName);
        Guard.Against.NullOrWhiteSpace(executedByUserId);
        Guard.Against.NullOrWhiteSpace(tenantId);

        var execution = new PlaybookExecution
        {
            Id = PlaybookExecutionId.New(),
            PlaybookId = playbookId,
            PlaybookName = playbookName,
            IncidentId = incidentId,
            ExecutedByUserId = executedByUserId,
            Status = PlaybookExecutionStatus.InProgress,
            StartedAt = startedAt,
            TenantId = tenantId,
        };
        execution.SetCreated(startedAt, executedByUserId);

        return execution;
    }

    /// <summary>
    /// Marca a execução como concluída com sucesso.
    /// Transição: InProgress → Completed.
    /// </summary>
    public Result<Unit> Complete(string? stepResults, string? evidence, string? notes, DateTimeOffset completedAt)
    {
        if (Status != PlaybookExecutionStatus.InProgress)
            return RuntimeIntelligenceErrors.PlaybookExecutionInvalidTransition(
                Id.Value.ToString(), Status.ToString(), PlaybookExecutionStatus.Completed.ToString());

        Status = PlaybookExecutionStatus.Completed;
        StepResults = stepResults;
        Evidence = evidence;
        Notes = notes;
        CompletedAt = completedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Marca a execução como falhada.
    /// Transição: InProgress → Failed.
    /// </summary>
    public Result<Unit> Fail(string? stepResults, string? evidence, string? errorNotes, DateTimeOffset failedAt)
    {
        if (Status != PlaybookExecutionStatus.InProgress)
            return RuntimeIntelligenceErrors.PlaybookExecutionInvalidTransition(
                Id.Value.ToString(), Status.ToString(), PlaybookExecutionStatus.Failed.ToString());

        Status = PlaybookExecutionStatus.Failed;
        StepResults = stepResults;
        Evidence = evidence;
        Notes = errorNotes;
        CompletedAt = failedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Aborta a execução manualmente.
    /// Transição: InProgress → Aborted.
    /// </summary>
    public Result<Unit> Abort(string? notes, DateTimeOffset abortedAt)
    {
        if (Status != PlaybookExecutionStatus.InProgress)
            return RuntimeIntelligenceErrors.PlaybookExecutionInvalidTransition(
                Id.Value.ToString(), Status.ToString(), PlaybookExecutionStatus.Aborted.ToString());

        Status = PlaybookExecutionStatus.Aborted;
        Notes = notes;
        CompletedAt = abortedAt;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado para PlaybookExecution.</summary>
public sealed record PlaybookExecutionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PlaybookExecutionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PlaybookExecutionId From(Guid id) => new(id);
}
