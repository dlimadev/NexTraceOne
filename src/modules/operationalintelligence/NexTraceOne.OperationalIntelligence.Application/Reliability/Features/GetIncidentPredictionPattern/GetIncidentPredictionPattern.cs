using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetIncidentPredictionPattern;

/// <summary>
/// Feature: GetIncidentPredictionPattern — obtém um padrão preditivo por ID.
/// </summary>
public static class GetIncidentPredictionPattern
{
    public sealed record Query(Guid PatternId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PatternId).NotEmpty();
        }
    }

    public sealed class Handler(
        IIncidentPredictionPatternRepository patternRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var pattern = await patternRepository.GetByIdAsync(
                IncidentPredictionPatternId.From(request.PatternId), cancellationToken);

            if (pattern is null)
                return Error.NotFound("INCIDENT_PREDICTION_PATTERN_NOT_FOUND",
                    $"Incident prediction pattern '{request.PatternId}' not found.");

            return Result<Response>.Success(new Response(
                pattern.Id.Value,
                pattern.PatternName,
                pattern.Description,
                pattern.PatternType.ToString(),
                pattern.ServiceId,
                pattern.ServiceName,
                pattern.Environment,
                pattern.ConfidencePercent,
                pattern.OccurrenceCount,
                pattern.SampleSize,
                pattern.Evidence,
                pattern.TriggerConditions,
                pattern.PreventionRecommendations,
                pattern.Severity.ToString(),
                pattern.Status.ToString(),
                pattern.DetectedAt,
                pattern.ValidatedAt,
                pattern.ValidationComment));
        }
    }

    public sealed record Response(
        Guid PatternId,
        string PatternName,
        string Description,
        string PatternType,
        string? ServiceId,
        string? ServiceName,
        string Environment,
        int ConfidencePercent,
        int OccurrenceCount,
        int SampleSize,
        string Evidence,
        string TriggerConditions,
        string? PreventionRecommendations,
        string Severity,
        string Status,
        DateTimeOffset DetectedAt,
        DateTimeOffset? ValidatedAt,
        string? ValidationComment);
}
