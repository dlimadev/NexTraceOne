using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ComputeTeamHealth;

/// <summary>
/// Feature: ComputeTeamHealth — calcula ou recomputa o snapshot de saúde de uma equipa.
/// Se já existir um snapshot para a equipa, executa recomputação.
/// Se não existir, cria novo snapshot.
/// O OverallScore é calculado automaticamente como média das 7 dimensões.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — visibilidade holística de saúde por equipa.
/// </summary>
public static class ComputeTeamHealth
{
    /// <summary>Comando para calcular ou recomputar a saúde de uma equipa.</summary>
    public sealed record Command(
        Guid TeamId,
        string TeamName,
        int ServiceCountScore,
        int ContractHealthScore,
        int IncidentFrequencyScore,
        int MttrScore,
        int TechDebtScore,
        int DocCoverageScore,
        int PolicyComplianceScore,
        string? DimensionDetails = null,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Validação do comando de saúde de equipa.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty();
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceCountScore).InclusiveBetween(0, 100);
            RuleFor(x => x.ContractHealthScore).InclusiveBetween(0, 100);
            RuleFor(x => x.IncidentFrequencyScore).InclusiveBetween(0, 100);
            RuleFor(x => x.MttrScore).InclusiveBetween(0, 100);
            RuleFor(x => x.TechDebtScore).InclusiveBetween(0, 100);
            RuleFor(x => x.DocCoverageScore).InclusiveBetween(0, 100);
            RuleFor(x => x.PolicyComplianceScore).InclusiveBetween(0, 100);
        }
    }

    /// <summary>Handler que cria ou recomputa a saúde de uma equipa.</summary>
    public sealed class Handler(
        ITeamHealthSnapshotRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var existing = await repository.GetByTeamIdAsync(request.TeamId, cancellationToken);

            if (existing is not null)
            {
                existing.Recompute(
                    serviceCountScore: request.ServiceCountScore,
                    contractHealthScore: request.ContractHealthScore,
                    incidentFrequencyScore: request.IncidentFrequencyScore,
                    mttrScore: request.MttrScore,
                    techDebtScore: request.TechDebtScore,
                    docCoverageScore: request.DocCoverageScore,
                    policyComplianceScore: request.PolicyComplianceScore,
                    dimensionDetails: request.DimensionDetails,
                    now: now);

                await repository.UpdateAsync(existing, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result<Response>.Success(new Response(
                    SnapshotId: existing.Id.Value,
                    TeamId: existing.TeamId,
                    TeamName: existing.TeamName,
                    OverallScore: existing.OverallScore,
                    IsRecomputation: true,
                    AssessedAt: existing.AssessedAt));
            }

            var snapshot = TeamHealthSnapshot.Compute(
                teamId: request.TeamId,
                teamName: request.TeamName,
                serviceCountScore: request.ServiceCountScore,
                contractHealthScore: request.ContractHealthScore,
                incidentFrequencyScore: request.IncidentFrequencyScore,
                mttrScore: request.MttrScore,
                techDebtScore: request.TechDebtScore,
                docCoverageScore: request.DocCoverageScore,
                policyComplianceScore: request.PolicyComplianceScore,
                dimensionDetails: request.DimensionDetails,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(snapshot, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                SnapshotId: snapshot.Id.Value,
                TeamId: snapshot.TeamId,
                TeamName: snapshot.TeamName,
                OverallScore: snapshot.OverallScore,
                IsRecomputation: false,
                AssessedAt: snapshot.AssessedAt));
        }
    }

    /// <summary>Resposta com o resultado do cálculo de saúde da equipa.</summary>
    public sealed record Response(
        Guid SnapshotId,
        Guid TeamId,
        string TeamName,
        int OverallScore,
        bool IsRecomputation,
        DateTimeOffset AssessedAt);
}
