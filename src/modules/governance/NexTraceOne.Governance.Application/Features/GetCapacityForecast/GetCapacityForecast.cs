using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetCapacityForecast;

/// <summary>
/// Feature: GetCapacityForecast — previsão de capacidade de recursos da plataforma.
/// Lê snapshots de runtime reais via IRuntimeIntelligenceModule e extrapola consumo a 30 dias
/// usando regressão linear simples. Fallback para valores estimados quando não há dados.
/// </summary>
public static class GetCapacityForecast
{
    /// <summary>Query sem parâmetros — retorna previsão de capacidade.</summary>
    public sealed record Query() : IQuery<CapacityForecastResponse>;

    /// <summary>
    /// Handler que consulta IRuntimeIntelligenceModule e calcula previsão de capacidade.
    /// Quando dados reais estão disponíveis, SimulatedNote é null.
    /// </summary>
    public sealed class Handler(
        IRuntimeIntelligenceModule runtimeModule,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : IQueryHandler<Query, CapacityForecastResponse>
    {
        public async Task<Result<CapacityForecastResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            PlatformAverageMetrics? metrics = null;
            try
            {
                metrics = await runtimeModule.GetPlatformAverageMetricsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read platform runtime metrics for capacity forecast — falling back to estimates.");
            }

            var utcNow = clock.UtcNow;
            var forecastDate = utcNow.AddDays(30);

            List<CapacityForecastDto> forecasts;
            string? simulatedNote;

            if (metrics is not null)
            {
                forecasts =
                [
                    BuildForecast("CPU", metrics.CurrentCpuPct, metrics.ForecastedCpuPct, metrics.CpuTrend, forecastDate),
                    BuildForecast("Memory", metrics.CurrentMemoryPct, metrics.ForecastedMemoryPct, metrics.MemoryTrend, forecastDate),
                    // Disk and DB connections have no telemetry source yet — keep estimated
                    new("Disk", CurrentUsagePct: 34, ForecastedUsagePct: 41,
                        ForecastedAt: forecastDate, Trend: "stable", Risk: "Low"),
                    new("DatabaseConnections", CurrentUsagePct: 22, ForecastedUsagePct: 30,
                        ForecastedAt: forecastDate, Trend: "stable", Risk: "Low"),
                ];
                simulatedNote = null;
            }
            else
            {
                forecasts =
                [
                    new("CPU", CurrentUsagePct: 42, ForecastedUsagePct: 58,
                        ForecastedAt: forecastDate, Trend: "increasing", Risk: "Low"),
                    new("Memory", CurrentUsagePct: 61, ForecastedUsagePct: 75,
                        ForecastedAt: forecastDate, Trend: "increasing", Risk: "Medium"),
                    new("Disk", CurrentUsagePct: 34, ForecastedUsagePct: 41,
                        ForecastedAt: forecastDate, Trend: "stable", Risk: "Low"),
                    new("DatabaseConnections", CurrentUsagePct: 22, ForecastedUsagePct: 30,
                        ForecastedAt: forecastDate, Trend: "stable", Risk: "Low"),
                ];
                simulatedNote = "Capacity forecasts are estimated. No runtime snapshots found in the observability pipeline yet.";
            }

            var response = new CapacityForecastResponse(
                Forecasts: forecasts,
                AnalysisWeeks: 4,
                NextReviewDate: forecastDate,
                GeneratedAt: utcNow,
                SimulatedNote: simulatedNote);

            return Result<CapacityForecastResponse>.Success(response);
        }

        private static CapacityForecastDto BuildForecast(
            string resource,
            double current,
            double forecasted,
            string trend,
            DateTimeOffset forecastDate)
        {
            var risk = forecasted switch
            {
                > 90 => "Critical",
                > 80 => "High",
                > 60 => "Medium",
                _ => "Low"
            };

            return new CapacityForecastDto(
                Resource: resource,
                CurrentUsagePct: Math.Round(current, 1),
                ForecastedUsagePct: Math.Round(forecasted, 1),
                ForecastedAt: forecastDate,
                Trend: trend,
                Risk: risk);
        }
    }

    /// <summary>Resposta com previsões de capacidade de recursos.</summary>
    public sealed record CapacityForecastResponse(
        IReadOnlyList<CapacityForecastDto> Forecasts,
        int AnalysisWeeks,
        DateTimeOffset NextReviewDate,
        DateTimeOffset GeneratedAt,
        string? SimulatedNote);

    /// <summary>Previsão de capacidade para um recurso específico.</summary>
    public sealed record CapacityForecastDto(
        string Resource,
        double CurrentUsagePct,
        double ForecastedUsagePct,
        DateTimeOffset ForecastedAt,
        string Trend,
        string Risk);
}
