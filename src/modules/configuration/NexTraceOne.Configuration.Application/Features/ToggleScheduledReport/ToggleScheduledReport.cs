using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.ToggleScheduledReport;

/// <summary>Feature: ToggleScheduledReport — activa ou desactiva um relatório programado.</summary>
public static class ToggleScheduledReport
{
    public sealed record Command(Guid ReportId, bool Enabled) : ICommand<bool>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReportId).NotEmpty();
        }
    }

    public sealed class Handler(
        IScheduledReportRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var report = await repository.GetByIdAsync(
                new ScheduledReportId(request.ReportId),
                currentTenant.Id.ToString(),
                cancellationToken);

            if (report is null)
                return Error.NotFound("ScheduledReport.NotFound", $"Scheduled report '{request.ReportId}' not found.");

            if (report.UserId != currentUser.Id)
                return Error.Forbidden("ScheduledReport.Forbidden", "You do not own this scheduled report.");

            report.Toggle(request.Enabled, clock.UtcNow);
            await repository.UpdateAsync(report, cancellationToken);
            return true;
        }
    }
}
