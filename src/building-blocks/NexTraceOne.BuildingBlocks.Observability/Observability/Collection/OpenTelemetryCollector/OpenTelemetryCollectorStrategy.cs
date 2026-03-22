using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Collection.OpenTelemetryCollector;

/// <summary>
/// Estratégia de coleta via OpenTelemetry Collector para ambientes Kubernetes.
/// O Collector atua como pipeline de ingestão, normalização e roteamento de telemetria.
///
/// Registrado via DI quando Telemetry:CollectionMode:ActiveMode = "OpenTelemetryCollector".
/// </summary>
public sealed class OpenTelemetryCollectorStrategy : ICollectionModeStrategy
{
    private readonly OpenTelemetryCollectorModeOptions _options;

    public OpenTelemetryCollectorStrategy(IOptions<TelemetryStoreOptions> options)
    {
        _options = options.Value.CollectionMode.OpenTelemetryCollector;
    }

    /// <inheritdoc />
    public string ModeName => "OpenTelemetryCollector";

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return false;

        // Check Collector health via HTTP endpoint.
        // Full implementation will use HttpClient to check /health endpoint.
        return await Task.FromResult(
            !string.IsNullOrWhiteSpace(_options.OtlpGrpcEndpoint));
    }

    /// <inheritdoc />
    public CollectionExportConfig GetExportConfig()
    {
        return new CollectionExportConfig
        {
            OtlpEndpoint = _options.OtlpGrpcEndpoint,
            Protocol = "grpc",
            UsesCollectorProxy = true
        };
    }
}
