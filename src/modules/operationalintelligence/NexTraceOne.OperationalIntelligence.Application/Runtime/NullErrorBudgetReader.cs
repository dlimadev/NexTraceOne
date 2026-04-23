using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime;

/// <summary>
/// Implementação null (honest-null) de IErrorBudgetReader.
/// Retorna lista vazia — sem dados de SLO disponíveis.
/// Wave AN.1 — GetErrorBudgetReport.
/// </summary>
public sealed class NullErrorBudgetReader : IErrorBudgetReader
{
    public Task<IReadOnlyList<IErrorBudgetReader.ServiceSloEntry>> ListByTenantAsync(
        string tenantId, int periodDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IErrorBudgetReader.ServiceSloEntry>>([]);
}
