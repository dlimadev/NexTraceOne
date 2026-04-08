using Microsoft.EntityFrameworkCore;

using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de subscrições de webhook.
/// Usa AsNoTracking nas consultas de leitura para melhor performance.
/// </summary>
internal sealed class WebhookSubscriptionRepository(IntegrationsDbContext context) : IWebhookSubscriptionRepository
{
    public async Task<(IReadOnlyList<WebhookSubscription> Items, int TotalCount)> ListAsync(
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.WebhookSubscriptions.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<WebhookSubscription?> GetByIdAsync(WebhookSubscriptionId id, CancellationToken ct)
        => await context.WebhookSubscriptions.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(WebhookSubscription subscription, CancellationToken ct)
        => await context.WebhookSubscriptions.AddAsync(subscription, ct);

    public Task UpdateAsync(WebhookSubscription subscription, CancellationToken ct)
    {
        context.WebhookSubscriptions.Update(subscription);
        return Task.CompletedTask;
    }
}
