using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.CreateDashboardMonitor;

public static class CreateDashboardMonitor
{
    public sealed record Command(
        Guid DashboardId,
        string WidgetId,
        string TenantId,
        string UserId,
        string Name,
        string NqlQuery,
        string ConditionField,
        string ConditionOperator,
        decimal ConditionThreshold,
        int EvaluationWindowMinutes,
        string Severity,
        IReadOnlyList<string> NotificationChannels) : ICommand<Response>;

    public sealed record Response(Guid MonitorId, string Name, MonitorSeverity Severity, MonitorStatus Status);

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> Operators = ["GreaterThan", "LessThan", "Equals", "NotEquals"];
        private static readonly HashSet<string> Severities = ["Info", "Warning", "Critical"];

        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.WidgetId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.NqlQuery).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.ConditionField).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ConditionOperator).Must(o => Operators.Contains(o));
            RuleFor(x => x.EvaluationWindowMinutes).InclusiveBetween(1, 1440);
            RuleFor(x => x.Severity).Must(s => Severities.Contains(s));
        }
    }

    public sealed class Handler(
        IDashboardMonitorRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var op = Enum.Parse<MonitorConditionOperator>(request.ConditionOperator);
            var sev = Enum.Parse<MonitorSeverity>(request.Severity);

            var monitor = DashboardMonitorDefinition.Create(
                request.DashboardId, request.WidgetId, request.TenantId, request.UserId,
                request.Name, request.NqlQuery, request.ConditionField, op,
                request.ConditionThreshold, request.EvaluationWindowMinutes, sev,
                request.NotificationChannels, clock.UtcNow);

            await repository.AddAsync(monitor, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(monitor.Id.Value, monitor.Name, monitor.Severity, monitor.Status));
        }
    }
}
