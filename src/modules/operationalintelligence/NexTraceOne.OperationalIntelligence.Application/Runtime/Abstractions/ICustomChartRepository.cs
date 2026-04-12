using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>Contrato do repositório para gráficos customizados.</summary>
public interface ICustomChartRepository
{
    Task<CustomChart?> GetByIdAsync(CustomChartId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomChart>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken);
    Task AddAsync(CustomChart chart, CancellationToken cancellationToken);
    Task UpdateAsync(CustomChart chart, CancellationToken cancellationToken);
    Task DeleteAsync(CustomChartId id, CancellationToken cancellationToken);
}
