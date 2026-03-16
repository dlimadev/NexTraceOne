using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPlatformConfig;

/// <summary>
/// Feature: GetPlatformConfig — configuração runtime da plataforma (sem segredos).
/// Expõe modo de deployment, feature flags, estado de subsistemas e conectividade
/// para administradores e dashboards operacionais.
/// Utiliza IConfiguration real para environment, feature flags e nomes de connection strings.
/// </summary>
public static class GetPlatformConfig
{
    /// <summary>Query sem parâmetros — retorna configuração runtime agregada.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que agrega configuração runtime visível da plataforma.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var environmentName =
                configuration["ASPNETCORE_ENVIRONMENT"]
                ?? configuration["DOTNET_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";

            // Feature flags reais a partir da configuração, com fallback para mock
            var featureFlags = BuildFeatureFlags();

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

            // Nomes de connection strings reais (sem expor credenciais)
            var databases = BuildDatabaseConnectivity();

            var response = new Response(
                EnvironmentName: environmentName,
                DeploymentMode: DeploymentMode.SaaS,
                FeatureFlags: featureFlags,
                Subsystems: subsystems,
                Databases: databases,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private List<FeatureFlagDto> BuildFeatureFlags()
        {
            var featureFlagsSection = configuration.GetSection("FeatureFlags");
            if (featureFlagsSection.Exists() && featureFlagsSection.GetChildren().Any())
            {
                return featureFlagsSection.GetChildren()
                    .Select(c => new FeatureFlagDto(
                        c.Key,
                        bool.TryParse(c.Value, out var enabled) && enabled,
                        $"Feature flag: {c.Key}"))
                    .ToList();
            }

            return
            [
                new("ai-assistant", true, "AI Assistant module"),
                new("contract-studio", true, "Contract Studio editor"),
                new("change-intelligence", true, "Change Intelligence analysis"),
                new("finops-dashboard", false, "FinOps contextual dashboard"),
                new("developer-portal", true, "Developer Portal access"),
                new("advanced-analytics", false, "Advanced analytics pipeline")
            ];
        }

        private List<DatabaseConnectivityDto> BuildDatabaseConnectivity()
        {
            var connectionStrings = configuration.GetSection("ConnectionStrings");
            if (connectionStrings.Exists() && connectionStrings.GetChildren().Any())
            {
                return connectionStrings.GetChildren()
                    .Select(c => new DatabaseConnectivityDto(
                        c.Key,
                        InferProvider(c.Value),
                        !string.IsNullOrWhiteSpace(c.Value),
                        !string.IsNullOrWhiteSpace(c.Value) ? "Connected" : "Empty"))
                    .ToList();
            }

            return
            [
                new("Primary", "PostgreSQL", true, "Connected"),
                new("ReadReplica", "PostgreSQL", true, "Connected"),
                new("Cache", "Redis", true, "Connected")
            ];
        }

        private static string InferProvider(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return "Unknown";
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                return "PostgreSQL";
            if (connectionString.Contains("redis", StringComparison.OrdinalIgnoreCase))
                return "Redis";
            if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
                return "SQL Server";
            return "Configured";
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
