using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IsChangeWindowOpen;

/// <summary>
/// Feature: IsChangeWindowOpen — verifica se uma mudança pode ser promovida
/// com base nas janelas activas do Release Calendar.
/// Semântica: uma janela de Freeze ou Maintenance activa BLOQUEIA a mudança.
/// Uma janela Scheduled ou HotfixAllowed PERMITE.
/// Ausência de janelas PERMITE (open by default).
/// Wave F.1 — Release Calendar.
/// </summary>
public static class IsChangeWindowOpen
{
    public sealed record Query(
        string TenantId,
        string Environment,
        DateTimeOffset? AtMoment = null) : IQuery<Response>;

    public sealed record BlockingWindowDto(
        Guid WindowId,
        string Name,
        ReleaseWindowType WindowType,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);

    public sealed record Response(
        string TenantId,
        string Environment,
        DateTimeOffset EvaluatedAt,
        bool IsOpen,
        string Reason,
        IReadOnlyList<BlockingWindowDto> BlockingWindows);

    public sealed class Handler(
        IReleaseCalendarRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.Environment);

            var moment = request.AtMoment ?? clock.UtcNow;

            var activeWindows = await repository.ListActiveAtAsync(
                request.TenantId,
                moment,
                request.Environment,
                cancellationToken);

            var blockingWindows = activeWindows
                .Where(w => w.BlocksChanges)
                .Select(w => new BlockingWindowDto(
                    w.Id.Value,
                    w.Name,
                    w.WindowType,
                    w.StartsAt,
                    w.EndsAt))
                .ToList();

            bool isOpen;
            string reason;

            if (blockingWindows.Count > 0)
            {
                isOpen = false;
                var kinds = string.Join(", ", blockingWindows.Select(w => w.WindowType.ToString()).Distinct());
                reason = $"Change is blocked by {blockingWindows.Count} active window(s): {kinds}.";
            }
            else
            {
                isOpen = true;
                reason = activeWindows.Count > 0
                    ? $"{activeWindows.Count} scheduled window(s) active — changes are allowed."
                    : "No release windows active — changes are permitted by default.";
            }

            return Result<Response>.Success(new Response(
                request.TenantId,
                request.Environment,
                moment,
                isOpen,
                reason,
                blockingWindows));
        }
    }
}
