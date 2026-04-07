using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>Implementação em memória do repositório de taxonomias.</summary>
public sealed class InMemoryTaxonomyRepository : ITaxonomyRepository
{
    private readonly ConcurrentDictionary<Guid, TaxonomyCategory> _categories = new();
    private readonly ConcurrentDictionary<Guid, TaxonomyValue> _values = new();

    public Task<TaxonomyCategory?> GetCategoryByIdAsync(TaxonomyCategoryId id, string tenantId, CancellationToken cancellationToken)
    {
        _categories.TryGetValue(id.Value, out var cat);
        return Task.FromResult(cat?.TenantId == tenantId ? cat : null);
    }

    public Task<IReadOnlyList<TaxonomyCategory>> ListCategoriesAsync(string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<TaxonomyCategory> result = _categories.Values
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<TaxonomyValue>> ListValuesByCategoryAsync(TaxonomyCategoryId categoryId, string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<TaxonomyValue> result = _values.Values
            .Where(v => v.CategoryId.Value == categoryId.Value && v.TenantId == tenantId)
            .OrderBy(v => v.SortOrder)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddCategoryAsync(TaxonomyCategory category, CancellationToken cancellationToken)
    {
        _categories[category.Id.Value] = category;
        return Task.CompletedTask;
    }

    public Task AddValueAsync(TaxonomyValue value, CancellationToken cancellationToken)
    {
        _values[value.Id.Value] = value;
        return Task.CompletedTask;
    }

    public Task DeleteCategoryAsync(TaxonomyCategoryId id, CancellationToken cancellationToken)
    {
        _categories.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }

    public Task DeleteValueAsync(TaxonomyValueId id, CancellationToken cancellationToken)
    {
        _values.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
