using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação null (honest-null) de <see cref="IMigrationProgressReader"/>.
/// Retorna lista vazia — nenhum serviço em migração registado.
/// Wave AF.3 — GetServiceMigrationProgressReport.
/// </summary>
public sealed class NullMigrationProgressReader : IMigrationProgressReader
{
    public Task<IReadOnlyList<MigrationProgressEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<MigrationProgressEntry>>([]);
}
