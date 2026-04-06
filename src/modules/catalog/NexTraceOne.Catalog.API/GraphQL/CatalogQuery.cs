using HotChocolate.Types;
using HotChocolate;
using MediatR;
using NexTraceOne.Catalog.API.GraphQL.Types;
using NexTraceOne.Catalog.Application.Contracts.Features.ListContractsByService;
using NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperNpsSummary;
using NexTraceOne.Catalog.Application.Graph.Features.ListServices;
using DomainCriticality = NexTraceOne.Catalog.Domain.Graph.Enums.Criticality;
using DomainServiceType = NexTraceOne.Catalog.Domain.Graph.Enums.ServiceType;

namespace NexTraceOne.Catalog.API.GraphQL;

/// <summary>
/// Root query do GraphQL Federation Gateway do NexTraceOne.
/// Expõe entidades centrais do catálogo: serviços, contratos e NPS.
/// Delega execução ao mediator via features do módulo Catalog.
/// Usa [ExtendObjectType] para que outros módulos possam adicionar os seus próprios campos ao tipo Query raiz.
/// Persona: Engineer, Tech Lead, Architect, Executive.
/// </summary>
[ExtendObjectType("Query")]
public sealed class CatalogQuery
{
    /// <summary>
    /// Retorna lista de serviços do catálogo com filtros opcionais.
    /// Suporta filtros por domínio, equipa, criticidade, tipo de serviço e pesquisa textual.
    /// </summary>
    public async Task<IReadOnlyList<ServiceType>> GetServicesAsync(
        [Service] IMediator mediator,
        string? searchTerm = null,
        string? domain = null,
        string? teamName = null,
        string? criticality = null,
        string? serviceType = null,
        CancellationToken cancellationToken = default)
    {
        DomainCriticality? criticalityFilter = null;
        if (!string.IsNullOrWhiteSpace(criticality) &&
            Enum.TryParse<DomainCriticality>(criticality, ignoreCase: true, out var parsedCriticality))
        {
            criticalityFilter = parsedCriticality;
        }

        DomainServiceType? serviceTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(serviceType) &&
            Enum.TryParse<DomainServiceType>(serviceType, ignoreCase: true, out var parsedServiceType))
        {
            serviceTypeFilter = parsedServiceType;
        }

        var query = new ListServices.Query(
            TeamName: teamName,
            Domain: domain,
            ServiceType: serviceTypeFilter,
            Criticality: criticalityFilter,
            LifecycleStatus: null,
            ExposureType: null,
            SearchTerm: searchTerm);

        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return [];

        return result.Value.Items
            .Select(s => new ServiceType
            {
                ServiceId = s.ServiceId,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                ServiceKind = s.ServiceType,
                Domain = s.Domain,
                SystemArea = s.SystemArea,
                TeamName = s.TeamName,
                TechnicalOwner = s.TechnicalOwner,
                Criticality = s.Criticality,
                LifecycleStatus = s.LifecycleStatus,
                ExposureType = s.ExposureType
            })
            .ToArray();
    }

    /// <summary>
    /// Retorna contratos de um serviço específico.
    /// Inclui protocolo, versão semântica, estado de ciclo de vida e ownership.
    /// </summary>
    public async Task<IReadOnlyList<ContractSummaryType>> GetContractsAsync(
        [Service] IMediator mediator,
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var query = new ListContractsByService.Query(ServiceId: serviceId);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return [];

        return result.Value.Contracts
            .Select(c => new ContractSummaryType
            {
                VersionId = c.VersionId,
                ApiAssetId = c.ApiAssetId,
                ServiceId = serviceId,
                ApiName = c.ApiName,
                ApiRoutePattern = c.ApiRoutePattern,
                SemVer = c.SemVer,
                Protocol = c.Protocol,
                LifecycleState = c.LifecycleState,
                IsLocked = c.IsLocked,
                CreatedAt = c.CreatedAt
            })
            .ToArray();
    }

    /// <summary>
    /// Retorna sumário de NPS de developer experience de uma equipa.
    /// Consolida NPS, satisfação com ferramentas/processos/plataforma e distribuição promotores/passivos/detratores.
    /// </summary>
    public async Task<NpsSummaryType?> GetNpsSummaryAsync(
        [Service] IMediator mediator,
        string teamId,
        string? period = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDeveloperNpsSummary.Query(TeamId: teamId, Period: period);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return null;

        var r = result.Value;
        return new NpsSummaryType
        {
            TeamId = r.TeamId,
            Period = r.Period,
            TotalResponses = r.TotalResponses,
            NpsScore = r.NpsScore,
            PromoterPercent = r.PromoterPercent,
            PassivePercent = r.PassivePercent,
            DetractorPercent = r.DetractorPercent,
            PromoterCount = r.PromoterCount,
            PassiveCount = r.PassiveCount,
            DetractorCount = r.DetractorCount,
            AvgToolSatisfaction = r.AvgToolSatisfaction,
            AvgProcessSatisfaction = r.AvgProcessSatisfaction,
            AvgPlatformSatisfaction = r.AvgPlatformSatisfaction
        };
    }
}
