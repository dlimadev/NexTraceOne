namespace NexTraceOne.ProductAnalytics.Application.Abstractions;

/// <summary>
/// Leitor de dados de saúde dos workflows de self-service do NexTraceOne.
/// Agrega eventos de início e conclusão de fluxos (CreateService, CreateContractDraft, etc.)
/// para medir CompletionRate, AbandonmentRate e AdminInterventionRate.
/// </summary>
public interface ISelfServiceWorkflowReader
{
    /// <summary>Lista dados de execução de workflows de self-service para o período.</summary>
    Task<IReadOnlyList<WorkflowExecutionEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Lista hotspots de abandono (etapas com maior frequência de abandono).</summary>
    Task<IReadOnlyList<AbandonmentHotspot>> GetAbandonmentHotspotsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Retorna tendência de CompletionRate por release da plataforma.</summary>
    Task<IReadOnlyList<WorkflowReleaseSnapshot>> GetTrendByReleaseAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Dados de execução de um workflow de self-service.</summary>
    public sealed record WorkflowExecutionEntry(
        string WorkflowName,
        int AttemptCount,
        int SuccessfulCompletions,
        int AbandonedCount,
        int AdminInterventionCount,
        double AvgCompletionTimeMinutes);

    /// <summary>Hotspot de abandono em etapa específica de um workflow.</summary>
    public sealed record AbandonmentHotspot(
        string WorkflowName,
        string StepName,
        int AbandonCount,
        string Description);

    /// <summary>Snapshot de taxa de conclusão após release da plataforma.</summary>
    public sealed record WorkflowReleaseSnapshot(
        string ReleaseLabel,
        DateTimeOffset ReleasedAt,
        double AvgCompletionRate);
}
