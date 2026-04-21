using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterReleaseWindow;

/// <summary>
/// Feature: RegisterReleaseWindow — regista uma nova janela no Release Calendar.
/// Suporta janelas de deployment planeado, freeze, hotfix e manutenção.
/// Wave F.1 — Release Calendar.
/// </summary>
public static class RegisterReleaseWindow
{
    public sealed record Command(
        string TenantId,
        string Name,
        ReleaseWindowType WindowType,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string? EnvironmentFilter = null,
        string? Description = null,
        string? RecurrenceTag = null) : ICommand<Response>;

    public sealed record Response(
        Guid WindowId,
        string TenantId,
        string Name,
        ReleaseWindowType WindowType,
        ReleaseWindowStatus Status,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string? EnvironmentFilter,
        bool BlocksChanges);

    public sealed class Handler(
        IReleaseCalendarRepository repository,
        IChangeIntelligenceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.Name);

            var result = ReleaseCalendarEntry.Register(
                request.TenantId,
                request.Name,
                request.WindowType,
                request.StartsAt,
                request.EndsAt,
                request.EnvironmentFilter,
                request.Description,
                request.RecurrenceTag);

            if (!result.IsSuccess)
                return result.Error;

            var entry = result.Value;
            repository.Add(entry);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                entry.Id.Value,
                entry.TenantId,
                entry.Name,
                entry.WindowType,
                entry.Status,
                entry.StartsAt,
                entry.EndsAt,
                entry.EnvironmentFilter,
                entry.BlocksChanges));
        }
    }
}
