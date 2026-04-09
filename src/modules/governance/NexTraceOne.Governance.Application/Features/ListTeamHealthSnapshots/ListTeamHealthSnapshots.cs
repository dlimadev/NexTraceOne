using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ListTeamHealthSnapshots;

/// <summary>
/// Feature: ListTeamHealthSnapshots — lista snapshots de saúde de equipas.
/// Suporta filtro opcional por score mínimo.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — visão panorâmica de saúde das equipas.
/// </summary>
public static class ListTeamHealthSnapshots
{
    /// <summary>Query para listar snapshots de saúde de equipas com filtro opcional de score mínimo.</summary>
    public sealed record Query(int? MinOverallScore = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.MinOverallScore)
                .InclusiveBetween(0, 100)
                .When(x => x.MinOverallScore.HasValue);
        }
    }

    /// <summary>Handler que lista snapshots de saúde com filtro opcional por score mínimo.</summary>
    public sealed class Handler(
        ITeamHealthSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var snapshots = await repository.ListAsync(request.MinOverallScore, cancellationToken);

            var items = snapshots
                .Select(s => new TeamHealthItemDto(
                    SnapshotId: s.Id.Value,
                    TeamId: s.TeamId,
                    TeamName: s.TeamName,
                    OverallScore: s.OverallScore,
                    ServiceCountScore: s.ServiceCountScore,
                    ContractHealthScore: s.ContractHealthScore,
                    IncidentFrequencyScore: s.IncidentFrequencyScore,
                    MttrScore: s.MttrScore,
                    TechDebtScore: s.TechDebtScore,
                    DocCoverageScore: s.DocCoverageScore,
                    PolicyComplianceScore: s.PolicyComplianceScore,
                    AssessedAt: s.AssessedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: items,
                TotalCount: items.Count,
                MinOverallScore: request.MinOverallScore));
        }
    }

    /// <summary>Resposta com a lista de snapshots de saúde das equipas.</summary>
    public sealed record Response(
        IReadOnlyList<TeamHealthItemDto> Items,
        int TotalCount,
        int? MinOverallScore);

    /// <summary>DTO resumido de um snapshot de saúde para listagem.</summary>
    public sealed record TeamHealthItemDto(
        Guid SnapshotId,
        Guid TeamId,
        string TeamName,
        int OverallScore,
        int ServiceCountScore,
        int ContractHealthScore,
        int IncidentFrequencyScore,
        int MttrScore,
        int TechDebtScore,
        int DocCoverageScore,
        int PolicyComplianceScore,
        DateTimeOffset AssessedAt);
}
