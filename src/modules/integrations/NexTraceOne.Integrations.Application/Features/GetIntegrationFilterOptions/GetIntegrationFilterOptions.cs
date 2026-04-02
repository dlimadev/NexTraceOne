using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Features.GetIntegrationFilterOptions;

/// <summary>
/// Feature: GetIntegrationFilterOptions — expõe opções de filtro suportadas pelo backend
/// para o Integration Hub, evitando listas hardcoded e opções incompletas no frontend.
/// Ownership: módulo Integrations.
/// </summary>
public static class GetIntegrationFilterOptions
{
    /// <summary>Query sem parâmetros para obter metadados de filtros.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que devolve tipos de conectores existentes e estados suportados.</summary>
    public sealed class Handler(IIntegrationConnectorRepository connectorRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var connectors = await connectorRepository.ListAsync(
                status: null,
                health: null,
                connectorType: null,
                search: null,
                ct: cancellationToken);

            var connectorTypes = connectors
                .Select(connector => connector.ConnectorType)
                .Where(static connectorType => !string.IsNullOrWhiteSpace(connectorType))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static connectorType => connectorType, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var connectorStatuses = Enum
                .GetNames<ConnectorStatus>()
                .OrderBy(static status => status, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var connectorHealthStatuses = Enum
                .GetNames<ConnectorHealth>()
                .OrderBy(static status => status, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Result<Response>.Success(new Response(
                ConnectorTypes: connectorTypes,
                ConnectorStatuses: connectorStatuses,
                ConnectorHealthStatuses: connectorHealthStatuses));
        }
    }

    /// <summary>Resposta com opções de filtro do Integration Hub.</summary>
    public sealed record Response(
        IReadOnlyList<string> ConnectorTypes,
        IReadOnlyList<string> ConnectorStatuses,
        IReadOnlyList<string> ConnectorHealthStatuses);
}
