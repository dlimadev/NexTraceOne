using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Maturity;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListServices;

/// <summary>
/// Feature: ListServices — lista serviços do catálogo com filtros opcionais.
/// Ponto de entrada principal para o catálogo de serviços do NexTraceOne.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class ListServices
{
    // Níveis de maturidade válidos para filtragem e validação.
    private static readonly HashSet<string> ValidMaturityLevels =
        ["Initial", "Developing", "Defined", "Managed", "Optimizing"];

    // Campos de ordenação válidos.
    private static readonly HashSet<string> ValidSortFields =
        ["name", "maturity"];

    /// <summary>Query de listagem filtrada de serviços do catálogo.</summary>
    public sealed record Query(
        string? TeamName,
        string? Domain,
        ServiceType? ServiceType,
        Criticality? Criticality,
        LifecycleStatus? LifecycleStatus,
        ExposureType? ExposureType,
        string? SearchTerm,
        int Page = 1,
        int PageSize = 50,
        string? MaturityLevel = null,
        string? SortBy = null,
        bool SortDescending = false) : IQuery<Response>;

    /// <summary>Validador da query ListServices.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
            RuleFor(x => x.SearchTerm).MaximumLength(200).When(x => x.SearchTerm is not null);
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.MaturityLevel)
                .Must(v => ValidMaturityLevels.Contains(v!))
                .When(x => x.MaturityLevel is not null)
                .WithMessage($"MaturityLevel deve ser um de: {string.Join(", ", ValidMaturityLevels)}.");
            RuleFor(x => x.SortBy)
                .Must(v => ValidSortFields.Contains(v!.ToLowerInvariant()))
                .When(x => x.SortBy is not null)
                .WithMessage($"SortBy deve ser um de: {string.Join(", ", ValidSortFields)}.");
        }
    }

    /// <summary>Handler que lista serviços com filtros opcionais.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceMaturityCalculator serviceMaturityCalculator) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Caminho rápido: paginação SQL quando maturidade não é necessária.
            var isMaturitySort = string.Equals(request.SortBy, "maturity", StringComparison.OrdinalIgnoreCase);
            if (request.MaturityLevel is null && !isMaturitySort)
            {
                var (services, totalCount) = await serviceAssetRepository.ListFilteredAsync(
                    request.TeamName,
                    request.Domain,
                    request.ServiceType,
                    request.Criticality,
                    request.LifecycleStatus,
                    request.ExposureType,
                    request.SearchTerm,
                    request.Page,
                    request.PageSize,
                    cancellationToken);

                return new Response(MapItems(services), totalCount);
            }

            // Caminho maturity: busca tudo (até 10 000), calcula, filtra, ordena, pagina em memória.
            var (allServices, _) = await serviceAssetRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                request.ServiceType,
                request.Criticality,
                request.LifecycleStatus,
                request.ExposureType,
                request.SearchTerm,
                page: 1,
                pageSize: 10_000,
                cancellationToken);

            var maturityByService = await serviceMaturityCalculator.ComputeForServicesAsync(
                allServices, cancellationToken);

            // Filtra por nível de maturidade quando especificado.
            var filtered = request.MaturityLevel is not null
                ? allServices
                    .Where(svc => maturityByService.TryGetValue(svc.Id.Value, out var m)
                                  && m.Level == request.MaturityLevel)
                    .ToList()
                : allServices.ToList();

            // Ordena por score de maturidade.
            IOrderedEnumerable<NexTraceOne.Catalog.Domain.Graph.Entities.ServiceAsset> ordered =
                request.SortDescending
                    ? filtered.OrderByDescending(svc =>
                        maturityByService.TryGetValue(svc.Id.Value, out var m) ? m.OverallScore : 0m)
                    : filtered.OrderBy(svc =>
                        maturityByService.TryGetValue(svc.Id.Value, out var m) ? m.OverallScore : 0m);

            var totalFiltered = filtered.Count;
            var paged = ordered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new Response(MapItems(paged), totalFiltered);
        }

        // Mapeamento comum de ServiceAsset para ServiceListItem.
        private static IReadOnlyList<ServiceListItem> MapItems(
            IEnumerable<NexTraceOne.Catalog.Domain.Graph.Entities.ServiceAsset> services)
            => services
                .Select(svc => new ServiceListItem(
                    svc.Id.Value,
                    svc.Name,
                    svc.DisplayName,
                    svc.Description,
                    svc.ServiceType.ToString(),
                    svc.Domain,
                    svc.SystemArea,
                    svc.TeamName,
                    svc.TechnicalOwner,
                    svc.Criticality.ToString(),
                    svc.LifecycleStatus.ToString(),
                    svc.ExposureType.ToString()))
                .ToList();
    }

    /// <summary>Resposta da listagem de serviços do catálogo.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceListItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um serviço na listagem do catálogo.</summary>
    public sealed record ServiceListItem(
        Guid ServiceId,
        string Name,
        string DisplayName,
        string Description,
        string ServiceType,
        string Domain,
        string SystemArea,
        string TeamName,
        string TechnicalOwner,
        string Criticality,
        string LifecycleStatus,
        string ExposureType);
}
