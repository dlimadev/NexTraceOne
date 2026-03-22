using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Collection.ClrProfiler;

/// <summary>
/// Estratégia de coleta via CLR Profiler para ambientes IIS/Windows.
/// Para aplicações .NET hospedadas em IIS com menor intrusão manual.
///
/// O profiler captura sinais relevantes (traces, métricas, logs) sem exigir
/// reescrita da aplicação. Os dados são enviados para o destino configurado,
/// preferencialmente via pipeline OTLP compatível com o restante da arquitetura.
///
/// Registrado via DI quando Telemetry:CollectionMode:ActiveMode = "ClrProfiler".
///
/// NOTA: O CLR Profiler não é um container na stack local. É um mecanismo
/// aplicável aos ambientes IIS onde a aplicação monitorada é hospedada.
/// </summary>
public sealed class ClrProfilerStrategy : ICollectionModeStrategy
{
    private readonly ClrProfilerModeOptions _options;

    public ClrProfilerStrategy(IOptions<TelemetryStoreOptions> options)
    {
        _options = options.Value.CollectionMode.ClrProfiler;
    }

    /// <inheritdoc />
    public string ModeName => "ClrProfiler";

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return false;

        // CLR Profiler health depends on the agent being installed on the target host.
        // This check validates configuration is present.
        return await Task.FromResult(
            !string.IsNullOrWhiteSpace(_options.OtlpEndpoint));
    }

    /// <inheritdoc />
    public CollectionExportConfig GetExportConfig()
    {
        var usesCollector = string.Equals(
            _options.ExportTarget, "Collector", StringComparison.OrdinalIgnoreCase);

        return new CollectionExportConfig
        {
            OtlpEndpoint = _options.OtlpEndpoint,
            Protocol = "grpc",
            UsesCollectorProxy = usesCollector
        };
    }
}
