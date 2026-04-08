using System.Collections.Concurrent;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Repositories;

/// <summary>
/// Implementação in-memory do repositório de gráficos customizados.
/// NÃO é registada em DI de produção — apenas CustomChartRepository (EF Core) é o provider ativo.
/// Mantida temporariamente para referência em testes. Será removida em versão futura.
/// </summary>
[System.Obsolete("Production uses EF Core CustomChartRepository registered in DI. This class will be removed in a future version.")]
public sealed class InMemoryCustomChartRepository : ICustomChartRepository
{
    private readonly ConcurrentDictionary<Guid, CustomChart> _store = new();

    public Task<CustomChart?> GetByIdAsync(CustomChartId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var chart);
        var result = chart?.TenantId == tenantId ? chart : null;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<CustomChart>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomChart> result = _store.Values
            .Where(c => c.UserId == userId && c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(CustomChart chart, CancellationToken cancellationToken)
    {
        _store[chart.Id.Value] = chart;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CustomChart chart, CancellationToken cancellationToken)
    {
        _store[chart.Id.Value] = chart;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CustomChartId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
