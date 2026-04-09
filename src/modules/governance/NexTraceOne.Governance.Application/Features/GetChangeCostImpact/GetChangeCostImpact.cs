using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.GetChangeCostImpact;

/// <summary>
/// Feature: GetChangeCostImpact — obtém o impacto de custo de uma mudança pelo ReleaseId.
/// Retorna todos os detalhes de custo e cálculos derivados.
///
/// Owner: módulo Governance — subdomínio FinOps.
/// Pilar: FinOps contextual — consulta de custo por release/mudança.
/// </summary>
public static class GetChangeCostImpact
{
    /// <summary>Query para obter o impacto de custo pelo ReleaseId.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém o impacto de custo de uma release.</summary>
    public sealed class Handler(
        IChangeCostImpactRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var impact = await repository.GetByReleaseIdAsync(request.ReleaseId, cancellationToken);

            if (impact is null)
                return GovernanceChangeCostErrors.ReleaseNotFound(request.ReleaseId.ToString());

            return Result<Response>.Success(new Response(
                ImpactId: impact.Id.Value,
                ReleaseId: impact.ReleaseId,
                ServiceName: impact.ServiceName,
                Environment: impact.Environment,
                ChangeDescription: impact.ChangeDescription,
                BaselineCostPerDay: impact.BaselineCostPerDay,
                ActualCostPerDay: impact.ActualCostPerDay,
                CostDelta: impact.CostDelta,
                CostDeltaPercentage: impact.CostDeltaPercentage,
                Direction: impact.Direction,
                CostProvider: impact.CostProvider,
                CostDetails: impact.CostDetails,
                MeasurementWindowStart: impact.MeasurementWindowStart,
                MeasurementWindowEnd: impact.MeasurementWindowEnd,
                RecordedAt: impact.RecordedAt));
        }
    }

    /// <summary>Resposta com todos os detalhes de impacto de custo de uma release.</summary>
    public sealed record Response(
        Guid ImpactId,
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string? ChangeDescription,
        decimal BaselineCostPerDay,
        decimal ActualCostPerDay,
        decimal CostDelta,
        decimal CostDeltaPercentage,
        CostChangeDirection Direction,
        string? CostProvider,
        string? CostDetails,
        DateTimeOffset MeasurementWindowStart,
        DateTimeOffset MeasurementWindowEnd,
        DateTimeOffset RecordedAt);
}
