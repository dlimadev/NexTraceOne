using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexTraceOne.BuildingBlocks.Observability.Logging;

/// <summary>
/// Configuração centralizada do Serilog para toda a plataforma.
/// Inclui: enrichers (Environment, MachineName, ThreadId, TraceId/SpanId/TenantId),
/// sinks (Console, File, OpenTelemetry/OTLP), destructuring de objetos de domínio.
/// 
/// A plataforma envia logs via OpenTelemetry (OTLP) para o Collector,
/// que roteia para o provider de observabilidade configurado (ClickHouse ou Elastic).
/// O enricher NexTraceLogEnricher garante correlação automática logs ↔ traces.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>Configura o Serilog como logger estruturado padrão da plataforma.</summary>
    public static IHostBuilder ConfigureNexTraceSerilog(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        return hostBuilder.UseSerilog((_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.With<NexTraceLogEnricher>()
                .WriteTo.Console();

            var filePath = configuration["Observability:Serilog:FilePath"];
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                loggerConfiguration.WriteTo.File(filePath, rollingInterval: RollingInterval.Day);
            }

            // OTLP export: envia logs estruturados ao OpenTelemetry Collector
            // para que sejam armazenados no mesmo provider de observabilidade
            // (Elasticsearch ou ClickHouse) junto com traces e métricas.
            var otlpEndpoint = System.Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                ?? configuration["Telemetry:Collector:OtlpGrpcEndpoint"]
                ?? configuration["OpenTelemetry:Endpoint"];

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                loggerConfiguration.WriteTo.OpenTelemetry(opts =>
                {
                    opts.Endpoint = otlpEndpoint;
                    opts.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    opts.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = configuration["OpenTelemetry:ServiceName"] ?? "NexTraceOne",
                        ["service.namespace"] = "nextraceone"
                    };
                });
            }
        });
    }
}
