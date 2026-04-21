using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Application.Features.GetMigrationHealthReport;

/// <summary>
/// Feature: GetMigrationHealthReport — relatório de prontidão de upgrade para plataforma self-hosted.
///
/// Gera um relatório estruturado de gestão de migrações EF Core para operadores e SREs
/// em instalações enterprise self-hosted do NexTraceOne.
///
/// Retorna orientações sobre:
/// - Módulos e os seus bounded contexts (cada módulo tem DbContext isolado)
/// - Passos seguros de upgrade (backup → migrate → verify → rollback-ready)
/// - Ordem recomendada de aplicação de migrações por módulo
/// - Indicadores de saúde para pós-migração
///
/// Wave D backlog — Upgrade path automatizado para migrações EF Core.
/// Nota: A verificação real de migrations pendentes depende de acesso ao banco de dados
/// e é delegada ao operador ou automatizada via CLI `nex db migrate`.
/// </summary>
public static class GetMigrationHealthReport
{
    public sealed record Query(string? TenantId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).MaximumLength(100).When(x => x.TenantId is not null);
        }
    }

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var modules = new List<ModuleMigrationInfo>
            {
                new("IdentityAccess", "iam_*", "Identity, authentication, authorization, licensing", UpgradeOrder: 1),
                new("Configuration", "cfg_*", "Platform configuration and parametrization", UpgradeOrder: 2),
                new("Catalog.Graph", "graph_*", "Service catalog, topology, API assets", UpgradeOrder: 3),
                new("Catalog.Contracts", "contracts_*", "API contracts, SOAP, AsyncAPI, event contracts", UpgradeOrder: 4),
                new("ChangeGovernance", "chg_*", "Releases, promotion gates, evidence packs, change intelligence", UpgradeOrder: 5),
                new("OperationalIntelligence.Incidents", "ops_incidents_*", "Incidents, mitigation, post-incident reviews", UpgradeOrder: 6),
                new("OperationalIntelligence.Runtime", "ops_runtime_*", "Runtime baselines, snapshots, drift, profiling sessions", UpgradeOrder: 7),
                new("OperationalIntelligence.Reliability", "ops_reliability_*", "SLIs, SLOs, reliability reports", UpgradeOrder: 8),
                new("OperationalIntelligence.Automation", "ops_automation_*", "Playbooks, automation workflows", UpgradeOrder: 9),
                new("OperationalIntelligence.TelemetryStore", "ops_telemetry_*", "Telemetry storage, OTel ingest", UpgradeOrder: 10),
                new("Knowledge", "knw_*", "Knowledge hub, runbooks, documentation", UpgradeOrder: 11),
                new("AIKnowledge", "aik_*", "AI models, guardrails, audit, cost attribution", UpgradeOrder: 12),
            };

            var steps = new List<UpgradeStep>
            {
                new(1, "Backup", "Create a full PostgreSQL backup before any migration: pg_dump -Fc nextraceone > backup_$(date +%Y%m%d).dump"),
                new(2, "HealthCheck", "Verify all services are healthy and no in-flight requests: GET /health/ready"),
                new(3, "Migrate", "Apply migrations per module in order: dotnet ef database update --project <module> --connection <conn>"),
                new(4, "Verify", "Run post-migration health checks: GET /health/live and smoke-test critical endpoints"),
                new(5, "RollbackPlan", "If verify fails: pg_restore -d nextraceone backup_$(date +%Y%m%d).dump. EF Down() migrations are available per module."),
            };

            return Task.FromResult(Result<Response>.Success(new Response(
                TotalModules: modules.Count,
                Modules: modules,
                UpgradeSteps: steps,
                RecommendedMaintenanceWindow: "Use a maintenance window of at least 30 minutes for full upgrade. Disable ingestion endpoints during migration.",
                PostMigrationChecks: new List<string>
                {
                    "GET /api/v1/health/live → 200 OK",
                    "GET /api/v1/health/ready → 200 OK",
                    "Verify key catalog queries return data",
                    "Verify release ingestion pipeline is functional",
                    "Check audit trail for migration events",
                })));
        }
    }

    public sealed record ModuleMigrationInfo(
        string ModuleName,
        string TablePrefix,
        string Description,
        int UpgradeOrder);

    public sealed record UpgradeStep(
        int Order,
        string Name,
        string Description);

    public sealed record Response(
        int TotalModules,
        IReadOnlyList<ModuleMigrationInfo> Modules,
        IReadOnlyList<UpgradeStep> UpgradeSteps,
        string RecommendedMaintenanceWindow,
        IReadOnlyList<string> PostMigrationChecks);
}
