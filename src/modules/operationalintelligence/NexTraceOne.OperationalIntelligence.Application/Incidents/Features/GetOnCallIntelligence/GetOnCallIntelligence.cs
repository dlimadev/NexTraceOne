using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetOnCallIntelligence;

/// <summary>
/// Feature: GetOnCallIntelligence — fornece inteligência de on-call para uma equipa.
/// Analisa distribuição de incidentes, indicadores de fadiga, hora de pico e recomendações.
/// Computação pura — usa dados do IIncidentStore sem persistência adicional.
/// </summary>
public static class GetOnCallIntelligence
{
    public sealed record Query(
        string TeamId,
        int PeriodDays = 30) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PeriodDays).InclusiveBetween(1, 365);
        }
    }

    public sealed class Handler(IIncidentStore store, IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var since = clock.UtcNow.AddDays(-request.PeriodDays);
            var allIncidents = store.GetIncidentListItems()
                .Where(i => i.CreatedAt >= since)
                .Where(i => i.OwnerTeam.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var total = allIncidents.Count;
            var avgPerDay = total > 0 ? Math.Round((decimal)total / request.PeriodDays, 2) : 0m;

            // Computar hora e dia de pico a partir dos timestamps reais dos incidentes
            int peakHour;
            string peakDayOfWeek;
            decimal nightCallsPercent;
            decimal weekendCallsPercent;

            if (total > 0)
            {
                // Hora de pico — a hora UTC com mais incidentes
                peakHour = allIncidents
                    .GroupBy(i => i.CreatedAt.UtcDateTime.Hour)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;

                // Dia da semana com mais incidentes
                var days = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
                peakDayOfWeek = days[(int)allIncidents
                    .GroupBy(i => i.CreatedAt.UtcDateTime.DayOfWeek)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key];

                // Incidentes nocturnos (22h-7h UTC)
                var nightIncidents = allIncidents.Count(i =>
                {
                    var hour = i.CreatedAt.UtcDateTime.Hour;
                    return hour >= 22 || hour < 7;
                });
                nightCallsPercent = Math.Round(100m * nightIncidents / total, 1);

                // Incidentes ao fim-de-semana (sábado e domingo)
                var weekendIncidents = allIncidents.Count(i =>
                    i.CreatedAt.UtcDateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
                weekendCallsPercent = Math.Round(100m * weekendIncidents / total, 1);
            }
            else
            {
                peakHour = 0;
                peakDayOfWeek = "N/A";
                nightCallsPercent = 0m;
                weekendCallsPercent = 0m;
            }

            // Dias consecutivos com pelo menos 1 incidente
            var consecutiveDays = 0;
            if (total > 0)
            {
                var incidentDates = allIncidents
                    .Select(i => i.CreatedAt.UtcDateTime.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                var streak = 1;
                var maxStreak = 1;
                for (var idx = 1; idx < incidentDates.Count; idx++)
                {
                    if ((incidentDates[idx - 1] - incidentDates[idx]).Days == 1)
                    {
                        streak++;
                        maxStreak = Math.Max(maxStreak, streak);
                    }
                    else
                    {
                        streak = 1;
                    }
                }
                consecutiveDays = maxStreak;
            }

            // Tempo médio estimado de resposta — sem dados de acknowledging, retorna 0
            // (requer integração com PagerDuty/OpsGenie para métricas reais de response time)
            var avgResponseMinutes = 0m;

            // Nível de fadiga — computado a partir de indicadores reais
            var fatigueScore = (nightCallsPercent / 60m * 30m) + (weekendCallsPercent / 40m * 20m) +
                               (avgPerDay * 10m) + (consecutiveDays > 5 ? 15m : 0m);
            var fatigueLevel = fatigueScore < 20m ? "Low" : fatigueScore < 40m ? "Medium" : fatigueScore < 60m ? "High" : "Critical";

            // Top serviços afetados — derivados de dados reais
            var topServices = allIncidents
                .GroupBy(i => i.ServiceDisplayName ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new ServiceIncidentCount(g.Key, g.Count()))
                .ToList();

            // Recomendações baseadas nos indicadores reais
            var recommendations = new List<string>();
            if (nightCallsPercent > 35m) recommendations.Add("Consider rotating on-call shifts to reduce night-hour burden.");
            if (weekendCallsPercent > 25m) recommendations.Add("Review weekend deployment windows to reduce off-hours incidents.");
            if (avgPerDay > 1.5m) recommendations.Add("High incident volume detected — investigate recurring root causes.");
            if (consecutiveDays > 5) recommendations.Add("Extended consecutive incident period — consider temporary staffing increase.");
            if (total == 0) recommendations.Add("No incidents recorded in the analysis period.");
            if (recommendations.Count == 0) recommendations.Add("On-call load is within acceptable parameters.");

            return Task.FromResult(Result<Response>.Success(new Response(
                request.TeamId,
                request.PeriodDays,
                total,
                avgPerDay,
                peakHour,
                peakDayOfWeek,
                topServices,
                new FatigueIndicators(nightCallsPercent, weekendCallsPercent, avgResponseMinutes, consecutiveDays),
                fatigueLevel,
                recommendations,
                clock.UtcNow)));
        }
    }

    public sealed record ServiceIncidentCount(string ServiceName, int Count);

    public sealed record FatigueIndicators(
        decimal NightCallsPercent,
        decimal WeekendCallsPercent,
        decimal AvgResponseMinutes,
        int ConsecutiveIncidentDays);

    public sealed record Response(
        string TeamId,
        int PeriodDays,
        int TotalIncidents,
        decimal AvgIncidentsPerDay,
        int PeakHour,
        string PeakDayOfWeek,
        IReadOnlyList<ServiceIncidentCount> TopAffectedServices,
        FatigueIndicators FatigueIndicators,
        string FatigueLevel,
        IReadOnlyList<string> Recommendations,
        DateTimeOffset ComputedAt);
}
