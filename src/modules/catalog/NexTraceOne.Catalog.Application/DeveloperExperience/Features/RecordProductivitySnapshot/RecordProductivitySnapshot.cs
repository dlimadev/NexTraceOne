using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.RecordProductivitySnapshot;

/// <summary>
/// Feature: RecordProductivitySnapshot — regista um snapshot de produtividade de equipa.
/// </summary>
public static class RecordProductivitySnapshot
{
    public sealed record Command(
        string TeamId,
        string? ServiceId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int DeploymentCount,
        decimal AverageCycleTimeHours,
        int IncidentCount,
        int ManualStepsCount,
        string? SnapshotSource) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PeriodEnd).Must((cmd, end) => end > cmd.PeriodStart)
                .WithMessage("PeriodEnd must be after PeriodStart.");
            RuleFor(x => x.DeploymentCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AverageCycleTimeHours).GreaterThanOrEqualTo(0m);
            RuleFor(x => x.IncidentCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ManualStepsCount).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class Handler(
        IProductivitySnapshotRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = ProductivitySnapshot.Create(
                request.TeamId,
                request.ServiceId,
                request.PeriodStart,
                request.PeriodEnd,
                request.DeploymentCount,
                request.AverageCycleTimeHours,
                request.IncidentCount,
                request.ManualStepsCount,
                request.SnapshotSource,
                clock.UtcNow);

            if (!result.IsSuccess) return result.Error;

            repository.Add(result.Value!);
            await unitOfWork.CommitAsync(cancellationToken);

            var snap = result.Value!;
            return Result<Response>.Success(new Response(
                snap.Id.Value, snap.TeamId, snap.PeriodStart, snap.PeriodEnd,
                snap.DeploymentCount, snap.RecordedAt));
        }
    }

    public sealed record Response(
        Guid SnapshotId,
        string TeamId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int DeploymentCount,
        DateTimeOffset RecordedAt);
}
