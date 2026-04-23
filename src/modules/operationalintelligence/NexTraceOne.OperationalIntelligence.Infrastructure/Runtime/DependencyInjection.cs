using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime;

/// <summary>
/// Registra serviços de infraestrutura do módulo RuntimeIntelligence.
/// Inclui: DbContext com connection string isolada, repositórios e UnitOfWork.
/// Cada módulo possui sua própria base de dados — sem compartilhamento.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo RuntimeIntelligence ao container DI.</summary>
    public static IServiceCollection AddRuntimeIntelligenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("RuntimeIntelligenceDatabase", "NexTraceOne");

        services.AddDbContext<RuntimeIntelligenceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RuntimeIntelligenceDbContext>());
        services.AddScoped<IRuntimeIntelligenceUnitOfWork>(sp => sp.GetRequiredService<RuntimeIntelligenceDbContext>());
        services.AddScoped<IRuntimeSnapshotRepository, RuntimeSnapshotRepository>();
        services.AddScoped<IRuntimeBaselineRepository, RuntimeBaselineRepository>();
        services.AddScoped<IDriftFindingRepository, DriftFindingRepository>();
        services.AddScoped<IObservabilityProfileRepository, ObservabilityProfileRepository>();
        services.AddScoped<IRuntimeIntelligenceModule, RuntimeIntelligenceModule>();

        services.AddScoped<ICustomChartRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.CustomChartRepository>();
        services.AddScoped<IChaosExperimentRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.ChaosExperimentRepository>();
        services.AddScoped<IAnomalyNarrativeRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.AnomalyNarrativeRepository>();
        services.AddScoped<IEnvironmentDriftReportRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.EnvironmentDriftReportRepository>();
        services.AddScoped<IOperationalPlaybookRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.OperationalPlaybookRepository>();
        services.AddScoped<IPlaybookExecutionRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.PlaybookExecutionRepository>();
        services.AddScoped<IResilienceReportRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.ResilienceReportRepository>();
        services.AddScoped<IProfilingSessionRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.ProfilingSessionRepository>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions.IServiceCostAllocationRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.ServiceCostAllocationRepository>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.ISloObservationRepository, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories.SloObservationRepository>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IActiveServiceNamesReader, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services.NullActiveServiceNamesReader>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.ITeamOperationalMetricsReader, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services.NullTeamOperationalMetricsReader>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IVulnerabilityAdvisoryReader, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services.NullVulnerabilityAdvisoryReader>();
        // ── Wave AB.3 — Incident Knowledge Base Report (null reader) ──────
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IIncidentKnowledgeReader, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services.NullIncidentKnowledgeReader>();

        // ── Wave AC.3 — Platform Adoption Report (null reader) ────────────
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IPlatformAdoptionReader, NexTraceOne.OperationalIntelligence.Application.Runtime.Services.NullPlatformAdoptionReader>();

        // ── Wave AI.3 — Deployment Risk Forecast Reader (null bridge — CG) ─
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IDeploymentRiskForecastReader, NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services.NullDeploymentRiskForecastReader>();

        // ── Wave AN — SRE null readers ─────────────────────────────────────
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IErrorBudgetReader,
            NexTraceOne.OperationalIntelligence.Application.Runtime.NullErrorBudgetReader>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IIncidentImpactScorecardReader,
            NexTraceOne.OperationalIntelligence.Application.Runtime.NullIncidentImpactScorecardReader>();
        services.AddScoped<NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.ISreMaturityReader,
            NexTraceOne.OperationalIntelligence.Application.Runtime.NullSreMaturityReader>();

        // ── Incidents (Incident Correlation & Mitigation) infrastructure ──
        services.AddIncidentsInfrastructure(configuration);

        // ── Automation (Workflow Persistence) infrastructure ──
        services.AddAutomationInfrastructure(configuration);

        // ── TelemetryStore (Product Store) infrastructure ──
        services.AddTelemetryStoreInfrastructure(configuration);

        return services;
    }
}
