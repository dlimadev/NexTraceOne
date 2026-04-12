using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class ContractTemplateRepository(ConfigurationDbContext context) : IContractTemplateRepository
{
    public async Task<ContractTemplate?> GetByIdAsync(ContractTemplateId id, string tenantId, CancellationToken cancellationToken)
        => await context.ContractTemplates.SingleOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<ContractTemplate>> ListByTenantAsync(string tenantId, string? contractType, CancellationToken cancellationToken)
        => await context.ContractTemplates
            .Where(t => t.TenantId == tenantId
                && (contractType == null || t.ContractType == contractType))
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ContractTemplate template, CancellationToken cancellationToken)
        => await context.ContractTemplates.AddAsync(template, cancellationToken);

    public async Task DeleteAsync(ContractTemplateId id, CancellationToken cancellationToken)
    {
        var entity = await context.ContractTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is not null) context.ContractTemplates.Remove(entity);
    }
}
