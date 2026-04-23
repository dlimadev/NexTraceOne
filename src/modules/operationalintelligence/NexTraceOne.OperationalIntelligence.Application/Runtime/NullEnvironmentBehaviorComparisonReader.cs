using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime;

/// <summary>
/// Implementação honest-null de <see cref="IEnvironmentBehaviorComparisonReader"/>.
/// Retorna listas vazias até a infraestrutura real ser ligada.
/// Wave BC.1 — GetEnvironmentBehaviorComparisonReport.
/// </summary>
public sealed class NullEnvironmentBehaviorComparisonReader : IEnvironmentBehaviorComparisonReader
{
    public Task<IReadOnlyList<IEnvironmentBehaviorComparisonReader.ServiceBehaviorEntry>> ListByTenantAsync(
        string tenantId, string sourceEnvironment, string targetEnvironment, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IEnvironmentBehaviorComparisonReader.ServiceBehaviorEntry>>([]);

    public Task<IReadOnlyList<IEnvironmentBehaviorComparisonReader.PromotionOutcomeEntry>>
        GetHistoricalPromotionOutcomesAsync(string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IEnvironmentBehaviorComparisonReader.PromotionOutcomeEntry>>([]);
}
