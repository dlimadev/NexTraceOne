using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetChangeRiskPrediction;

/// <summary>
/// Feature: GetChangeRiskPrediction — avalia o risco de uma mudança antes da promoção
/// para produção, considerando histórico de incidentes, blast radius, evidências e timing.
/// Computação pura — não persiste dados.
/// </summary>
public static class GetChangeRiskPrediction
{
    public sealed record Query(
        Guid ChangeId,
        string ServiceId,
        string Environment,
        int PriorIncidentRate,
        decimal BlastRadius,
        bool HasTestEvidence,
        bool IsBusinessHours,
        string ChangeType) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PriorIncidentRate).GreaterThanOrEqualTo(0);
            RuleFor(x => x.BlastRadius).InclusiveBetween(0m, 100m);
            RuleFor(x => x.ChangeType).NotEmpty().MaximumLength(50);
        }
    }

    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var historicalRisk = Math.Min(request.PriorIncidentRate * 5m, 30m);
            var blastRisk = request.BlastRadius * 0.25m;
            var testEvidence = request.HasTestEvidence ? -10m : 5m;
            var timingRisk = request.IsBusinessHours ? 5m : 0m;
            var typeMultiplier = request.ChangeType switch
            {
                "Major" => 1.3m,
                "Breaking" => 1.5m,
                "Hotfix" => 1.1m,
                _ => 1.0m
            };

            var baseScore = historicalRisk + blastRisk + testEvidence + timingRisk;
            var finalScore = Math.Min(Math.Max(baseScore * typeMultiplier, 0m), 100m);
            var riskLevel = finalScore < 25m ? "Low" : finalScore < 50m ? "Medium" : finalScore < 75m ? "High" : "Critical";

            var riskFactors = new List<string>();
            if (historicalRisk > 0m) riskFactors.Add($"Historical incident rate: {request.PriorIncidentRate} incidents");
            if (blastRisk > 0m) riskFactors.Add($"Blast radius: {request.BlastRadius:F0}% of services affected");
            if (!request.HasTestEvidence) riskFactors.Add("No test evidence provided");
            if (request.IsBusinessHours) riskFactors.Add("Deploying during business hours");
            if (request.ChangeType is "Major" or "Breaking") riskFactors.Add($"High-risk change type: {request.ChangeType}");

            var recommendations = new List<string>();
            if (riskLevel is "High" or "Critical")
            {
                recommendations.Add("Require explicit approval from tech lead.");
                recommendations.Add("Prepare and validate rollback plan before deployment.");
            }
            if (!request.HasTestEvidence) recommendations.Add("Add automated test evidence before proceeding.");
            if (request.IsBusinessHours) recommendations.Add("Consider scheduling outside business hours.");
            if (recommendations.Count == 0) recommendations.Add("Proceed with standard deployment checklist.");

            return Task.FromResult(Result<Response>.Success(new Response(
                request.ChangeId,
                request.ServiceId,
                finalScore,
                riskLevel,
                riskFactors,
                recommendations,
                clock.UtcNow)));
        }
    }

    public sealed record Response(
        Guid ChangeId,
        string ServiceId,
        decimal RiskScore,
        string RiskLevel,
        IReadOnlyList<string> RiskFactors,
        IReadOnlyList<string> Recommendations,
        DateTimeOffset AssessedAt);
}
