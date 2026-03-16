using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ListTeams;

/// <summary>
/// Feature: ListTeams — lista sumária de todas as equipas registadas na plataforma.
/// Inclui contadores de serviços, contratos e membros para visão geral de governança.
/// </summary>
public static class ListTeams
{
    /// <summary>Query para listar todas as equipas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna lista de equipas com indicadores sumários.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var teams = new List<TeamSummaryDto>
            {
                new("team-platform", "platform-squad", "Platform", "Equipa responsável pela infraestrutura e serviços transversais da plataforma.",
                    "Active", 8, 12, 6, "Managed", "Engineering"),
                new("team-commerce", "commerce-squad", "Commerce", "Equipa responsável pelos serviços de comércio eletrónico e pagamentos.",
                    "Active", 5, 7, 4, "Defined", "Product"),
                new("team-identity", "identity-squad", "Identity", "Equipa responsável pela autenticação, autorização e gestão de identidades.",
                    "Active", 3, 5, 3, "Managed", "Engineering"),
                new("team-data", "data-squad", "Data & Analytics", "Equipa responsável pela ingestão, transformação e análise de dados operacionais.",
                    "Active", 4, 6, 5, "Developing", "Data")
            };

            var response = new Response(Teams: teams);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista sumária de equipas.</summary>
    public sealed record Response(IReadOnlyList<TeamSummaryDto> Teams);

    /// <summary>DTO sumário de equipa com indicadores de governança.</summary>
    public sealed record TeamSummaryDto(
        string TeamId,
        string Name,
        string DisplayName,
        string? Description,
        string Status,
        int ServiceCount,
        int ContractCount,
        int MemberCount,
        string MaturityLevel,
        string? ParentOrganizationUnit);
}
