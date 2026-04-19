using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetObservabilityMode;

/// <summary>
/// Feature: GetObservabilityMode — modo de observabilidade da plataforma.
/// Lê de IConfiguration "Platform:Observability:*".
/// Modos: Full, Lite, Minimal.
/// </summary>
public static class GetObservabilityMode
{
    /// <summary>Query sem parâmetros — retorna configuração do modo de observabilidade.</summary>
    public sealed record Query() : IQuery<ObservabilityModeConfigResponse>;

    /// <summary>Comando para atualizar o modo de observabilidade.</summary>
    public sealed record UpdateObservabilityMode(string Mode) : ICommand<ObservabilityModeConfigResponse>;

    /// <summary>Handler de leitura do modo de observabilidade.</summary>
    public sealed class Handler(IConfiguration configuration, IDateTimeProvider clock) : IQueryHandler<Query, ObservabilityModeConfigResponse>
    {
        public Task<Result<ObservabilityModeConfigResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var mode = configuration["Platform:Observability:Mode"] ?? "Lite";
            var esUrl = configuration["Elasticsearch:Url"] ?? configuration["Elasticsearch:Uri"];
            var esConnected = !string.IsNullOrWhiteSpace(esUrl);
            var pgAnalytics = bool.TryParse(configuration["Platform:Observability:PostgresAnalyticsEnabled"], out var pga) && pga;
            var otelCollector = !string.IsNullOrWhiteSpace(configuration["Platform:Observability:OtelCollectorEndpoint"]);

            var tradeOffs = mode switch
            {
                "Full" => new List<string> { "Higher RAM usage", "Full trace correlation", "Elasticsearch required" },
                "Lite" => new List<string> { "Moderate RAM", "Limited trace sampling", "PostgreSQL analytics" },
                _ => new List<string> { "Minimal RAM", "No traces", "Metrics only" }
            };

            var response = new ObservabilityModeConfigResponse(
                CurrentMode: mode,
                ElasticsearchConnected: esConnected,
                Version: esConnected ? "8.x" : null,
                PostgresAnalyticsEnabled: pgAnalytics,
                OtelCollectorConnected: otelCollector,
                AdditionalRamUsageGb: mode == "Full" ? 4.0 : mode == "Lite" ? 1.5 : 0.5,
                TradeOffs: tradeOffs,
                UpdatedAt: clock.UtcNow);

            return Task.FromResult(Result<ObservabilityModeConfigResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização do modo de observabilidade.</summary>
    public sealed class UpdateHandler(IDateTimeProvider clock) : ICommandHandler<UpdateObservabilityMode, ObservabilityModeConfigResponse>
    {
        public Task<Result<ObservabilityModeConfigResponse>> Handle(UpdateObservabilityMode request, CancellationToken cancellationToken)
        {
            var tradeOffs = request.Mode switch
            {
                "Full" => new List<string> { "Higher RAM usage", "Full trace correlation", "Elasticsearch required" },
                "Lite" => new List<string> { "Moderate RAM", "Limited trace sampling", "PostgreSQL analytics" },
                _ => new List<string> { "Minimal RAM", "No traces", "Metrics only" }
            };

            var response = new ObservabilityModeConfigResponse(
                CurrentMode: request.Mode,
                ElasticsearchConnected: false,
                Version: null,
                PostgresAnalyticsEnabled: false,
                OtelCollectorConnected: false,
                AdditionalRamUsageGb: request.Mode == "Full" ? 4.0 : request.Mode == "Lite" ? 1.5 : 0.5,
                TradeOffs: tradeOffs,
                UpdatedAt: clock.UtcNow);

            return Task.FromResult(Result<ObservabilityModeConfigResponse>.Success(response));
        }
    }

    /// <summary>Resposta com configuração do modo de observabilidade.</summary>
    public sealed record ObservabilityModeConfigResponse(
        string CurrentMode,
        bool ElasticsearchConnected,
        string? Version,
        bool PostgresAnalyticsEnabled,
        bool OtelCollectorConnected,
        double AdditionalRamUsageGb,
        IReadOnlyList<string> TradeOffs,
        DateTimeOffset UpdatedAt);
}
