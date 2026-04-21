using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CloseReleaseWindow;

/// <summary>
/// Feature: CloseReleaseWindow — encerra ou cancela uma janela do Release Calendar.
/// Encerrar: janela iniciada, truncada manualmente.
/// Cancelar: janela ainda não iniciada, descartada.
/// Wave F.1 — Release Calendar.
/// </summary>
public static class CloseReleaseWindow
{
    public enum CloseAction { Close, Cancel }

    public sealed record Command(
        Guid WindowId,
        string TenantId,
        string UserId,
        CloseAction Action) : ICommand<Response>;

    public sealed record Response(
        Guid WindowId,
        ReleaseWindowStatus Status,
        DateTimeOffset? ClosedAt);

    public sealed class Handler(
        IReleaseCalendarRepository repository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.UserId);

            var id = ReleaseCalendarEntryId.From(request.WindowId);
            var entry = await repository.GetByIdAsync(id, cancellationToken);

            if (entry is null || entry.TenantId != request.TenantId)
                return Error.NotFound("release_calendar.not_found", "Release window not found.");

            var now = clock.UtcNow;

            var result = request.Action == CloseAction.Cancel
                ? entry.Cancel(request.UserId, now)
                : entry.Close(request.UserId, now);

            if (!result.IsSuccess)
                return result.Error;

            repository.Update(entry);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                entry.Id.Value,
                entry.Status,
                entry.ClosedAt));
        }
    }
}
