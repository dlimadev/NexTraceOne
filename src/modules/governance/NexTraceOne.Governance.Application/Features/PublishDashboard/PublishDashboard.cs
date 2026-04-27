using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.PublishDashboard;

/// <summary>
/// Feature: PublishDashboard — transiciona um dashboard de Draft para Published.
/// V3.6 — Deprecation Lifecycle.
/// </summary>
public static class PublishDashboard
{
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string UserId) : ICommand<Response>;

    public sealed record Response(
        Guid DashboardId,
        DashboardLifecycleStatus LifecycleStatus);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
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

            dashboard.Publish(clock.UtcNow);
            await repository.UpdateAsync(dashboard, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(dashboard.Id.Value, dashboard.LifecycleStatus));
        }
    }
}
