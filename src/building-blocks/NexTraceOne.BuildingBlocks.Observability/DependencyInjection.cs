using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Observability.Alerting;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Channels;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.Metrics;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Tracing;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NexTraceOne.BuildingBlocks.Observability;

/// <summary>
/// Registra toda a infraestrutura de observabilidade da plataforma:
/// OpenTelemetry (tracing + metrics via OTLP), configuração de telemetria
/// (Product Store vs Telemetry Store, retenção, collector) e health checks.
///
/// A plataforma é OpenTelemetry-native na ingestão, correlation-first no produto
/// e storage-aware na persistência. O PostgreSQL serve como Product Store
/// (agregados, correlações, topologia) e os backends especializados
/// (Tempo, Loki) como Telemetry Store (traces e logs crus).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra observabilidade base compartilhada da plataforma.
    /// Configura: OpenTelemetry (tracing + metrics), opções de telemetria
    /// (Product Store, Telemetry Store, retenção, collector) e health checks.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuração de telemetria: Product Store vs Telemetry Store, retenção, collector
        services.Configure<TelemetryStoreOptions>(
            configuration.GetSection(TelemetryStoreOptions.SectionName));

        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "NexTraceOne";
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceNamespace: "nextraceone",
                    serviceVersion: typeof(DependencyInjection).Assembly
                        .GetName().Version?.ToString() ?? "0.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "production"
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(
                        NexTraceActivitySources.Commands.Name,
                        NexTraceActivitySources.Queries.Name,
                        NexTraceActivitySources.Events.Name,
                        NexTraceActivitySources.ExternalHttp.Name,
                        NexTraceActivitySources.TelemetryPipeline.Name,
                        NexTraceActivitySources.Integrations.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                }
                else
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(NexTraceMeters.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                }
                else
                {
                    metrics.AddOtlpExporter();
                }
            });

        services.AddNexTraceHealthChecks();

        return services;
    }

    /// <summary>
    /// Registra o sistema de alertas da plataforma: gateway central, canais
    /// condicionais (Webhook, Email) e opções de configuração.
    /// Canais são ativados com base na configuração da secção "Alerting".
    /// </summary>
    public static IServiceCollection AddBuildingBlocksAlerting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AlertingOptions>(
            configuration.GetSection(AlertingOptions.SectionName));

        services.AddSingleton<IAlertGateway, AlertGateway>();

        var alertingOptions = new AlertingOptions();
        configuration.GetSection(AlertingOptions.SectionName).Bind(alertingOptions);

        if (!alertingOptions.Enabled)
        {
            return services;
        }

        if (alertingOptions.Webhook.Enabled)
        {
            services.AddHttpClient(WebhookAlertChannel.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(alertingOptions.Webhook.TimeoutSeconds);
            });

            services.AddSingleton<IAlertChannel, WebhookAlertChannel>();
        }

        if (alertingOptions.Email.Enabled)
        {
            services.AddSingleton<IAlertChannel, EmailAlertChannel>();
        }

        return services;
    }
}
