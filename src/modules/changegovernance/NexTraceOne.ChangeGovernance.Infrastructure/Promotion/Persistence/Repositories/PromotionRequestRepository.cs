using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Repositories;

/// <summary>
/// Repositório de solicitações de promoção, implementando consultas específicas de negócio.
/// </summary>
internal sealed class PromotionRequestRepository(PromotionDbContext context)
    : RepositoryBase<PromotionRequest, PromotionRequestId>(context), IPromotionRequestRepository
{
    /// <summary>Busca uma solicitação de promoção pelo identificador.</summary>
    public override async Task<PromotionRequest?> GetByIdAsync(PromotionRequestId id, CancellationToken ct = default)
        => await context.PromotionRequests.SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Lista solicitações de promoção pelo status.</summary>
    public async Task<IReadOnlyList<PromotionRequest>> ListByStatusAsync(PromotionStatus status, CancellationToken ct)
        => await context.PromotionRequests
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

    /// <summary>Lista solicitações de promoção pelo identificador da release.</summary>
    public async Task<IReadOnlyList<PromotionRequest>> ListByReleaseIdAsync(Guid releaseId, CancellationToken ct)
        => await context.PromotionRequests
            .Where(r => r.ReleaseId == releaseId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

    /// <summary>Conta solicitações de promoção por status.</summary>
    public async Task<int> CountByStatusAsync(PromotionStatus status, CancellationToken ct)
        => await context.PromotionRequests.CountAsync(r => r.Status == status, ct);
}
