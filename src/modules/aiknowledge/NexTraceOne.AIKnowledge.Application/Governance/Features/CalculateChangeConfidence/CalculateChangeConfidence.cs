using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CalculateChangeConfidence;

/// <summary>
/// Feature: CalculateChangeConfidence — calcula o score de confiança antes de um deployment.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CalculateChangeConfidence
{
    public sealed record Command(
        string ChangeId,
        string ServiceName,
        Guid TenantId,
        int AffectedServicesCount,
        double TestCoverageDelta,
        int IncidentCountLast30Days,
        bool IsWeekend,
        bool IsBusinessHours,
        int DeployerSuccessfulDeploysCount,
        int ChangedLinesCount,
        int DependenciesWithOpenIncidents,
        string CalculatedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChangeId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.CalculatedBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IAiChangeConfidenceRepository confidenceRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var blastRadiusScore = Math.Max(0, 1.0 - (request.AffectedServicesCount / 10.0));
            var testCoverageScore = Math.Clamp(0.5 + request.TestCoverageDelta / 20.0, 0, 1);
            var incidentHistoryScore = Math.Max(0, 1.0 - (request.IncidentCountLast30Days / 5.0));
            var timeOfDayScore = request.IsWeekend ? 0.2 : (request.IsBusinessHours ? 0.9 : 0.6);
            var deployerExperienceScore = Math.Min(1.0, request.DeployerSuccessfulDeploysCount / 50.0);
            var changeSizeScore = Math.Max(0, 1.0 - (request.ChangedLinesCount / 1000.0));
            var dependencyStabilityScore = Math.Max(0, 1.0 - (request.DependenciesWithOpenIncidents / 5.0));

            var scoreEntity = ChangeConfidenceScore.Calculate(
                request.ChangeId,
                request.ServiceName,
                request.TenantId,
                blastRadiusScore,
                testCoverageScore,
                incidentHistoryScore,
                timeOfDayScore,
                deployerExperienceScore,
                changeSizeScore,
                dependencyStabilityScore,
                request.CalculatedBy,
                DateTimeOffset.UtcNow);

            confidenceRepository.Add(scoreEntity);
            await unitOfWork.CommitAsync(ct);

            return new Response(
                scoreEntity.Id.Value,
                scoreEntity.ChangeId,
                scoreEntity.ServiceName,
                scoreEntity.Score,
                scoreEntity.Verdict,
                scoreEntity.BlastRadiusScore,
                scoreEntity.TestCoverageScore,
                scoreEntity.IncidentHistoryScore,
                scoreEntity.TimeOfDayScore,
                scoreEntity.DeployerExperienceScore,
                scoreEntity.ChangeSizeScore,
                scoreEntity.DependencyStabilityScore,
                scoreEntity.RecommendationText);
        }
    }

    public sealed record Response(
        Guid ScoreId,
        string ChangeId,
        string ServiceName,
        int Score,
        string Verdict,
        double BlastRadiusScore,
        double TestCoverageScore,
        double IncidentHistoryScore,
        double TimeOfDayScore,
        double DeployerExperienceScore,
        double ChangeSizeScore,
        double DependencyStabilityScore,
        string RecommendationText);
}
