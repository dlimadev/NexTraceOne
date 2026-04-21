using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleaseWindows;

/// <summary>
/// Feature: ListReleaseWindows — lista as janelas do Release Calendar de um tenant.
/// Complementa o GetReleaseCalendar existente (que agrega releases + freeze windows).
/// Este handler é específico para gestão das entradas ReleaseCalendarEntry.
/// Suporta filtros por estado, tipo e intervalo temporal.
/// Wave F.1 — Release Calendar.
/// </summary>
public static class ListReleaseWindows
{
    public sealed record Query(
        string TenantId,
        ReleaseWindowStatus? Status = null,
        ReleaseWindowType? WindowType = null,
        DateTimeOffset? From = null,
        DateTimeOffset? To = null) : IQuery<Response>;

    public sealed record WindowDto(
        Guid WindowId,
        string Name,
        string? Description,
        ReleaseWindowType WindowType,
        ReleaseWindowStatus Status,
        string? EnvironmentFilter,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string? RecurrenceTag,
        bool BlocksChanges,
        bool IsHotfixOnly);

    public sealed record Response(
        string TenantId,
        IReadOnlyList<WindowDto> Windows,
        int TotalCount);

    public sealed class Handler(
        IReleaseCalendarRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var windows = await repository.ListAsync(
                request.TenantId,
                request.Status,
                request.WindowType,
                request.From,
                request.To,
                cancellationToken);

            var dtos = windows.Select(w => new WindowDto(
                w.Id.Value,
                w.Name,
                w.Description,
                w.WindowType,
                w.Status,
                w.EnvironmentFilter,
                w.StartsAt,
                w.EndsAt,
                w.RecurrenceTag,
                w.BlocksChanges,
                w.IsHotfixOnly)).ToList();

            return Result<Response>.Success(new Response(request.TenantId, dtos, dtos.Count));
        }
    }
}
