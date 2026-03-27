using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.ListIntegrationConnectors;

/// <summary>
/// Feature: ListIntegrationConnectors — lista conectores de integração registados no Integration Hub.
/// Permite filtragem por tipo, status, ambiente e pesquisa textual com paginação.
/// Handler nativo do módulo Integrations.
/// Ownership: módulo Integrations.
/// </summary>
public static class ListIntegrationConnectors
{
    /// <summary>Query para listar conectores com filtros e paginação.</summary>
    public sealed record Query(
        string? ConnectorType = null,
        string? Status = null,
        string? Environment = null,
        string? Search = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que retorna a lista paginada de conectores de integração.</summary>
    public sealed class Handler(IIntegrationConnectorRepository connectorRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Parse optional status filter
            ConnectorStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<ConnectorStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            // Parse optional health filter from Status (for backwards compatibility)
            ConnectorHealth? healthFilter = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<ConnectorHealth>(request.Status, ignoreCase: true, out var parsedHealth))
            {
                healthFilter = parsedHealth;
            }

            var connectors = await connectorRepository.ListAsync(
                status: statusFilter,
                health: healthFilter,
                connectorType: request.ConnectorType,
                search: request.Search,
                ct: cancellationToken);

            var total = connectors.Count;

            var items = connectors
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new ConnectorListItem(
                    ConnectorId: c.Id.Value,
                    Name: c.Name,
                    DisplayName: c.Description ?? c.Name,
                    ConnectorType: c.ConnectorType,
                    Provider: c.Provider,
                    Status: c.Status.ToString(),
                    Environment: c.Environment,
                    HealthStatus: c.Health.ToString(),
                    LastSuccessAt: c.LastSuccessAt,
                    LastFailureAt: c.LastErrorAt,
                    FreshnessLag: FormatFreshnessLag(c.FreshnessLagMinutes),
                    ItemsSyncedTotal: c.TotalExecutions))
                .ToList();

            var response = new Response(
                TotalCount: total,
                Page: request.Page,
                PageSize: request.PageSize,
                Items: items);

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

    /// <summary>Resposta paginada com lista de conectores de integração.</summary>
    public sealed record Response(
        int TotalCount,
        int Page,
        int PageSize,
        IReadOnlyList<ConnectorListItem> Items);

    /// <summary>DTO resumido de um conector de integração.</summary>
    public sealed record ConnectorListItem(
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
        long ItemsSyncedTotal);
}
