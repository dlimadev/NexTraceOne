using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ListScheduledDashboardReports;

/// <summary>
/// Feature: ListScheduledDashboardReports — lista todos os agendamentos de relatório do tenant.
/// V3.6 — Governance, Reports &amp; Embedding.
/// </summary>
public static class ListScheduledDashboardReports
{
    public sealed record Query(string TenantId) : IQuery<Response>;

    public sealed record ScheduledReportDto(
        Guid Id,
        Guid DashboardId,
        string DashboardName,
        string CronExpression,
        string Format,
        IReadOnlyList<string> Recipients,
        int RetentionDays,
        bool IsActive,
        DateTimeOffset? LastRunAt,
        DateTimeOffset? NextRunAt,
        int SuccessCount,
        int FailureCount);

    public sealed record Response(IReadOnlyList<ScheduledReportDto> Items);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.TenantId).NotEmpty();
    }

    public sealed class Handler(
        IScheduledDashboardReportRepository reportRepository,
        ICustomDashboardRepository dashboardRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var reports = await reportRepository.ListByTenantAsync(request.TenantId, cancellationToken);

            var dashboardIds = reports.Select(r => r.DashboardId).Distinct().ToList();
            var dashboards = await dashboardRepository.ListAsync(null, cancellationToken);
            var dashboardMap = dashboards
                .Where(d => dashboardIds.Contains(d.Id.Value))
                .ToDictionary(d => d.Id.Value, d => d.Name);

            var dtos = reports.Select(r =>
            {
                var recipients = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(r.RecipientsJson) ?? [];

                dashboardMap.TryGetValue(r.DashboardId, out var dashboardName);

                return new ScheduledReportDto(
                    r.Id.Value,
                    r.DashboardId,
                    dashboardName ?? r.DashboardId.ToString(),
                    r.CronExpression,
                    r.Format.ToUpperInvariant(),
                    recipients,
                    r.RetentionDays,
                    r.IsActive,
                    r.LastRunAt,
                    r.NextRunAt,
                    r.SuccessCount,
                    r.FailureCount);
            }).ToList();

            return Result<Response>.Success(new Response(dtos));
        }
    }
}
