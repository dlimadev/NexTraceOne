using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.DeprecateDashboard;

/// <summary>
/// Feature: DeprecateDashboard — marca um dashboard como deprecado com nota e substituto opcional.
/// V3.6 — Deprecation Lifecycle.
/// </summary>
public static class DeprecateDashboard
{
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string UserId,
        string? DeprecationNote = null,
        Guid? SuccessorDashboardId = null) : ICommand<Response>;

    public sealed record Response(
        Guid DashboardId,
        DashboardLifecycleStatus LifecycleStatus,
        DateTimeOffset? DeprecatedAt,
        string? DeprecationNote,
        Guid? SuccessorDashboardId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.DeprecationNote).MaximumLength(500).When(x => x.DeprecationNote is not null);
        }
    }

    public sealed class Handler(
        ICustomDashboardRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var dashboard = await repository.GetByIdAsync(
                new CustomDashboardId(request.DashboardId), cancellationToken);

            if (dashboard is null)
                return Error.NotFound(
                    "Dashboard.NotFound",
                    "Dashboard with ID '{0}' was not found.",
                    request.DashboardId);

            if (dashboard.LifecycleStatus == DashboardLifecycleStatus.Archived)
                return Error.Business(
                    "Dashboard.CannotDeprecateArchived",
                    "Cannot deprecate an archived dashboard.");

            dashboard.Deprecate(
                userId: request.UserId,
                note: request.DeprecationNote,
                successorId: request.SuccessorDashboardId,
                now: clock.UtcNow);

            await repository.UpdateAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                dashboard.Id.Value,
                dashboard.LifecycleStatus,
                dashboard.DeprecatedAt,
                dashboard.DeprecationNote,
                dashboard.SuccessorDashboardId));
        }
    }
}
