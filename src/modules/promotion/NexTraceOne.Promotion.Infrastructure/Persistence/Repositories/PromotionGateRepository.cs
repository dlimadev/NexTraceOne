using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;

namespace NexTraceOne.Promotion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de gates de promoção, implementando consultas específicas de negócio.
/// </summary>
internal sealed class PromotionGateRepository(PromotionDbContext context)
    : RepositoryBase<PromotionGate, PromotionGateId>(context), IPromotionGateRepository
{
    /// <summary>Busca um gate de promoção pelo identificador.</summary>
    public override async Task<PromotionGate?> GetByIdAsync(PromotionGateId id, CancellationToken ct = default)
        => await context.PromotionGates.SingleOrDefaultAsync(g => g.Id == id, ct);

    /// <summary>Lista todos os gates ativos vinculados a um ambiente de deployment.</summary>
    public async Task<IReadOnlyList<PromotionGate>> ListByEnvironmentIdAsync(DeploymentEnvironmentId envId, CancellationToken ct)
        => await context.PromotionGates
            .Where(g => g.DeploymentEnvironmentId == envId && g.IsActive)
            .OrderBy(g => g.GateName)
            .ToListAsync(ct);

    /// <summary>Lista apenas os gates obrigatórios ativos vinculados a um ambiente de deployment.</summary>
    public async Task<IReadOnlyList<PromotionGate>> ListRequiredByEnvironmentIdAsync(DeploymentEnvironmentId envId, CancellationToken ct)
        => await context.PromotionGates
            .Where(g => g.DeploymentEnvironmentId == envId && g.IsRequired && g.IsActive)
            .OrderBy(g => g.GateName)
            .ToListAsync(ct);
}
