using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Features.GetIngestionFreshness;

/// <summary>
/// Feature: GetIngestionFreshness — detalhe de frescura por fonte de ingestão.
/// Permite visualizar o estado de frescura de cada feed por domínio, conector e tipo de fonte.
/// Handler nativo do módulo Integrations.
/// Ownership: módulo Integrations.
/// </summary>
public static class GetIngestionFreshness
{
    /// <summary>Query para obter frescura das fontes de ingestão. Filtro opcional por domínio.</summary>
    public sealed record Query(string? DataDomain = null) : IQuery<Response>;

    /// <summary>Validador da query GetIngestionFreshness.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DataDomain).MaximumLength(100).When(x => x.DataDomain is not null);
        }
    }

    /// <summary>Handler que retorna o detalhe de frescura por fonte de ingestão.</summary>
    public sealed class Handler(
        IIngestionSourceRepository sourceRepository,
        IIntegrationConnectorRepository connectorRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            // Get all active sources
            var sources = await sourceRepository.ListAsync(
                connectorId: null,
                status: null,
                freshnessStatus: null,
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

            var items = sources.Select(s =>
            {
                var lagMinutes = s.LastDataReceivedAt.HasValue
                    ? (long)(now - s.LastDataReceivedAt.Value).TotalMinutes
                    : 0;

                return new FreshnessItem(
                    Domain: s.SourceType, // Using SourceType as Domain for now
                    ConnectorName: connectorNames.TryGetValue(s.ConnectorId, out var name) ? name : "Unknown",
                    SourceType: s.SourceType,
                    Freshness: s.FreshnessStatus.ToString(),
                    LastReceivedAt: s.LastDataReceivedAt,
                    LagMinutes: lagMinutes,
                    TrustLevel: s.TrustLevel.ToString(),
                    Status: s.Status.ToString());
            }).ToList();

            // Filter by domain if specified
            if (!string.IsNullOrEmpty(request.DataDomain))
            {
                items = items
                    .Where(f => f.Domain.Equals(request.DataDomain, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var response = new Response(Items: items);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com lista de itens de frescura por fonte de ingestão.</summary>
    public sealed record Response(IReadOnlyList<FreshnessItem> Items);

    /// <summary>DTO de frescura de uma fonte de ingestão.</summary>
    public sealed record FreshnessItem(
        string Domain,
        string ConnectorName,
        string SourceType,
        string Freshness,
        DateTimeOffset? LastReceivedAt,
        long LagMinutes,
        string TrustLevel,
        string Status);
}
