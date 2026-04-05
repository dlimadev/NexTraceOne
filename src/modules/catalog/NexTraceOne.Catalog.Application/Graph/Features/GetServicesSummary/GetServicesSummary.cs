using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServicesSummary;

/// <summary>
/// Feature: GetServicesSummary — obtém resumos agregados de serviços por equipa ou domínio.
/// Útil para dashboards de Tech Lead e Executive.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class GetServicesSummary
{
    /// <summary>Query de resumo agregado de serviços (por equipa ou domínio).</summary>
    public sealed record Query(string? TeamName, string? Domain) : IQuery<Response>;

    /// <summary>Validador da query GetServicesSummary.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
        }
    }

    /// <summary>Handler que calcula resumos agregados de serviços.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var services = await serviceAssetRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                serviceType: null,
                criticality: null,
                lifecycleStatus: null,
                exposureType: null,
                searchTerm: null,
                cancellationToken);

            var totalCount = services.Count;
            var criticalCount = services.Count(s => s.Criticality == Domain.Graph.Enums.Criticality.Critical);
            var highCount = services.Count(s => s.Criticality == Domain.Graph.Enums.Criticality.High);
            var activeCount = services.Count(s => s.LifecycleStatus == Domain.Graph.Enums.LifecycleStatus.Active);
            var deprecatedCount = services.Count(s =>
                s.LifecycleStatus == Domain.Graph.Enums.LifecycleStatus.Deprecated ||
                s.LifecycleStatus == Domain.Graph.Enums.LifecycleStatus.Deprecating);
            var retiredCount = services.Count(s => s.LifecycleStatus == Domain.Graph.Enums.LifecycleStatus.Retired);

            var byServiceType = services
                .GroupBy(s => s.ServiceType.ToString())
                .Select(g => new GroupCount(g.Key, g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();

            var byCriticality = services
                .GroupBy(s => s.Criticality.ToString())
                .Select(g => new GroupCount(g.Key, g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();

            var byLifecycle = services
                .GroupBy(s => s.LifecycleStatus.ToString())
                .Select(g => new GroupCount(g.Key, g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();

            var byDomain = services
                .GroupBy(s => s.Domain)
                .Select(g => new GroupCount(g.Key, g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();

            var byTeam = services
                .GroupBy(s => s.TeamName)
                .Select(g => new GroupCount(g.Key, g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();

            return new Response(
                totalCount,
                criticalCount,
                highCount,
                activeCount,
                deprecatedCount,
                retiredCount,
                byServiceType,
                byCriticality,
                byLifecycle,
                byDomain,
                byTeam);
        }
    }

    /// <summary>Resposta com resumos agregados de serviços.</summary>
    public sealed record Response(
        int TotalCount,
        int CriticalCount,
        int HighCriticalityCount,
        int ActiveCount,
        int DeprecatedCount,
        int RetiredCount,
        IReadOnlyList<GroupCount> ByServiceType,
        IReadOnlyList<GroupCount> ByCriticality,
        IReadOnlyList<GroupCount> ByLifecycle,
        IReadOnlyList<GroupCount> ByDomain,
        IReadOnlyList<GroupCount> ByTeam);

    /// <summary>Contagem agrupada por categoria.</summary>
    public sealed record GroupCount(string Key, int Count);
}
