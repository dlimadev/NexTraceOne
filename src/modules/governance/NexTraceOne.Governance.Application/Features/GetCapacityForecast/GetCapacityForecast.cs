using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetCapacityForecast;

/// <summary>
/// Feature: GetCapacityForecast — previsão de capacidade de recursos da plataforma.
/// Retorna previsões sintéticas (CPU, Memory, Disk, DatabaseConnections) com SimulatedNote.
/// </summary>
public static class GetCapacityForecast
{
    /// <summary>Query sem parâmetros — retorna previsão de capacidade.</summary>
    public sealed record Query() : IQuery<CapacityForecastResponse>;

    /// <summary>Handler que retorna previsões de capacidade sintéticas.</summary>
    public sealed class Handler : IQueryHandler<Query, CapacityForecastResponse>
    {
        public Task<Result<CapacityForecastResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var forecasts = new List<CapacityForecastDto>
            {
                new("CPU", CurrentUsagePct: 42, ForecastedUsagePct: 58, ForecastedAt: DateTimeOffset.UtcNow.AddDays(30), Trend: "increasing", Risk: "Low"),
                new("Memory", CurrentUsagePct: 61, ForecastedUsagePct: 75, ForecastedAt: DateTimeOffset.UtcNow.AddDays(30), Trend: "increasing", Risk: "Medium"),
                new("Disk", CurrentUsagePct: 34, ForecastedUsagePct: 41, ForecastedAt: DateTimeOffset.UtcNow.AddDays(30), Trend: "stable", Risk: "Low"),
                new("DatabaseConnections", CurrentUsagePct: 22, ForecastedUsagePct: 30, ForecastedAt: DateTimeOffset.UtcNow.AddDays(30), Trend: "stable", Risk: "Low")
            };

            var response = new CapacityForecastResponse(
                Forecasts: forecasts,
                AnalysisWeeks: 4,
                NextReviewDate: DateTimeOffset.UtcNow.AddDays(30),
                GeneratedAt: DateTimeOffset.UtcNow,
                SimulatedNote: "Capacity forecasts are synthetic. Real forecasting requires historical metrics from the observability pipeline.");

            return Task.FromResult(Result<CapacityForecastResponse>.Success(response));
        }
    }

    /// <summary>Resposta com previsões de capacidade de recursos.</summary>
    public sealed record CapacityForecastResponse(
        IReadOnlyList<CapacityForecastDto> Forecasts,
        int AnalysisWeeks,
        DateTimeOffset NextReviewDate,
        DateTimeOffset GeneratedAt,
        string SimulatedNote);

    /// <summary>Previsão de capacidade para um recurso específico.</summary>
    public sealed record CapacityForecastDto(
        string Resource,
        double CurrentUsagePct,
        double ForecastedUsagePct,
        DateTimeOffset ForecastedAt,
        string Trend,
        string Risk);
}
