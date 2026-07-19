using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Maturity;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityDashboard;

/// <summary>
/// Feature: GetServiceMaturityDashboard — dashboard de maturidade para todos os serviços.
/// Calcula scorecard de maturidade para cada serviço e apresenta resumo agregado.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceMaturityDashboard
{
    /// <summary>Query de dashboard de maturidade. Filtros opcionais por equipa e domínio.</summary>
    public sealed record Query(
        string? TeamName = null,
        string? Domain = null) : IQuery<Response>;

    /// <summary>Validador da query GetServiceMaturityDashboard.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
        }
    }

    /// <summary>Handler que computa maturidade de todos os serviços filtrados.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceMaturityCalculator calculator) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (services, _) = await serviceAssetRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                serviceType: null,
                criticality: null,
                lifecycleStatus: null,
                exposureType: null,
                searchTerm: null,
                page: 1,
                pageSize: 10_000,
                cancellationToken);

            // Batch — elimina o N+1 do loop anterior
            var maturityByService = await calculator.ComputeForServicesAsync(services, cancellationToken);

            var scorecards = services.Select(service =>
            {
                var m = maturityByService[service.Id.Value];
                return new ServiceMaturityItemDto(
                    ServiceId: service.Id.Value,
                    ServiceName: service.Name,
                    DisplayName: service.DisplayName,
                    TeamName: service.TeamName,
                    Domain: service.Domain,
                    Criticality: service.Criticality.ToString(),
                    LifecycleStatus: service.LifecycleStatus.ToString(),
                    Level: m.Level,
                    OverallScore: m.OverallScore,
                    HasOwnership: m.HasOwnership,
                    HasContracts: m.HasContracts,
                    HasDocumentation: m.HasDocumentation,
                    HasRunbook: m.HasRunbook,
                    HasMonitoring: m.HasMonitoring,
                    HasRepository: m.HasRepository,
                    ApiCount: m.ApiCount,
                    ContractCount: m.ContractCount,
                    LinkCount: m.LinkCount);
            }).ToList();

            var ordered = scorecards.OrderBy(s => s.OverallScore).ToList();

            // Summary
            var total = ordered.Count;
            var optimizing = ordered.Count(s => s.Level == "Optimizing");
            var managed = ordered.Count(s => s.Level == "Managed");
            var defined = ordered.Count(s => s.Level == "Defined");
            var developing = ordered.Count(s => s.Level == "Developing");
            var initial = ordered.Count(s => s.Level == "Initial");
            var avgScore = total > 0 ? Math.Round(ordered.Average(s => s.OverallScore), 2) : 0m;

            var withoutOwnership = ordered.Count(s => !s.HasOwnership);
            var withoutContracts = ordered.Count(s => !s.HasContracts && s.ApiCount > 0);
            var withoutDocs = ordered.Count(s => !s.HasDocumentation);
            var withoutRunbooks = ordered.Count(s => !s.HasRunbook);
            var withoutMonitoring = ordered.Count(s => !s.HasMonitoring);

            return new Response(
                Summary: new DashboardSummaryDto(
                    TotalServices: total,
                    AverageScore: avgScore,
                    Optimizing: optimizing,
                    Managed: managed,
                    Defined: defined,
                    Developing: developing,
                    Initial: initial,
                    WithoutOwnership: withoutOwnership,
                    WithoutContracts: withoutContracts,
                    WithoutDocumentation: withoutDocs,
                    WithoutRunbooks: withoutRunbooks,
                    WithoutMonitoring: withoutMonitoring),
                Services: ordered,
                ComputedAt: DateTimeOffset.UtcNow);
        }

    }

    /// <summary>Resposta do dashboard de maturidade.</summary>
    public sealed record Response(
        DashboardSummaryDto Summary,
        IReadOnlyList<ServiceMaturityItemDto> Services,
        DateTimeOffset ComputedAt);

    /// <summary>Resumo agregado de maturidade dos serviços.</summary>
    public sealed record DashboardSummaryDto(
        int TotalServices,
        decimal AverageScore,
        int Optimizing,
        int Managed,
        int Defined,
        int Developing,
        int Initial,
        int WithoutOwnership,
        int WithoutContracts,
        int WithoutDocumentation,
        int WithoutRunbooks,
        int WithoutMonitoring);

    /// <summary>Item de maturidade de um serviço para o dashboard.</summary>
    public sealed record ServiceMaturityItemDto(
        Guid ServiceId,
        string ServiceName,
        string DisplayName,
        string TeamName,
        string Domain,
        string Criticality,
        string LifecycleStatus,
        string Level,
        decimal OverallScore,
        bool HasOwnership,
        bool HasContracts,
        bool HasDocumentation,
        bool HasRunbook,
        bool HasMonitoring,
        bool HasRepository,
        int ApiCount,
        int ContractCount,
        int LinkCount);
}
