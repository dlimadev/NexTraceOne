using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListDiscoveredServices;

/// <summary>
/// Feature: ListDiscoveredServices — lista serviços descobertos com filtros.
/// Alimenta a ServiceDiscoveryPage no frontend.
/// </summary>
public static class ListDiscoveredServices
{
    /// <summary>Query com filtros opcionais.</summary>
    public sealed record Query(
        string? Status,
        string? Environment,
        string? SearchTerm) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Environment).MaximumLength(100);
            RuleFor(x => x.SearchTerm).MaximumLength(200);
        }
    }

    /// <summary>Handler que lista serviços descobertos.</summary>
    public sealed class Handler(
        IDiscoveredServiceRepository discoveredServiceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            DiscoveryStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<DiscoveryStatus>(request.Status, true, out var parsed))
            {
                statusFilter = parsed;
            }

            var services = await discoveredServiceRepository.ListFilteredAsync(
                statusFilter,
                request.Environment,
                request.SearchTerm,
                cancellationToken);

            var items = services.Select(s => new DiscoveredServiceItem(
                s.Id.Value,
                s.ServiceName,
                s.ServiceNamespace,
                s.Environment,
                s.FirstSeenAt,
                s.LastSeenAt,
                s.TraceCount,
                s.EndpointCount,
                s.Status.ToString(),
                s.MatchedServiceAssetId,
                s.IgnoreReason)).ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta com lista de serviços descobertos.</summary>
    public sealed record Response(IReadOnlyList<DiscoveredServiceItem> Items, int TotalCount);

    /// <summary>Item individual de serviço descoberto.</summary>
    public sealed record DiscoveredServiceItem(
        Guid Id,
        string ServiceName,
        string ServiceNamespace,
        string Environment,
        DateTimeOffset FirstSeenAt,
        DateTimeOffset LastSeenAt,
        long TraceCount,
        int EndpointCount,
        string Status,
        Guid? MatchedServiceAssetId,
        string? IgnoreReason);
}
