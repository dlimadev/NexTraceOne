using NexTraceOne.ProductAnalytics.Application.Abstractions;

namespace NexTraceOne.ProductAnalytics.Application;

/// <summary>
/// Implementação nula de <see cref="ISelfServiceWorkflowReader"/>.
/// Utilizada quando não existe fonte de dados de workflows de self-service configurada.
/// </summary>
public sealed class NullSelfServiceWorkflowReader : ISelfServiceWorkflowReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<ISelfServiceWorkflowReader.WorkflowExecutionEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ISelfServiceWorkflowReader.WorkflowExecutionEntry>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ISelfServiceWorkflowReader.AbandonmentHotspot>> GetAbandonmentHotspotsAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ISelfServiceWorkflowReader.AbandonmentHotspot>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<ISelfServiceWorkflowReader.WorkflowReleaseSnapshot>> GetTrendByReleaseAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ISelfServiceWorkflowReader.WorkflowReleaseSnapshot>>([]);
}
