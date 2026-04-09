using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.GetTeamHealthSnapshot;

/// <summary>
/// Feature: GetTeamHealthSnapshot — obtém o snapshot de saúde de uma equipa pelo TeamId.
/// Retorna todas as dimensões, scores e o OverallScore composto.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — consulta de saúde por equipa.
/// </summary>
public static class GetTeamHealthSnapshot
{
    /// <summary>Query para obter o snapshot de saúde de uma equipa.</summary>
    public sealed record Query(Guid TeamId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém o snapshot de saúde de uma equipa.</summary>
    public sealed class Handler(
        ITeamHealthSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var snapshot = await repository.GetByTeamIdAsync(request.TeamId, cancellationToken);

            if (snapshot is null)
                return GovernanceTeamHealthErrors.SnapshotNotFoundForTeam(request.TeamId.ToString());

            return Result<Response>.Success(new Response(
                SnapshotId: snapshot.Id.Value,
                TeamId: snapshot.TeamId,
                TeamName: snapshot.TeamName,
                OverallScore: snapshot.OverallScore,
                ServiceCountScore: snapshot.ServiceCountScore,
                ContractHealthScore: snapshot.ContractHealthScore,
                IncidentFrequencyScore: snapshot.IncidentFrequencyScore,
                MttrScore: snapshot.MttrScore,
                TechDebtScore: snapshot.TechDebtScore,
                DocCoverageScore: snapshot.DocCoverageScore,
                PolicyComplianceScore: snapshot.PolicyComplianceScore,
                DimensionDetails: snapshot.DimensionDetails,
                AssessedAt: snapshot.AssessedAt));
        }
    }

    /// <summary>Resposta com todos os scores e detalhes de saúde da equipa.</summary>
    public sealed record Response(
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
        string? DimensionDetails,
        DateTimeOffset AssessedAt);
}
