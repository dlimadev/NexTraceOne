using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.RecordDashboardUsage;

/// <summary>
/// Feature: RecordDashboardUsage — regista um evento de acesso/uso a um dashboard.
/// V3.6 — Usage Analytics.
/// </summary>
public static class RecordDashboardUsage
{
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string? UserId,
        string? Persona,
        string EventType,
        int? DurationSeconds = null) : ICommand<Unit>;

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> ValidTypes = ["view", "export", "embed", "share", "snapshot"];

        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.EventType)
                .Must(t => ValidTypes.Contains(t))
                .WithMessage("EventType must be one of: view, export, embed, share, snapshot.");
        }
    }

    public sealed class Handler(
        IDashboardUsageRepository usageRepository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var evt = DashboardUsageEvent.Record(
                dashboardId: request.DashboardId,
                tenantId: request.TenantId,
                userId: request.UserId,
                persona: request.Persona,
                eventType: request.EventType,
                now: clock.UtcNow,
                durationSeconds: request.DurationSeconds);

            await usageRepository.AddAsync(evt, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
