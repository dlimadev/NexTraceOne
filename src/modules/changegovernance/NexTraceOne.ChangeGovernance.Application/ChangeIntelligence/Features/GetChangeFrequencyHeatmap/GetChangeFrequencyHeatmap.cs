using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeFrequencyHeatmap;

/// <summary>
/// Feature: GetChangeFrequencyHeatmap — heatmap de deployments por dia da semana × hora do dia.
///
/// Analisa a distribuição temporal dos deploys no período solicitado, agrupando
/// por (DayOfWeek, HourOfDay) para revelar padrões de cadência operacional:
/// - picos de deploy (e.g. sexta-feira à tarde = risco elevado)
/// - janelas preferenciais de deploy
/// - distribuição por ambiente
///
/// A célula mais quente (MaxCount) é destacada para fácil identificação do pico.
/// Útil para Platform Admin, Tech Lead e Risk personas.
///
/// Wave K.3a — Change Frequency Heatmap (ChangeGovernance).
/// </summary>
public static class GetChangeFrequencyHeatmap
{
    public sealed record Query(
        int Days = 30,
        string? ServiceName = null,
        string? Environment = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Days).InclusiveBetween(7, 90);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.Environment).MaximumLength(100).When(x => x.Environment is not null);
        }
    }

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);

            var releases = await releaseRepository.ListInRangeAsync(since, now, request.Environment, currentTenant.Id, cancellationToken);

            var filtered = request.ServiceName is not null
                ? releases.Where(r => r.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase)).ToList()
                : releases.ToList();

            // Agrupar por (DayOfWeek, HourOfDay) — hora em UTC
            var cells = new Dictionary<(int DayOfWeek, int Hour), int>();
            foreach (var release in filtered)
            {
                var dt = release.CreatedAt.UtcDateTime;
                var key = ((int)dt.DayOfWeek, dt.Hour);
                cells.TryGetValue(key, out var count);
                cells[key] = count + 1;
            }

            var heatmap = cells
                .Select(kv => new HeatmapCell(
                    DayOfWeek: kv.Key.DayOfWeek,
                    HourOfDay: kv.Key.Hour,
                    Count: kv.Value))
                .OrderBy(c => c.DayOfWeek)
                .ThenBy(c => c.HourOfDay)
                .ToList();

            var maxCount = heatmap.Count > 0 ? heatmap.Max(c => c.Count) : 0;
            var peakCell = heatmap.FirstOrDefault(c => c.Count == maxCount);

            // Distribuição por dia da semana (agregado ao longo do dia)
            var byDayOfWeek = Enumerable.Range(0, 7)
                .Select(d => new DayDistribution(
                    DayOfWeek: d,
                    DayName: ((DayOfWeek)d).ToString(),
                    Count: heatmap.Where(c => c.DayOfWeek == d).Sum(c => c.Count)))
                .ToList();

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                ServiceFilter: request.ServiceName,
                EnvironmentFilter: request.Environment,
                TotalDeploys: filtered.Count,
                Heatmap: heatmap,
                MaxCellCount: maxCount,
                PeakDayOfWeek: peakCell?.DayOfWeek,
                PeakHourOfDay: peakCell?.HourOfDay,
                ByDayOfWeek: byDayOfWeek));
        }
    }

    public sealed record HeatmapCell(int DayOfWeek, int HourOfDay, int Count);
    public sealed record DayDistribution(int DayOfWeek, string DayName, int Count);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string? ServiceFilter,
        string? EnvironmentFilter,
        int TotalDeploys,
        IReadOnlyList<HeatmapCell> Heatmap,
        int MaxCellCount,
        int? PeakDayOfWeek,
        int? PeakHourOfDay,
        IReadOnlyList<DayDistribution> ByDayOfWeek);
}
