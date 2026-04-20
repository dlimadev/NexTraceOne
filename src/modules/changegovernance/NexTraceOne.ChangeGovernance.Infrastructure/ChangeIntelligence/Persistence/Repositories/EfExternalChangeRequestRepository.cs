using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para pedidos de mudança externos.
/// Implementa consultas por chave natural, estado e serviço afetado.
/// </summary>
internal sealed class EfExternalChangeRequestRepository(ChangeIntelligenceDbContext context)
    : IExternalChangeRequestRepository
{
    /// <inheritdoc />
    public async Task<ExternalChangeRequest?> GetByExternalIdAsync(
        string externalSystem,
        string externalId,
        CancellationToken ct)
        => await context.ExternalChangeRequests
            .SingleOrDefaultAsync(
                r => r.ExternalSystem == externalSystem && r.ExternalId == externalId,
                ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalChangeRequest>> ListByStatusAsync(
        ExternalChangeRequestStatus status,
        CancellationToken ct)
        => await context.ExternalChangeRequests
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.IngestedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalChangeRequest>> ListByServiceAsync(
        Guid serviceId,
        CancellationToken ct)
        => await context.ExternalChangeRequests
            .Where(r => r.ServiceId == serviceId)
            .OrderByDescending(r => r.IngestedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public void Add(ExternalChangeRequest request)
        => context.ExternalChangeRequests.Add(request);
}
