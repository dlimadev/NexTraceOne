using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListScheduledReports;

/// <summary>Feature: ListScheduledReports — lista os relatórios programados do utilizador no tenant.</summary>
public static class ListScheduledReports
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(
        IScheduledReportRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var reports = await repository.ListByTenantAsync(
                currentTenant.Id.ToString(),
                currentUser.Id,
                cancellationToken);

            var items = reports
                .Select(r => new ReportSummary(
                    r.Id.Value,
                    r.Name,
                    r.ReportType,
                    r.Schedule,
                    r.Format,
                    r.RecipientsJson,
                    r.IsEnabled,
                    r.LastSentAt,
                    r.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record ReportSummary(
        Guid ReportId,
        string Name,
        string ReportType,
        string Schedule,
        string Format,
        string RecipientsJson,
        bool IsEnabled,
        DateTimeOffset? LastSentAt,
        DateTimeOffset CreatedAt);

    public sealed record Response(IReadOnlyList<ReportSummary> Items, int TotalCount);
}
