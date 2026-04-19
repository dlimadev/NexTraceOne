using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Infrastructure.Observability;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence.Providers;
using NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

namespace NexTraceOne.Governance.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura do módulo Governance.
/// Inclui: DbContext, Repositórios, UnitOfWork.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo Governance.</summary>
    public static IServiceCollection AddGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("GovernanceDatabase", "NexTraceOne");

        services.AddDbContext<GovernanceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        // UnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GovernanceDbContext>());
        services.AddScoped<IGovernanceUnitOfWork>(sp => sp.GetRequiredService<GovernanceDbContext>());

        // Repositories
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IGovernanceDomainRepository, GovernanceDomainRepository>();
        services.AddScoped<IGovernancePackRepository, GovernancePackRepository>();
        services.AddScoped<IGovernancePackVersionRepository, GovernancePackVersionRepository>();
        services.AddScoped<IGovernanceWaiverRepository, GovernanceWaiverRepository>();
        services.AddScoped<IDelegatedAdministrationRepository, DelegatedAdministrationRepository>();
        services.AddScoped<ITeamDomainLinkRepository, TeamDomainLinkRepository>();
        services.AddScoped<IGovernanceRolloutRecordRepository, GovernanceRolloutRecordRepository>();
        services.AddScoped<IGovernanceAnalyticsRepository, GovernanceAnalyticsRepository>();
        services.AddScoped<IEvidencePackageRepository, EvidencePackageRepository>();
        services.AddScoped<IComplianceGapRepository, ComplianceGapRepository>();
        services.AddScoped<IPolicyAsCodeRepository, PolicyAsCodeRepository>();

        // Security Gate
        services.AddScoped<ISecurityScanRepository, SecurityScanRepository>();

        // Custom Dashboards & Technical Debt
        services.AddScoped<ICustomDashboardRepository, CustomDashboardRepository>();
        services.AddScoped<ITechnicalDebtRepository, TechnicalDebtRepository>();

        // Service Maturity Assessments
        services.AddScoped<IServiceMaturityAssessmentRepository, ServiceMaturityAssessmentRepository>();

        // Team Health Snapshots
        services.AddScoped<ITeamHealthSnapshotRepository, TeamHealthSnapshotRepository>();

        // Change Cost Impact (FinOps por mudança)
        services.AddScoped<IChangeCostImpactRepository, ChangeCostImpactRepository>();

        // Executive Briefings
        services.AddScoped<IExecutiveBriefingRepository, ExecutiveBriefingRepository>();

        // Cost Attributions (FinOps contextual)
        services.AddScoped<ICostAttributionRepository, CostAttributionRepository>();

        // License Compliance Reports
        services.AddScoped<ILicenseComplianceReportRepository, LicenseComplianceReportRepository>();

        // FinOps Budget Approvals
        services.AddScoped<IFinOpsBudgetApprovalRepository, FinOpsBudgetApprovalRepository>();

        // Platform runtime providers — real data for P03.5 platform status handlers
        services.AddScoped<IPlatformQueueMetricsProvider, GovernanceOutboxQueueMetricsProvider>();
        services.AddScoped<IPlatformJobStatusProvider, KnownJobsStatusProvider>();
        services.AddScoped<IPlatformEventProvider, GovernanceEventProvider>();

        // TenantSchemaManager — schema-per-tenant PostgreSQL isolation
        services.AddTenantSchemaManager(connectionString);

        // OTEL Metrics — ingestão de métricas do OpenTelemetry Collector
        services.AddScoped<IOtelMetricRepository, OtelMetricRepository>();

        // Non-Prod Schedules
        services.AddScoped<INonProdScheduleRepository, NonProdScheduleRepository>();

        // Demo Seed State
        services.AddScoped<IDemoSeedStateRepository, DemoSeedStateRepository>();

        // SAML SSO Configuration
        services.AddScoped<ISamlSsoConfigurationRepository, SamlSsoConfigurationRepository>();

        // GreenOps Configuration
        services.AddScoped<IGreenOpsConfigurationRepository, GreenOpsConfigurationRepository>();

        // Support Bundles — geração real de bundles de diagnóstico
        services.AddScoped<ISupportBundleRepository, SupportBundleRepository>();

        // HTTP Audit Reader — consulta IObservabilityProvider para auditoria de chamadas HTTP externas
        services.AddScoped<IHttpAuditReader, ObservabilityHttpAuditReader>();

        return services;
    }
}
