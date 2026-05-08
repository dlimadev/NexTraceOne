using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Nql;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Infrastructure.AI;
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
        services.AddScoped<IDashboardRevisionRepository, DashboardRevisionRepository>();
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

        // OTEL Metrics — provider-aware: ClickHouse for analytics scale, PostgreSQL for MVP
        var otelProvider = configuration["Telemetry:ObservabilityProvider:Provider"] ?? "Elastic";
        if (string.Equals(otelProvider, "ClickHouse", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<ClickHouseOtelMetricOptions>(
                configuration.GetSection(ClickHouseOtelMetricOptions.SectionName));
            services.AddHttpClient<ClickHouseOtelMetricRepository>();
            services.AddScoped<IOtelMetricRepository>(
                sp => sp.GetRequiredService<ClickHouseOtelMetricRepository>());
        }
        else
        {
            services.AddScoped<IOtelMetricRepository, OtelMetricRepository>();
        }

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
        services.AddScoped<IRecoveryJobRepository, RecoveryJobRepository>();

        // HTTP Audit Reader — consulta IObservabilityProvider para auditoria de chamadas HTTP externas
        services.AddScoped<IHttpAuditReader, ObservabilityHttpAuditReader>();

        // Observability backend health — port adapter para IObservabilityProvider
        services.AddScoped<IObservabilityBackendHealth, ObservabilityBackendHealthAdapter>();

        // Dashboard Usage Forwarder — propaga DashboardUsageEvent para analytics store
        services.AddScoped<IDashboardUsageForwarder, DashboardUsageForwarder>();

        // Ingestion Observability — DLQ stats da bb_dead_letter_messages para dashboard de ingestão
        services.AddScoped<IIngestionObservabilityProvider, IngestionObservabilityProvider>();

        // NQL Query Governance — Wave V3.2
        services.AddScoped<IQueryGovernanceService, DefaultQueryGovernanceService>();

        // Notebooks — Wave V3.4
        services.AddScoped<INotebookRepository, NotebookRepository>();

        // Scheduled Dashboard Reports + Usage Analytics — Wave V3.6
        services.AddScoped<IScheduledDashboardReportRepository, ScheduledDashboardReportRepository>();
        services.AddScoped<IDashboardUsageRepository, DashboardUsageRepository>();

        // Dashboard Comments — Wave V3.7 (Real-time Collaboration)
        services.AddScoped<IDashboardCommentRepository, DashboardCommentRepository>();

        // Dashboard Templates Marketplace — Wave V3.8
        services.AddScoped<IDashboardTemplateRepository, DashboardTemplateRepository>();

        // Persona Home Configuration — Wave V3.10
        services.AddScoped<IPersonaHomeConfigurationRepository, PersonaHomeConfigurationRepository>();

        // Presence Sessions — Wave V3.7 (Collaboration)
        services.AddScoped<IPresenceSessionRepository, PresenceSessionRepository>();

        // Dashboard Monitors — Wave V3.9 (Alerting from Widget)
        services.AddScoped<IDashboardMonitorRepository, DashboardMonitorRepository>();

        // Setup Wizard State — F-04
        services.AddScoped<ISetupWizardRepository, SetupWizardRepository>();

        // Widget Snapshots — B-02 (real delta computation)
        services.AddScoped<IWidgetSnapshotRepository, WidgetSnapshotRepository>();

        // Dashboard Data Bridge — B-04 (SSE live stream real events via snapshots)
        services.AddScoped<IDashboardDataBridge, SnapshotDashboardDataBridge>();

        // AI Dashboard Composer — Wave V3.4; uses IChatCompletionProvider when configured
        services.AddScoped<IAiDashboardComposerService>(sp =>
            new AiDashboardComposerService(
                sp.GetService<IChatCompletionProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AiDashboardComposerService>>()));

        return services;
    }
}
