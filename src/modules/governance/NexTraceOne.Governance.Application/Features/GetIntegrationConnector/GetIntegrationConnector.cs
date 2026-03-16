using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetIntegrationConnector;

/// <summary>
/// Feature: GetIntegrationConnector — detalhe completo de um conector de integração.
/// Inclui configuração, execuções recentes, escopo e permissões.
/// </summary>
public static class GetIntegrationConnector
{
    /// <summary>Query para obter detalhe de um conector pelo ID.</summary>
    public sealed record Query(string ConnectorId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe completo de um conector de integração.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new Response(
                ConnectorId: Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"),
                Name: "github-cicd",
                DisplayName: "GitHub CI/CD",
                ConnectorType: "CI/CD",
                Provider: "GitHub",
                Status: "Active",
                Environment: "Production",
                HealthStatus: "Healthy",
                LastSuccessAt: DateTimeOffset.UtcNow.AddMinutes(-12),
                LastFailureAt: null,
                FreshnessLag: "12m",
                ItemsSyncedTotal: 48_230,
                Description: "Integração com GitHub Actions para ingestão de pipelines CI/CD, deployments e status de workflows. Recolhe dados de builds, releases e artefactos para correlação com mudanças em produção.",
                Configuration: new ConfigurationSummary(
                    Endpoint: "https://api.github.com/repos/org/*/actions",
                    AuthenticationMode: "OAuth2 App Token",
                    PollingMode: "Webhook + Polling (fallback 5m)",
                    RetryPolicy: "Exponential backoff, max 3 retries",
                    IsEnabled: true,
                    AllowedDataDomains: new[] { "Changes", "Runtime", "Alerts" }),
                RecentExecutions: new List<ExecutionSummaryItem>
                {
                    new(Guid.Parse("e1e2e3e4-0001-4000-8000-000000000001"),
                        DateTimeOffset.UtcNow.AddMinutes(-12),
                        DateTimeOffset.UtcNow.AddMinutes(-11),
                        "Success", 142, 0, 0),
                    new(Guid.Parse("e1e2e3e4-0002-4000-8000-000000000002"),
                        DateTimeOffset.UtcNow.AddMinutes(-42),
                        DateTimeOffset.UtcNow.AddMinutes(-41),
                        "Success", 89, 0, 0),
                    new(Guid.Parse("e1e2e3e4-0003-4000-8000-000000000003"),
                        DateTimeOffset.UtcNow.AddHours(-1).AddMinutes(-12),
                        DateTimeOffset.UtcNow.AddHours(-1).AddMinutes(-10),
                        "PartialSuccess", 210, 3, 0),
                    new(Guid.Parse("e1e2e3e4-0004-4000-8000-000000000004"),
                        DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(-12),
                        DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(-11),
                        "Success", 175, 0, 0),
                    new(Guid.Parse("e1e2e3e4-0005-4000-8000-000000000005"),
                        DateTimeOffset.UtcNow.AddHours(-3).AddMinutes(-12),
                        DateTimeOffset.UtcNow.AddHours(-3).AddMinutes(-10),
                        "Failed", 0, 0, 2)
                },
                SourceScope: new List<string>
                {
                    "org/payment-service",
                    "org/order-api",
                    "org/catalog-sync",
                    "org/notification-worker",
                    "org/platform-infra"
                },
                AllowedTeams: new List<string>
                {
                    "payment-squad",
                    "order-squad",
                    "platform-squad",
                    "integration-squad"
                });

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com detalhe completo do conector de integração.</summary>
    public sealed record Response(
        Guid ConnectorId,
        string Name,
        string DisplayName,
        string ConnectorType,
        string Provider,
        string Status,
        string Environment,
        string HealthStatus,
        DateTimeOffset? LastSuccessAt,
        DateTimeOffset? LastFailureAt,
        string? FreshnessLag,
        long ItemsSyncedTotal,
        string Description,
        ConfigurationSummary Configuration,
        IReadOnlyList<ExecutionSummaryItem> RecentExecutions,
        IReadOnlyList<string> SourceScope,
        IReadOnlyList<string> AllowedTeams);

    /// <summary>DTO com resumo da configuração do conector.</summary>
    public sealed record ConfigurationSummary(
        string Endpoint,
        string AuthenticationMode,
        string PollingMode,
        string RetryPolicy,
        bool IsEnabled,
        string[] AllowedDataDomains);

    /// <summary>DTO com resumo de uma execução recente do conector.</summary>
    public sealed record ExecutionSummaryItem(
        Guid ExecutionId,
        DateTimeOffset StartedAt,
        DateTimeOffset? FinishedAt,
        string Result,
        int RecordsProcessed,
        int Warnings,
        int Errors);
}
