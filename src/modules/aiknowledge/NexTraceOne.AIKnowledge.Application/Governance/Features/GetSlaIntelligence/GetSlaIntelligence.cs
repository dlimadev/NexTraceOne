using FluentValidation;
using NexTraceOne.AIKnowledge.Domain.Governance.ValueObjects;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetSlaIntelligence;

/// <summary>
/// Feature: GetSlaIntelligence — analisa SLAs, causas de downtime e recomendações de melhoria.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetSlaIntelligence
{
    public sealed record Query(
        string ServiceName,
        Guid TenantId,
        double CurrentSlaTarget,
        double ActualAvailabilityPercent,
        int MaintenanceWindowMinutesPerMonth,
        int DeploymentFailuresLast12m,
        int FridayDeployCount,
        decimal EstimatedPenaltyPerBreachMonth) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.CurrentSlaTarget).InclusiveBetween(90, 100);
            RuleFor(x => x.ActualAvailabilityPercent).InclusiveBetween(0, 100);
        }
    }

    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            bool inBreach = request.ActualAvailabilityPercent < request.CurrentSlaTarget;
            double recommendedSla = Math.Round(request.ActualAvailabilityPercent - 0.2, 2);

            var causes = new List<SlaDowntimeCause>();
            double totalDowntimeMinutes = (100 - request.ActualAvailabilityPercent) / 100.0 * 43800;

            if (totalDowntimeMinutes > 0)
            {
                double maintPct = Math.Min(60, request.MaintenanceWindowMinutesPerMonth * 12 / totalDowntimeMinutes * 100);
                causes.Add(new SlaDowntimeCause("Maintenance Windows", maintPct, "Deployment and planned maintenance downtime", true));

                double deployPct = Math.Min(30, request.DeploymentFailuresLast12m * 15.0 / totalDowntimeMinutes * 100);
                causes.Add(new SlaDowntimeCause("Deployment Failures", deployPct, $"{request.DeploymentFailuresLast12m} failed deployments in 12m", true));

                double otherPct = Math.Max(0, 100 - maintPct - deployPct);
                causes.Add(new SlaDowntimeCause("External Dependencies", otherPct, "Downstream service unavailability", false));
            }

            var improvements = new List<string>();
            if (request.MaintenanceWindowMinutesPerMonth > 30)
                improvements.Add("Implement blue-green deployments to eliminate maintenance windows");
            if (request.FridayDeployCount > 2)
                improvements.Add($"Deploy freeze policy on Fridays ({request.FridayDeployCount} friday deploys in period)");
            if (request.DeploymentFailuresLast12m > 3)
                improvements.Add("Add circuit breakers for external dependencies");

            int breachMonths = inBreach ? (int)Math.Round((request.CurrentSlaTarget - request.ActualAvailabilityPercent) * 12) : 0;
            decimal totalPenalty = breachMonths * request.EstimatedPenaltyPerBreachMonth;

            return Task.FromResult(Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                CurrentSlaTarget: request.CurrentSlaTarget,
                ActualAvailability: request.ActualAvailabilityPercent,
                IsInBreach: inBreach,
                RecommendedSla: recommendedSla,
                DowntimeCauses: causes,
                ImprovementsNeeded: improvements,
                EstimatedBreachCostAnnual: totalPenalty,
                AnalysedAt: clock.UtcNow)));
        }
    }

    public sealed record Response(
        string ServiceName,
        double CurrentSlaTarget,
        double ActualAvailability,
        bool IsInBreach,
        double RecommendedSla,
        IReadOnlyList<SlaDowntimeCause> DowntimeCauses,
        IReadOnlyList<string> ImprovementsNeeded,
        decimal EstimatedBreachCostAnnual,
        DateTimeOffset AnalysedAt);
}
