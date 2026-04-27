using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetDashboardUsageAnalytics;

/// <summary>
/// Feature: GetDashboardUsageAnalytics — retorna métricas de uso agregadas por dashboard.
/// V3.6 — Usage Analytics para curadoria editorial.
/// </summary>
public static class GetDashboardUsageAnalytics
{
    public sealed record Query(
        string TenantId,
        Guid? DashboardId = null,
        int WindowDays = 30) : IQuery<Response>;

    public sealed record DashboardUsageSummary(
        Guid DashboardId,
        string DashboardName,
        long TotalViews,
        long UniqueUsers,
        long ExportCount,
        long EmbedCount,
        double AvgDurationSeconds,
        DateTimeOffset? LastViewedAt,
        string TopPersona);

    public sealed record Response(
        IReadOnlyList<DashboardUsageSummary> Items,
        DateTimeOffset WindowFrom,
        DateTimeOffset WindowTo);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.WindowDays).InclusiveBetween(1, 365);
        }
    }

    public sealed class Handler(IDashboardUsageRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var windowFrom = DateTimeOffset.UtcNow.AddDays(-request.WindowDays);
            var windowTo = DateTimeOffset.UtcNow;

            var summaries = await repository.GetAnalyticsAsync(
                request.TenantId,
                request.DashboardId,
                windowFrom,
                windowTo,
                cancellationToken);

            return Result<Response>.Success(new Response(summaries, windowFrom, windowTo));
        }
    }
}
