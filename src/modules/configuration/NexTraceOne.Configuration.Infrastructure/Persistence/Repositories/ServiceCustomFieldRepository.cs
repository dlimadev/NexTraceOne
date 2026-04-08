using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class ServiceCustomFieldRepository(ConfigurationDbContext context) : IServiceCustomFieldRepository
{
    public async Task<ServiceCustomField?> GetByIdAsync(ServiceCustomFieldId id, string tenantId, CancellationToken cancellationToken)
        => await context.ServiceCustomFields.SingleOrDefaultAsync(
            f => f.Id == id && f.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<ServiceCustomField>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
        => await context.ServiceCustomFields
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ServiceCustomField field, CancellationToken cancellationToken)
        => await context.ServiceCustomFields.AddAsync(field, cancellationToken);

    public Task UpdateAsync(ServiceCustomField field, CancellationToken cancellationToken)
    {
        context.ServiceCustomFields.Update(field);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ServiceCustomFieldId id, CancellationToken cancellationToken)
    {
        var entity = await context.ServiceCustomFields.SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity is not null) context.ServiceCustomFields.Remove(entity);
    }
}
