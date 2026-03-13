using Microsoft.EntityFrameworkCore;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de subscrições de API, implementando persistência via EF Core.
/// Suporta consultas por API, subscritor e combinação única (API + subscritor).
/// </summary>
internal sealed class SubscriptionRepository(DeveloperPortalDbContext context) : ISubscriptionRepository
{
    /// <summary>Busca subscrição por identificador único.</summary>
    public async Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken ct = default)
        => await context.Subscriptions.SingleOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>Busca subscrição por combinação única API + subscritor para verificação de duplicidade.</summary>
    public async Task<Subscription?> GetByApiAndSubscriberAsync(Guid apiAssetId, Guid subscriberId, CancellationToken ct = default)
        => await context.Subscriptions
            .SingleOrDefaultAsync(s => s.ApiAssetId == apiAssetId && s.SubscriberId == subscriberId, ct);

    /// <summary>Lista todas as subscrições de um utilizador.</summary>
    public async Task<IReadOnlyList<Subscription>> GetBySubscriberAsync(Guid subscriberId, CancellationToken ct = default)
        => await context.Subscriptions
            .Where(s => s.SubscriberId == subscriberId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Lista todas as subscrições de uma API.</summary>
    public async Task<IReadOnlyList<Subscription>> GetByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.Subscriptions
            .Where(s => s.ApiAssetId == apiAssetId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Adiciona nova subscrição ao contexto.</summary>
    public void Add(Subscription subscription)
        => context.Subscriptions.Add(subscription);

    /// <summary>Marca subscrição como modificada no contexto.</summary>
    public void Update(Subscription subscription)
        => context.Subscriptions.Update(subscription);

    /// <summary>Remove subscrição do contexto.</summary>
    public void Remove(Subscription subscription)
        => context.Subscriptions.Remove(subscription);
}
