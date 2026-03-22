using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexTraceOne.BuildingBlocks.Observability.Logging;

/// <summary>
/// Configuração centralizada do Serilog para toda a plataforma.
/// Inclui: enrichers (Environment, MachineName, ThreadId),
/// sinks (Console, File, Loki), destructuring de objetos de domínio.
/// 
/// Loki sink: controlado pela secção Observability:Serilog:Loki.
/// Labels recomendadas: application, environment, module.
/// Nunca logar secrets, tokens, connection strings ou payloads sensíveis.
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
                .WriteTo.Console();

            var filePath = configuration["Observability:Serilog:FilePath"];
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                loggerConfiguration.WriteTo.File(filePath, rollingInterval: RollingInterval.Day);
            }

            var lokiEndpoint = configuration["Observability:Serilog:Loki:Endpoint"];
            if (!string.IsNullOrWhiteSpace(lokiEndpoint))
            {
                var applicationName = configuration["OpenTelemetry:ServiceName"] ?? "NexTraceOne";
                var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

                var lokiLabels = new LokiLabel[]
                {
                    new() { Key = "application", Value = applicationName },
                    new() { Key = "environment", Value = environmentName }
                };

                loggerConfiguration.WriteTo.GrafanaLoki(
                    lokiEndpoint,
                    labels: lokiLabels,
                    propertiesAsLabels: ["module"],
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
            }
        });
    }
}
