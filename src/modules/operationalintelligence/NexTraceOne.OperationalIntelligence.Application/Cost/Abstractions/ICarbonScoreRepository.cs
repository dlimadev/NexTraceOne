using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>Repositório de registos de carbon score por serviço.</summary>
public interface ICarbonScoreRepository
{
    Task<IReadOnlyList<CarbonScoreRecord>> ListByTenantAndPeriodAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<CarbonScoreRecord>> ListByServiceAsync(
        Guid serviceId, Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task UpsertAsync(CarbonScoreRecord record, CancellationToken cancellationToken);
}
