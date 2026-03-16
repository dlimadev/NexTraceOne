using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var domains = new List<DomainSummaryDto>
            {
                new("domain-commerce", "commerce", "Commerce", "Domínio de comércio eletrónico, pagamentos e gestão de encomendas.",
                    "Critical", 3, 5, 7, "Defined", "Revenue-Generating"),
                new("domain-platform", "platform", "Platform", "Domínio de serviços transversais de infraestrutura e plataforma.",
                    "High", 2, 8, 12, "Managed", "Enabling"),
                new("domain-identity", "identity", "Identity", "Domínio de autenticação, autorização e gestão de identidades.",
                    "High", 1, 3, 5, "Managed", "Supporting"),
                new("domain-data", "data-analytics", "Data & Analytics", "Domínio de ingestão, transformação e análise de dados operacionais.",
                    "Medium", 2, 4, 6, "Developing", "Enabling")
            };

            var response = new Response(Domains: domains);

            return Task.FromResult(Result<Response>.Success(response));
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
