using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListTaxonomyCategories;

/// <summary>Feature: ListTaxonomyCategories — lista as categorias de taxonomia do tenant.</summary>
public static class ListTaxonomyCategories
{
    public sealed record Query(string TenantId) : IQuery<Response>;

    public sealed class Handler(ITaxonomyRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var categories = await repository.ListCategoriesAsync(request.TenantId, cancellationToken);
            var items = categories.Select(c => new CategorySummary(c.Id.Value, c.Name, c.Description, c.IsRequired)).ToList();
            return Result<Response>.Success(new Response(items));
        }
    }

    public sealed record CategorySummary(Guid CategoryId, string Name, string Description, bool IsRequired);
    public sealed record Response(IReadOnlyList<CategorySummary> Items);
}
