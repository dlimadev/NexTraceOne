using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Observability.Alerting;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Channels;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Writers;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.Ingestion;
using NexTraceOne.BuildingBlocks.Observability.Metrics;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Collection.ClrProfiler;
using NexTraceOne.BuildingBlocks.Observability.Observability.Collection.OpenTelemetryCollector;
using NexTraceOne.BuildingBlocks.Observability.Observability.Providers.ClickHouse;
using NexTraceOne.BuildingBlocks.Observability.Observability.Providers.Elastic;
using NexTraceOne.BuildingBlocks.Observability.Observability.Services;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Tracing;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NexTraceOne.BuildingBlocks.Observability;

/// <summary>
/// Registra toda a infraestrutura de observabilidade da plataforma:
/// OpenTelemetry (tracing + metrics via OTLP), provider de observabilidade
/// configurável (Elastic ou ClickHouse), estratégia de coleta por ambiente
/// (OpenTelemetry Collector ou CLR Profiler) e health checks.
///
/// A plataforma é OpenTelemetry-native na ingestão, correlation-first no produto
/// e storage-aware na persistência. O PostgreSQL serve como Product Store
/// (agregados, correlações, topologia) e o provider configurável (Elastic ou ClickHouse)
/// como storage analítico para traces, logs e métricas crus.
///
/// Coleta, transporte, storage e análise são preocupações separadas.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra observabilidade base compartilhada da plataforma.
    /// Configura: OpenTelemetry (tracing + metrics), provider de observabilidade
    /// (Elastic ou ClickHouse), modo de coleta (Collector ou CLR Profiler),
    /// opções de telemetria (Product Store, retenção, collector) e health checks.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuração de telemetria: Product Store, provider, collection mode, retenção, collector
        services.Configure<TelemetryStoreOptions>(
            configuration.GetSection(TelemetryStoreOptions.SectionName));

        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "NexTraceOne";
        // Canonical OTLP endpoint: Telemetry:Collector:OtlpGrpcEndpoint (canonical path).
        // OpenTelemetry:Endpoint is kept as backwards-compatible fallback.
        // OTEL_EXPORTER_OTLP_ENDPOINT env var takes precedence (standard SDK convention).
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            ?? configuration["Telemetry:Collector:OtlpGrpcEndpoint"]
            ?? configuration["OpenTelemetry:Endpoint"];

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

        // Registrar provider de observabilidade baseado na configuração
        RegisterObservabilityProvider(services, configuration);

        // Registrar estratégia de coleta baseada na configuração
        RegisterCollectionModeStrategy(services, configuration);

        services.AddIngestionMetrics(configuration);

        services.AddNexTraceHealthChecks();

        return services;
    }

    /// <summary>
    /// Registra IIngestionMetricsCollector para emissão de métricas do pipeline de ingestão.
    /// Pode ser chamado independentemente do AddBuildingBlocksObservability (ex: Ingestion.Api, BackgroundWorkers).
    /// </summary>
    public static IServiceCollection AddIngestionMetrics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IngestionObservabilityOptions>(
            configuration.GetSection(IngestionObservabilityOptions.SectionName));

        var enabled = configuration.GetValue<bool>($"{IngestionObservabilityOptions.SectionName}:Enabled", defaultValue: true);

        if (enabled)
            services.AddSingleton<IIngestionMetricsCollector, IngestionMetricsCollector>();
        else
            services.AddSingleton<IIngestionMetricsCollector, NullIngestionMetricsCollector>();

        return services;
    }

    /// <summary>
    /// Registra o provider de observabilidade configurado (Elastic ou ClickHouse).
    /// A escolha é feita por configuração em Telemetry:ObservabilityProvider:Provider.
    /// Padrão: Elastic.
    /// </summary>
    private static void RegisterObservabilityProvider(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var providerName = configuration["Telemetry:ObservabilityProvider:Provider"] ?? "Elastic";

        if (string.Equals(providerName, "ClickHouse", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ClickHouseObservabilityProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddSingleton<IObservabilityProvider>(sp =>
                sp.GetRequiredService<ClickHouseObservabilityProvider>());
        }
        else
        {
            // Default: Elastic
            services.AddHttpClient<ElasticObservabilityProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddSingleton<IObservabilityProvider>(sp =>
                sp.GetRequiredService<ElasticObservabilityProvider>());
        }

        services.AddSingleton<ITelemetryQueryService, TelemetryQueryService>();
    }

    /// <summary>
    /// Registra a estratégia de coleta configurada (OpenTelemetryCollector ou ClrProfiler).
    /// A escolha é feita por configuração em Telemetry:CollectionMode:ActiveMode.
    /// </summary>
    private static void RegisterCollectionModeStrategy(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var collectionMode = configuration["Telemetry:CollectionMode:ActiveMode"] ?? "OpenTelemetryCollector";

        if (string.Equals(collectionMode, "ClrProfiler", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICollectionModeStrategy, ClrProfilerStrategy>();
        }
        else
        {
            // Default: OpenTelemetryCollector
            services.AddSingleton<ICollectionModeStrategy, OpenTelemetryCollectorStrategy>();
        }
    }

    /// <summary>
    /// Registra a camada de escrita analítica do NexTraceOne.
    /// Quando Analytics:Enabled = true, seleciona o writer de acordo com
    /// Telemetry:ObservabilityProvider:Provider (ClickHouse ou Elastic).
    /// Quando false, registra NullAnalyticsWriter (graceful degradation).
    ///
    /// Módulos suportados: Product Analytics, Operational Intelligence,
    /// Integrations, Governance Analytics.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksAnalytics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var analyticsOptions = new AnalyticsOptions();
        configuration.GetSection(AnalyticsOptions.SectionName).Bind(analyticsOptions);

        if (!analyticsOptions.Enabled)
        {
            services.AddSingleton<IAnalyticsWriter, NullAnalyticsWriter>();
            return services;
        }

        services.Configure<AnalyticsOptions>(configuration.GetSection(AnalyticsOptions.SectionName));

        var providerName = configuration["Telemetry:ObservabilityProvider:Provider"] ?? "Elastic";

        if (string.Equals(providerName, "ClickHouse", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ClickHouseAnalyticsWriter>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(analyticsOptions.WriteTimeoutSeconds + 5);
            });
            services.AddSingleton<IAnalyticsWriter, ClickHouseAnalyticsWriter>();
        }
        else
        {
            services.AddHttpClient<ElasticAnalyticsWriter>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(analyticsOptions.WriteTimeoutSeconds + 5);
            });
            services.AddSingleton<IAnalyticsWriter, ElasticAnalyticsWriter>();
        }

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
