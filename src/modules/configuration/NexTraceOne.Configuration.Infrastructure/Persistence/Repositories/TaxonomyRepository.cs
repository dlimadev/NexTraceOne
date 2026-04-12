using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class TaxonomyRepository(ConfigurationDbContext context) : ITaxonomyRepository
{
    public async Task<TaxonomyCategory?> GetCategoryByIdAsync(TaxonomyCategoryId id, string tenantId, CancellationToken cancellationToken)
        => await context.TaxonomyCategories.SingleOrDefaultAsync(
            c => c.Id == id && c.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<TaxonomyCategory>> ListCategoriesAsync(string tenantId, CancellationToken cancellationToken)
        => await context.TaxonomyCategories
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaxonomyValue>> ListValuesByCategoryAsync(TaxonomyCategoryId categoryId, string tenantId, CancellationToken cancellationToken)
        => await context.TaxonomyValues
            .Where(v => v.CategoryId == categoryId && v.TenantId == tenantId)
            .OrderBy(v => v.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddCategoryAsync(TaxonomyCategory category, CancellationToken cancellationToken)
        => await context.TaxonomyCategories.AddAsync(category, cancellationToken);

    public async Task AddValueAsync(TaxonomyValue value, CancellationToken cancellationToken)
        => await context.TaxonomyValues.AddAsync(value, cancellationToken);

    public async Task DeleteCategoryAsync(TaxonomyCategoryId id, CancellationToken cancellationToken)
    {
        var entity = await context.TaxonomyCategories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is not null) context.TaxonomyCategories.Remove(entity);
    }

    public async Task DeleteValueAsync(TaxonomyValueId id, CancellationToken cancellationToken)
    {
        var entity = await context.TaxonomyValues.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (entity is not null) context.TaxonomyValues.Remove(entity);
    }
}
