using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListIngestionSources;

/// <summary>
/// Feature: ListIngestionSources — lista fontes de ingestão registadas.
/// Cada fonte está associada a um conector e tem domínio de dados, nível de confiança e estado de frescura.
/// </summary>
public static class ListIngestionSources
{
    /// <summary>Query para listar fontes de ingestão com filtros e paginação.</summary>
    public sealed record Query(
        Guid? ConnectorId = null,
        string? DataDomain = null,
        string? TrustLevel = null,
        string? Status = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que retorna a lista paginada de fontes de ingestão.</summary>
    public sealed class Handler(
        IIngestionSourceRepository sourceRepository,
        IIntegrationConnectorRepository connectorRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Parse optional filters
            IntegrationConnectorId? connectorIdFilter = request.ConnectorId.HasValue
                ? new IntegrationConnectorId(request.ConnectorId.Value)
                : null;

            SourceStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<SourceStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            FreshnessStatus? freshnessFilter = null;
            if (!string.IsNullOrEmpty(request.TrustLevel) &&
                Enum.TryParse<FreshnessStatus>(request.TrustLevel, ignoreCase: true, out var parsedFreshness))
            {
                freshnessFilter = parsedFreshness;
            }

            var sources = await sourceRepository.ListAsync(
                connectorId: connectorIdFilter,
                status: statusFilter,
                freshnessStatus: freshnessFilter,
                ct: cancellationToken);

            // Get connector names for display
            var connectorIds = sources.Select(s => s.ConnectorId).Distinct().ToList();
            var connectorNames = new Dictionary<IntegrationConnectorId, string>();

            foreach (var connId in connectorIds)
            {
                var connector = await connectorRepository.GetByIdAsync(connId, cancellationToken);
                if (connector is not null)
                {
                    connectorNames[connId] = connector.Name;
                }
            }

            var total = sources.Count;

            var items = sources
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new IngestionSourceItem(
                    SourceId: s.Id.Value,
                    ConnectorId: s.ConnectorId.Value,
                    ConnectorName: connectorNames.TryGetValue(s.ConnectorId, out var name) ? name : "Unknown",
                    SourceType: s.SourceType,
                    DataDomain: string.IsNullOrEmpty(s.DataDomain) ? s.SourceType : s.DataDomain,
                    TrustLevel: s.TrustLevel.ToString(),
                    Freshness: s.FreshnessStatus.ToString(),
                    LastReceivedAt: s.LastDataReceivedAt,
                    LastProcessedAt: s.LastProcessedAt,
                    Status: s.Status.ToString(),
                    ProvenanceSummary: s.Description ?? "No description available"))
                .ToList();

            var response = new Response(
                TotalCount: total,
                Page: request.Page,
                PageSize: request.PageSize,
                Items: items);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta paginada com lista de fontes de ingestão.</summary>
    public sealed record Response(
        int TotalCount,
        int Page,
        int PageSize,
        IReadOnlyList<IngestionSourceItem> Items);

    /// <summary>DTO de uma fonte de ingestão com dados de proveniência e confiança.</summary>
    public sealed record IngestionSourceItem(
        Guid SourceId,
        Guid ConnectorId,
        string ConnectorName,
        string SourceType,
        string DataDomain,
        string TrustLevel,
        string Freshness,
        DateTimeOffset? LastReceivedAt,
        DateTimeOffset? LastProcessedAt,
        string Status,
        string ProvenanceSummary);
}
