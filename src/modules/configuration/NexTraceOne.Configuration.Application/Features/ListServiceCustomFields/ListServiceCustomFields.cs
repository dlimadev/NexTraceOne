using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListServiceCustomFields;

/// <summary>Feature: ListServiceCustomFields — lista os campos personalizados de serviços do tenant.</summary>
public static class ListServiceCustomFields
{
    public sealed record Query(string TenantId) : IQuery<Response>;

    public sealed class Handler(IServiceCustomFieldRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var fields = await repository.ListByTenantAsync(request.TenantId, cancellationToken);
            var items = fields.OrderBy(f => f.SortOrder)
                .Select(f => new FieldSummary(f.Id.Value, f.FieldName, f.FieldType, f.IsRequired, f.DefaultValue, f.SortOrder))
                .ToList();
            return Result<Response>.Success(new Response(items));
        }
    }

    public sealed record FieldSummary(Guid FieldId, string FieldName, string FieldType, bool IsRequired, string DefaultValue, int SortOrder);
    public sealed record Response(IReadOnlyList<FieldSummary> Items);
}
