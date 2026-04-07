using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de taxonomias.</summary>
public interface ITaxonomyRepository
{
    Task<TaxonomyCategory?> GetCategoryByIdAsync(TaxonomyCategoryId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaxonomyCategory>> ListCategoriesAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaxonomyValue>> ListValuesByCategoryAsync(TaxonomyCategoryId categoryId, string tenantId, CancellationToken cancellationToken);
    Task AddCategoryAsync(TaxonomyCategory category, CancellationToken cancellationToken);
    Task AddValueAsync(TaxonomyValue value, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(TaxonomyCategoryId id, CancellationToken cancellationToken);
    Task DeleteValueAsync(TaxonomyValueId id, CancellationToken cancellationToken);
}
