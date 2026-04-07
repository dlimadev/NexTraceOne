using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de templates de webhook.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryWebhookTemplateRepository : IWebhookTemplateRepository
{
    private readonly ConcurrentDictionary<Guid, WebhookTemplate> _store = new();

    public Task<WebhookTemplate?> GetByIdAsync(WebhookTemplateId id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var template);
        return Task.FromResult(template);
    }

    public Task<IReadOnlyList<WebhookTemplate>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<WebhookTemplate> result = _store.Values
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(WebhookTemplate template, CancellationToken cancellationToken)
    {
        _store[template.Id.Value] = template;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WebhookTemplate template, CancellationToken cancellationToken)
    {
        _store[template.Id.Value] = template;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WebhookTemplateId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
