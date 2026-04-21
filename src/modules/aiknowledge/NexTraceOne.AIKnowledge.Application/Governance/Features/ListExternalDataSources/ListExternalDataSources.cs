using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListExternalDataSources;

/// <summary>Lista fontes de dados externas com filtros opcionais.</summary>
public static class ListExternalDataSources
{
    public sealed record Query(
        string? ConnectorType,
        bool? IsActive) : IQuery<Response>;

    public sealed class Handler(
        IExternalDataSourceRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            ExternalDataSourceConnectorType? connectorType = null;
            if (!string.IsNullOrWhiteSpace(request.ConnectorType)
                && Enum.TryParse<ExternalDataSourceConnectorType>(request.ConnectorType, ignoreCase: true, out var parsed))
                connectorType = parsed;

            var sources = await repository.ListAsync(connectorType, request.IsActive, cancellationToken);

            var items = sources.Select(s => new DataSourceItem(
                Id: s.Id.Value,
                Name: s.Name,
                Description: s.Description,
                ConnectorType: s.ConnectorType.ToString(),
                IsActive: s.IsActive,
                Priority: s.Priority,
                SyncIntervalMinutes: s.SyncIntervalMinutes,
                LastSyncedAt: s.LastSyncedAt,
                LastSyncStatus: s.LastSyncStatus,
                LastSyncDocumentCount: s.LastSyncDocumentCount,
                RegisteredAt: s.RegisteredAt
            )).ToList();

            return new Response(items);
        }
    }

    public sealed record DataSourceItem(
        Guid Id,
        string Name,
        string? Description,
        string ConnectorType,
        bool IsActive,
        int Priority,
        int SyncIntervalMinutes,
        DateTimeOffset? LastSyncedAt,
        string? LastSyncStatus,
        int LastSyncDocumentCount,
        DateTimeOffset RegisteredAt);

    public sealed record Response(IReadOnlyList<DataSourceItem> Items);
}
