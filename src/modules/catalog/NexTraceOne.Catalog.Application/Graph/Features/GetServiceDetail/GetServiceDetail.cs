using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceDetail;

/// <summary>
/// Feature: GetServiceDetail — obtém o detalhe completo de um serviço do catálogo.
/// Inclui identidade, ownership, classificação e resumos de contratos/APIs associados.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceDetail
{
    /// <summary>Query de detalhe do serviço pelo identificador.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o detalhe completo de um serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            // Obter APIs associadas a este serviço
            var allApis = await apiAssetRepository.ListAllAsync(cancellationToken);
            var serviceApis = allApis
                .Where(api => api.OwnerService.Id == service.Id)
                .ToList();

            var apiSummaries = serviceApis
                .Select(api => new ApiSummary(
                    api.Id.Value,
                    api.Name,
                    api.RoutePattern,
                    api.Version,
                    api.Visibility,
                    api.IsDecommissioned,
                    api.ConsumerRelationships.Count))
                .ToList();

            var totalConsumers = serviceApis
                .SelectMany(api => api.ConsumerRelationships)
                .Select(rel => rel.ConsumerName)
                .Distinct()
                .Count();

            return new Response(
                service.Id.Value,
                service.Name,
                service.DisplayName,
                service.Description,
                service.ServiceType.ToString(),
                service.Domain,
                service.SystemArea,
                service.TeamName,
                service.TechnicalOwner,
                service.BusinessOwner,
                service.Criticality.ToString(),
                service.LifecycleStatus.ToString(),
                service.ExposureType.ToString(),
                service.DocumentationUrl,
                service.RepositoryUrl,
                apiSummaries,
                apiSummaries.Count,
                totalConsumers);
        }
    }

    /// <summary>Resposta do detalhe completo de um serviço do catálogo.</summary>
    public sealed record Response(
        Guid ServiceId,
        string Name,
        string DisplayName,
        string Description,
        string ServiceType,
        string Domain,
        string SystemArea,
        string TeamName,
        string TechnicalOwner,
        string BusinessOwner,
        string Criticality,
        string LifecycleStatus,
        string ExposureType,
        string DocumentationUrl,
        string RepositoryUrl,
        IReadOnlyList<ApiSummary> Apis,
        int ApiCount,
        int TotalConsumers);

    /// <summary>Resumo de uma API associada ao serviço.</summary>
    public sealed record ApiSummary(
        Guid ApiId,
        string Name,
        string RoutePattern,
        string Version,
        string Visibility,
        bool IsDecommissioned,
        int ConsumerCount);
}
