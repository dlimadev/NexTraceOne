using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.AnalyzePredictivePatterns;

/// <summary>
/// Feature: AnalyzePredictivePatterns — regista um padrão preditivo de incidentes.
/// Cria padrão a partir de dados de análise histórica.
/// Marca padrões anteriores do mesmo serviço/ambiente como Stale quando aplicável.
/// </summary>
public static class AnalyzePredictivePatterns
{
    public sealed record Command(
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
        string? PreventionRecommendations = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidPatternTypes = Enum.GetNames<PredictionPatternType>();

        public Validator()
        {
            RuleFor(x => x.PatternName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.PatternType).NotEmpty()
                .Must(t => ValidPatternTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Valid pattern types: {string.Join(", ", ValidPatternTypes)}");
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ConfidencePercent).InclusiveBetween(0, 100);
            RuleFor(x => x.OccurrenceCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SampleSize).GreaterThanOrEqualTo(0);
            RuleFor(x => x).Must(x => x.OccurrenceCount <= x.SampleSize)
                .WithMessage("Occurrence count cannot exceed sample size.");
            RuleFor(x => x.Evidence).NotEmpty();
            RuleFor(x => x.TriggerConditions).NotEmpty();
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
        }
    }

    public sealed class Handler(
        IIncidentPredictionPatternRepository patternRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider,
        IReliabilityUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            if (!Enum.TryParse<PredictionPatternType>(request.PatternType, true, out var patternType))
                return Error.Validation("INVALID_PATTERN_TYPE", $"Invalid pattern type: {request.PatternType}");

            // Mark previous patterns for same service/environment as stale
            if (request.ServiceId is not null)
            {
                var previousPattern = await patternRepository.GetLatestByServiceAsync(
                    request.ServiceId, request.Environment, cancellationToken);

                if (previousPattern is not null && previousPattern.Status == PredictionPatternStatus.Detected)
                {
                    previousPattern.MarkAsStale();
                    patternRepository.Update(previousPattern);
                }
            }

            // Determine severity based on confidence and occurrence count
            var severity = request.ConfidencePercent >= 80
                ? PredictionSeverity.Critical
                : request.ConfidencePercent >= 60
                    ? PredictionSeverity.High
                    : request.ConfidencePercent >= 40
                        ? PredictionSeverity.Medium
                        : PredictionSeverity.Low;

            var pattern = IncidentPredictionPattern.Detect(
                request.PatternName,
                request.Description,
                patternType,
                request.ServiceId,
                request.ServiceName,
                request.Environment,
                request.ConfidencePercent,
                request.OccurrenceCount,
                request.SampleSize,
                request.Evidence,
                request.TriggerConditions,
                request.PreventionRecommendations,
                severity,
                currentTenant.Id,
                now);

            patternRepository.Add(pattern);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                pattern.Id.Value,
                pattern.PatternName,
                pattern.PatternType.ToString(),
                pattern.ServiceId,
                pattern.Environment,
                pattern.ConfidencePercent,
                pattern.OccurrenceCount,
                pattern.SampleSize,
                severity.ToString(),
                pattern.Status.ToString(),
                pattern.DetectedAt));
        }
    }

    public sealed record Response(
        Guid PatternId,
        string PatternName,
        string PatternType,
        string? ServiceId,
        string Environment,
        int ConfidencePercent,
        int OccurrenceCount,
        int SampleSize,
        string Severity,
        string Status,
        DateTimeOffset DetectedAt);
}
