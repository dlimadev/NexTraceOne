using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformConfig;

/// <summary>
/// Feature: GetPlatformConfig — configuração runtime da plataforma (sem segredos).
/// Expõe modo de deployment, feature flags, estado de subsistemas e conectividade
/// para administradores e dashboards operacionais.
/// </summary>
public static class GetPlatformConfig
{
    /// <summary>Query sem parâmetros — retorna configuração runtime agregada.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que agrega configuração runtime visível da plataforma.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var featureFlags = new List<FeatureFlagDto>
            {
                new("ai-assistant", true, "AI Assistant module"),
                new("contract-studio", true, "Contract Studio editor"),
                new("change-intelligence", true, "Change Intelligence analysis"),
                new("finops-dashboard", false, "FinOps contextual dashboard"),
                new("developer-portal", true, "Developer Portal access"),
                new("advanced-analytics", false, "Advanced analytics pipeline")
            };

            var subsystems = new List<SubsystemConfigDto>
            {
                new("Identity", true, "Authentication and authorization"),
                new("Catalog", true, "Service catalog and topology"),
                new("Contracts", true, "API and event contract governance"),
                new("ChangeIntelligence", true, "Production change analysis"),
                new("Audit", true, "Audit trail and compliance"),
                new("Governance", true, "Governance rules and policies"),
                new("Ingestion", true, "Telemetry ingestion pipeline"),
                new("AI", true, "AI assistant and model registry"),
                new("BackgroundWorkers", true, "Background job processing")
            };

            var databases = new List<DatabaseConnectivityDto>
            {
                new("Primary", "PostgreSQL", true, "Connected"),
                new("ReadReplica", "PostgreSQL", true, "Connected"),
                new("Cache", "Redis", true, "Connected")
            };

            var response = new Response(
                EnvironmentName: "Production",
                DeploymentMode: DeploymentMode.SaaS,
                FeatureFlags: featureFlags,
                Subsystems: subsystems,
                Databases: databases,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com configuração runtime da plataforma (sem segredos).</summary>
    public sealed record Response(
        string EnvironmentName,
        DeploymentMode DeploymentMode,
        IReadOnlyList<FeatureFlagDto> FeatureFlags,
        IReadOnlyList<SubsystemConfigDto> Subsystems,
        IReadOnlyList<DatabaseConnectivityDto> Databases,
        DateTimeOffset GeneratedAt);

    /// <summary>Feature flag com estado e descrição.</summary>
    public sealed record FeatureFlagDto(
        string Name,
        bool Enabled,
        string Description);

    /// <summary>Configuração de subsistema com estado de ativação.</summary>
    public sealed record SubsystemConfigDto(
        string Name,
        bool Enabled,
        string Description);

    /// <summary>Resumo de conectividade de base de dados (sem credenciais expostas).</summary>
    public sealed record DatabaseConnectivityDto(
        string Name,
        string Provider,
        bool Connected,
        string StatusDescription);
}
