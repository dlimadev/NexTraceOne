using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Application.Features.ListJourneyDefinitions;

/// <summary>
/// Lista as definições de jornadas configuráveis activas para o tenant corrente.
/// Funde as definições globais com as específicas do tenant (tenant tem prioridade por key).
/// </summary>
public static class ListJourneyDefinitions
{
    /// <summary>Query para listar definições de jornadas.</summary>
    public sealed record Query(bool IncludeGlobal = true) : IQuery<Response>;

    /// <summary>Handler que retorna todas as definições activas.</summary>
    public sealed class Handler(
        IJourneyDefinitionRepository repository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var definitions = await repository.ListActiveAsync(tenant.Id, cancellationToken);

            var dtos = definitions
                .Select(d => new JourneyDefinitionDto(
                    d.Id.Value,
                    d.TenantId,
                    d.Key,
                    d.Name,
                    d.StepsJson,
                    d.IsActive,
                    d.CreatedAt,
                    d.UpdatedAt))
                .ToList();

            return new Response(dtos);
        }
    }

    /// <summary>Resposta com lista de definições de jornadas.</summary>
    public sealed record Response(IReadOnlyList<JourneyDefinitionDto> Definitions);

    /// <summary>DTO para uma definição de jornada.</summary>
    public sealed record JourneyDefinitionDto(
        Guid Id,
        Guid? TenantId,
        string Key,
        string Name,
        string StepsJson,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
