using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ListDomains;

/// <summary>
/// Feature: ListDomains — lista sumária de todos os domínios de negócio registados.
/// Inclui contadores de equipas, serviços e contratos para visão geral de governança por domínio.
/// </summary>
public static class ListDomains
{
    /// <summary>Query para listar todos os domínios.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna lista de domínios com indicadores sumários.</summary>
    public sealed class Handler(
        IGovernanceDomainRepository domainRepository,
        ITeamDomainLinkRepository teamDomainLinkRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var domains = await domainRepository.ListAsync(criticality: null, cancellationToken);

            var dtos = new List<DomainSummaryDto>();
            foreach (var d in domains)
            {
                var links = await teamDomainLinkRepository.ListByDomainIdAsync(d.Id, cancellationToken);
                dtos.Add(new DomainSummaryDto(
                    DomainId: d.Id.Value.ToString(),
                    Name: d.Name,
                    DisplayName: d.DisplayName,
                    Description: d.Description,
                    Criticality: d.Criticality.ToString(),
                    TeamCount: links.Count,
                    ServiceCount: 0,    // Cross-module: requires Catalog integration
                    ContractCount: 0,   // Cross-module: requires Catalog integration
                    MaturityLevel: "Developing",
                    CapabilityClassification: d.CapabilityClassification
                ));
            }

            var response = new Response(Domains: dtos);
            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com lista sumária de domínios.</summary>
    public sealed record Response(IReadOnlyList<DomainSummaryDto> Domains);

    /// <summary>DTO sumário de domínio com indicadores de governança.</summary>
    public sealed record DomainSummaryDto(
        string DomainId,
        string Name,
        string DisplayName,
        string? Description,
        string Criticality,
        int TeamCount,
        int ServiceCount,
        int ContractCount,
        string MaturityLevel,
        string? CapabilityClassification);
}
