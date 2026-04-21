using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Features.GetFreshnessReport;

public static class GetFreshnessReport
{
    public sealed record Query(
        DocumentCategory? Category = null,
        int MaxResults = 50) : IQuery<Response>;

    public sealed class Handler(
        IKnowledgeDocumentRepository repo,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (docs, _) = await repo.ListAsync(request.Category, null, 1, Math.Min(request.MaxResults, 200), cancellationToken);

            var now = clock.UtcNow;
            var items = docs.Select(d =>
            {
                d.ComputeFreshnessScore(now);
                return new FreshnessItemDto(
                    DocumentId: d.Id.Value.ToString(),
                    Title: d.Title,
                    Category: d.Category.ToString(),
                    FreshnessScore: d.FreshnessScore,
                    LastReviewedAt: d.LastReviewedAt,
                    Status: d.FreshnessScore >= 80 ? "Fresh" : d.FreshnessScore >= 50 ? "Aging" : "Stale");
            })
            .OrderBy(i => i.FreshnessScore)
            .ToList();

            var staleCount = items.Count(i => i.Status == "Stale");
            var agingCount = items.Count(i => i.Status == "Aging");
            var freshCount = items.Count(i => i.Status == "Fresh");

            return Result<Response>.Success(new Response(
                TotalDocuments: items.Count,
                StaleCount: staleCount,
                AgingCount: agingCount,
                FreshCount: freshCount,
                AverageFreshnessScore: items.Count > 0 ? (int)Math.Round(items.Average(i => i.FreshnessScore)) : 100,
                Items: items));
        }
    }

    public sealed record Response(
        int TotalDocuments, int StaleCount, int AgingCount, int FreshCount,
        int AverageFreshnessScore, IReadOnlyList<FreshnessItemDto> Items);

    public sealed record FreshnessItemDto(
        string DocumentId, string Title, string Category,
        int FreshnessScore, DateTimeOffset? LastReviewedAt, string Status);
}
