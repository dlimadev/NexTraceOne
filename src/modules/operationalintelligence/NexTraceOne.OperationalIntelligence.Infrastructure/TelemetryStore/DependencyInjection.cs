using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore;

/// <summary>
/// Registra serviços de infraestrutura do sub-módulo TelemetryStore (Product Store).
/// Inclui: DbContext com connection string isolada e repositórios de métricas,
/// topologia, anomalias, referências de telemetria, correlações e investigações.
/// </summary>
public static class TelemetryStoreDependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do TelemetryStore ao container DI.</summary>
    public static IServiceCollection AddTelemetryStoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredConnectionString("TelemetryStoreDatabase", "NexTraceOne");

        services.AddDbContext<TelemetryStoreDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantRlsInterceptor>()));

        // ── Metrics Store ────────────────────────────────────────────────────
        services.AddScoped<IServiceMetricsWriter, ServiceMetricsRepository>();
        services.AddScoped<IServiceMetricsReader, ServiceMetricsRepository>();
        services.AddScoped<IDependencyMetricsWriter, DependencyMetricsRepository>();
        services.AddScoped<IDependencyMetricsReader, DependencyMetricsRepository>();

        // ── Product Store ────────────────────────────────────────────────────
        services.AddScoped<IObservedTopologyWriter, ObservedTopologyRepository>();
        services.AddScoped<IObservedTopologyReader, ObservedTopologyRepository>();
        services.AddScoped<IAnomalySnapshotWriter, AnomalySnapshotRepository>();
        services.AddScoped<IAnomalySnapshotReader, AnomalySnapshotRepository>();
        services.AddScoped<ITelemetryReferenceWriter, TelemetryReferenceRepository>();
        services.AddScoped<ITelemetryReferenceReader, TelemetryReferenceRepository>();
        services.AddScoped<IReleaseCorrelationWriter, ReleaseCorrelationRepository>();
        services.AddScoped<IReleaseCorrelationReader, ReleaseCorrelationRepository>();
        services.AddScoped<IInvestigationContextWriter, InvestigationContextRepository>();
        services.AddScoped<IInvestigationContextReader, InvestigationContextRepository>();

        return services;
    }
}
