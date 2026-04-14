using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class WebhookTemplateRepository(ConfigurationDbContext context) : IWebhookTemplateRepository
{
    public async Task<WebhookTemplate?> GetByIdAsync(WebhookTemplateId id, CancellationToken cancellationToken)
        => await context.WebhookTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WebhookTemplate>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
        => await context.WebhookTemplates
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WebhookTemplate template, CancellationToken cancellationToken)
    {
        await context.WebhookTemplates.AddAsync(template, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WebhookTemplate template, CancellationToken cancellationToken)
    {
        context.WebhookTemplates.Update(template);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WebhookTemplateId id, CancellationToken cancellationToken)
    {
        var entity = await context.WebhookTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is not null)
        {
            context.WebhookTemplates.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
