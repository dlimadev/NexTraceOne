using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListCustomCharts;

/// <summary>Feature: ListCustomCharts — lista os gráficos customizados do utilizador.</summary>
public static class ListCustomCharts
{
    public sealed record Query(string UserId, string TenantId) : IQuery<Response>;

    public sealed class Handler(ICustomChartRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var charts = await repository.ListByUserAsync(request.UserId, request.TenantId, cancellationToken);
            var items = charts.Select(c => new ChartSummary(
                c.Id.Value,
                c.Name,
                c.ChartType.ToString(),
                c.TimeRange,
                c.IsShared,
                c.CreatedAt)).ToList();
            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    public sealed record ChartSummary(Guid ChartId, string Name, string ChartType, string TimeRange, bool IsShared, DateTimeOffset CreatedAt);
    public sealed record Response(IReadOnlyList<ChartSummary> Items, int TotalCount);
}
