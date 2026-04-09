using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListIncidentPredictionPatterns;

/// <summary>
/// Feature: ListIncidentPredictionPatterns — lista padrões preditivos com filtros opcionais.
/// </summary>
public static class ListIncidentPredictionPatterns
{
    public sealed record Query(
        string? Environment = null,
        string? Status = null,
        string? PatternType = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Environment).MaximumLength(100).When(x => x.Environment is not null);
            RuleFor(x => x.Status)
                .Must(s => s is null || Enum.TryParse<PredictionPatternStatus>(s, true, out _))
                .WithMessage("Invalid status. Valid values: Detected, Confirmed, Dismissed, Stale.");
            RuleFor(x => x.PatternType)
                .Must(t => t is null || Enum.TryParse<PredictionPatternType>(t, true, out _))
                .WithMessage("Invalid pattern type.");
        }
    }

    public sealed class Handler(
        IIncidentPredictionPatternRepository patternRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            PredictionPatternStatus? status = null;
            if (request.Status is not null && Enum.TryParse<PredictionPatternStatus>(request.Status, true, out var parsedStatus))
                status = parsedStatus;

            PredictionPatternType? patternType = null;
            if (request.PatternType is not null && Enum.TryParse<PredictionPatternType>(request.PatternType, true, out var parsedType))
                patternType = parsedType;

            var patterns = await patternRepository.ListAsync(
                request.Environment, status, patternType, cancellationToken);

            var items = patterns.Select(p => new PatternItem(
                p.Id.Value,
                p.PatternName,
                p.PatternType.ToString(),
                p.ServiceId,
                p.ServiceName,
                p.Environment,
                p.ConfidencePercent,
                p.OccurrenceCount,
                p.SampleSize,
                p.Severity.ToString(),
                p.Status.ToString(),
                p.DetectedAt)).ToList();

            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    public sealed record PatternItem(
        Guid PatternId,
        string PatternName,
        string PatternType,
        string? ServiceId,
        string? ServiceName,
        string Environment,
        int ConfidencePercent,
        int OccurrenceCount,
        int SampleSize,
        string Severity,
        string Status,
        DateTimeOffset DetectedAt);

    public sealed record Response(IReadOnlyList<PatternItem> Items, int TotalCount);
}
