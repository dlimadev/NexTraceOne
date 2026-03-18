using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

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
    public sealed class Handler(ITeamRepository teamRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var teams = await teamRepository.ListAsync(status: null, cancellationToken);

            var dtos = teams.Select(t => new TeamSummaryDto(
                TeamId: t.Id.Value.ToString(),
                Name: t.Name,
                DisplayName: t.DisplayName,
                Description: t.Description,
                Status: t.Status.ToString(),
                ServiceCount: 0,    // TODO: enriquecer com contagem real de serviços
                ContractCount: 0,   // TODO: enriquecer com contagem real de contratos
                MemberCount: 0,     // TODO: enriquecer com contagem real de membros
                MaturityLevel: "Developing", // TODO: implementar cálculo de maturidade
                ParentOrganizationUnit: t.ParentOrganizationUnit
            )).ToList();

            var response = new Response(Teams: dtos);

            return Result<Response>.Success(response);
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
