using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de gates de promoção configuráveis.
/// </summary>
internal sealed class PromotionGateRepository(ChangeIntelligenceDbContext context) : IPromotionGateRepository
{
    /// <inheritdoc />
    public async Task<PromotionGate?> GetByIdAsync(PromotionGateId id, CancellationToken cancellationToken = default)
        => await context.PromotionGates
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionGate>> ListByEnvironmentAsync(
        string environmentFrom, string environmentTo, CancellationToken cancellationToken = default)
        => await context.PromotionGates
            .Where(g => g.EnvironmentFrom == environmentFrom && g.EnvironmentTo == environmentTo)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionGate>> ListActiveAsync(CancellationToken cancellationToken = default)
        => await context.PromotionGates
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(PromotionGate gate, CancellationToken cancellationToken = default)
        => await context.PromotionGates.AddAsync(gate, cancellationToken);
}
