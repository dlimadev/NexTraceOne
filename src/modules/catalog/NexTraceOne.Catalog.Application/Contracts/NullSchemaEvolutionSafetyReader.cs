using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>Wave AQ.3 — honest-null ISchemaEvolutionSafetyReader.</summary>
public sealed class NullSchemaEvolutionSafetyReader : ISchemaEvolutionSafetyReader
{
    public Task<IReadOnlyList<ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry>>([]);
}
