using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexTraceOne.BuildingBlocks.Observability.Logging;

/// <summary>
/// Configuração centralizada do Serilog para toda a plataforma.
/// Inclui: enrichers (Environment, MachineName, ThreadId),
/// sinks (Console, File), destructuring de objetos de domínio.
/// 
/// A plataforma envia logs via OpenTelemetry (OTLP) para o Collector,
/// que roteia para o provider de observabilidade configurado (ClickHouse ou Elastic).
/// Não há dependência direta de ferramentas externas de visualização.
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
        });
    }
}
