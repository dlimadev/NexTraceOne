using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de templates de webhook personalizados do tenant.</summary>
public interface IWebhookTemplateRepository
{
    Task<WebhookTemplate?> GetByIdAsync(WebhookTemplateId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WebhookTemplate>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken);
    Task AddAsync(WebhookTemplate template, CancellationToken cancellationToken);
    Task UpdateAsync(WebhookTemplate template, CancellationToken cancellationToken);
    Task DeleteAsync(WebhookTemplateId id, CancellationToken cancellationToken);
}
