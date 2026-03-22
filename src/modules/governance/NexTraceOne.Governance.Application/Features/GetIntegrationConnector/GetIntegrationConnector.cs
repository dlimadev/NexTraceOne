using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

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
    public sealed class Handler(
        IIntegrationConnectorRepository connectorRepository,
        IIngestionExecutionRepository executionRepository,
        IIngestionSourceRepository sourceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.ConnectorId, out var connectorGuid))
            {
                return Error.Validation("INVALID_CONNECTOR_ID", "Invalid connector ID format");
            }

            var connectorId = new IntegrationConnectorId(connectorGuid);
            var connector = await connectorRepository.GetByIdAsync(connectorId, cancellationToken);

            if (connector is null)
            {
                return Error.NotFound("CONNECTOR_NOT_FOUND", $"Connector {request.ConnectorId} not found");
            }

            // Get recent executions
            var executions = await executionRepository.ListByConnectorIdAsync(connectorId, limit: 10, cancellationToken);

            // Get sources for scope
            var sources = await sourceRepository.ListByConnectorIdAsync(connectorId, cancellationToken);

            var response = new Response(
                ConnectorId: connector.Id.Value,
                Name: connector.Name,
                DisplayName: connector.Description ?? connector.Name,
                ConnectorType: connector.ConnectorType,
                Provider: connector.Provider,
                Status: connector.Status.ToString(),
                Environment: connector.Environment,
                HealthStatus: connector.Health.ToString(),
                LastSuccessAt: connector.LastSuccessAt,
                LastFailureAt: connector.LastErrorAt,
                FreshnessLag: FormatFreshnessLag(connector.FreshnessLagMinutes),
                ItemsSyncedTotal: connector.TotalExecutions,
                Description: connector.Description ?? "No description available",
                Configuration: new ConfigurationSummary(
                    Endpoint: connector.Endpoint ?? "Not configured",
                    AuthenticationMode: connector.AuthenticationMode,
                    PollingMode: connector.PollingMode,
                    RetryPolicy: "Exponential backoff, max 3 retries",
                    IsEnabled: connector.Status == Domain.Enums.ConnectorStatus.Active,
                    AllowedDataDomains: new[] { "Changes", "Runtime", "Alerts" }),
                RecentExecutions: executions.Select(e => new ExecutionSummaryItem(
                    ExecutionId: e.Id.Value,
                    StartedAt: e.StartedAt,
                    FinishedAt: e.CompletedAt,
                    Result: e.Result.ToString(),
                    RecordsProcessed: e.ItemsProcessed,
                    Warnings: e.ItemsFailed > 0 && e.ItemsSucceeded > 0 ? e.ItemsFailed : 0,
                    Errors: e.Result == Domain.Enums.ExecutionResult.Failed ? e.ItemsFailed : 0))
                    .ToList(),
                SourceScope: sources.Select(s => s.Name).ToList(),
                AllowedTeams: connector.AllowedTeams.ToList());

            return Result<Response>.Success(response);
        }

        private static string? FormatFreshnessLag(int? lagMinutes)
        {
            if (!lagMinutes.HasValue) return null;

            var lag = lagMinutes.Value;
            return lag switch
            {
                < 60 => $"{lag}m",
                < 1440 => $"{lag / 60}h {lag % 60}m",
                _ => $"{lag / 1440}d"
            };
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
