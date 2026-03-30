using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseCalendar;

/// <summary>
/// Feature: GetReleaseCalendar — agrega releases e freeze windows numa janela temporal
/// para visualização em calendário/timeline de mudanças.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetReleaseCalendar
{
    /// <summary>Query para obter dados do calendário de releases.</summary>
    public sealed record Query(
        DateTimeOffset From,
        DateTimeOffset To,
        string? Environment) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.To).GreaterThan(x => x.From)
                .WithMessage("End date must be after start date.");
        }
    }

    /// <summary>Handler que agrega releases e freeze windows para o calendário.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IFreezeWindowRepository freezeRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releasesTask = releaseRepository.ListInRangeAsync(
                request.From, request.To, request.Environment, cancellationToken);

            var freezesTask = freezeRepository.ListInRangeAsync(
                request.From, request.To, request.Environment, true, cancellationToken);

            await Task.WhenAll(releasesTask, freezesTask);

            var releases = releasesTask.Result;
            var freezes = freezesTask.Result;

            var releaseDtos = releases.Select(r => new CalendarReleaseDto(
                r.Id.Value,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.Status.ToString(),
                r.ChangeType.ToString(),
                r.ConfidenceStatus.ToString(),
                r.ChangeScore,
                r.ChangeLevel,
                r.TeamName,
                r.CreatedAt)).ToList();

            var freezeDtos = freezes.Select(f => new CalendarFreezeDto(
                f.Id.Value,
                f.Name,
                f.Reason,
                f.Scope.ToString(),
                f.ScopeValue,
                f.StartsAt,
                f.EndsAt,
                f.IsActive)).ToList();

            // Compute daily summary for sparkline/heatmap
            var dailySummary = releases
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new DailySummaryDto(
                    DateOnly.FromDateTime(g.Key),
                    g.Count(),
                    g.Count(r => r.ConfidenceStatus == ConfidenceStatus.SuspectedRegression
                        || r.ConfidenceStatus == ConfidenceStatus.CorrelatedWithIncident),
                    g.Average(r => (double)r.ChangeScore)))
                .OrderBy(d => d.Date)
                .ToList();

            return new Response(releaseDtos, freezeDtos, dailySummary);
        }
    }

    /// <summary>DTO de release para o calendário.</summary>
    public sealed record CalendarReleaseDto(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        string ChangeType,
        string ConfidenceStatus,
        decimal ChangeScore,
        ChangeLevel ChangeLevel,
        string? TeamName,
        DateTimeOffset CreatedAt);

    /// <summary>DTO de janela de freeze para o calendário.</summary>
    public sealed record CalendarFreezeDto(
        Guid FreezeWindowId,
        string Name,
        string Reason,
        string Scope,
        string? ScopeValue,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        bool IsActive);

    /// <summary>Resumo diário para heatmap/sparkline.</summary>
    public sealed record DailySummaryDto(
        DateOnly Date,
        int TotalReleases,
        int HighRiskReleases,
        double AverageScore);

    /// <summary>Resposta do calendário de releases.</summary>
    public sealed record Response(
        IReadOnlyList<CalendarReleaseDto> Releases,
        IReadOnlyList<CalendarFreezeDto> FreezeWindows,
        IReadOnlyList<DailySummaryDto> DailySummary);
}
