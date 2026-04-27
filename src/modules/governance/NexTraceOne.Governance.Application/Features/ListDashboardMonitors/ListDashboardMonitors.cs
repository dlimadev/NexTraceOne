using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ListDashboardMonitors;

public static class ListDashboardMonitors
{
    public sealed record Query(Guid DashboardId, string TenantId) : IQuery<Response>;

    public sealed record MonitorDto(
        Guid MonitorId,
        string WidgetId,
        string Name,
        string NqlQuery,
        string ConditionField,
        string ConditionOperator,
        decimal ConditionThreshold,
        int EvaluationWindowMinutes,
        string Severity,
        string Status,
        DateTimeOffset? LastFiredAt,
        int FiredCount);

    public sealed record Response(IReadOnlyList<MonitorDto> Monitors, int Count);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(IDashboardMonitorRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var monitors = await repository.ListByDashboardAsync(request.DashboardId, request.TenantId, cancellationToken);
            var dtos = monitors.Select(m => new MonitorDto(
                m.Id.Value, m.WidgetId, m.Name, m.NqlQuery, m.ConditionField,
                m.ConditionOperator.ToString(), m.ConditionThreshold, m.EvaluationWindowMinutes,
                m.Severity.ToString(), m.Status.ToString(), m.LastFiredAt, m.FiredCount)).ToList();
            return Result.Success(new Response(dtos, dtos.Count));
        }
    }
}
