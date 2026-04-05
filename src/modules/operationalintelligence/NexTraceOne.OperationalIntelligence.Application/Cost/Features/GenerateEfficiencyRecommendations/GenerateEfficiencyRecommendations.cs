using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GenerateEfficiencyRecommendations;

public static class GenerateEfficiencyRecommendations
{
    public sealed record Command(
        string? Team,
        string? Domain,
        string Environment,
        string Period) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
        }
    }

    public sealed class Handler(
        ICostRecordRepository recordRepository,
        IEfficiencyRecommendationRepository recommendationRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var records = request.Team is not null
                ? await recordRepository.ListByTeamAsync(request.Team, request.Period, cancellationToken)
                : request.Domain is not null
                    ? await recordRepository.ListByDomainAsync(request.Domain, request.Period, cancellationToken)
                    : await recordRepository.ListByPeriodAsync(request.Period, cancellationToken);

            var filtered = records
                .Where(r => r.Environment == request.Environment || string.IsNullOrWhiteSpace(r.Environment))
                .ToList();

            var byService = filtered
                .GroupBy(r => new { r.ServiceId, r.ServiceName })
                .Select(g => new { g.Key.ServiceId, g.Key.ServiceName, TotalCost = g.Sum(r => r.TotalCost) })
                .OrderByDescending(s => s.TotalCost)
                .ToList();

            if (byService.Count == 0)
                return new Response([], 0, dateTimeProvider.UtcNow);

            var sortedCosts = byService.Select(s => s.TotalCost).OrderBy(c => c).ToList();
            var median = ComputeMedian(sortedCosts);
            var threshold = median * 1.40m;

            var context = request.Team is not null ? "team"
                : request.Domain is not null ? "domain"
                : "platform";

            var now = dateTimeProvider.UtcNow;
            var recommendations = new List<EfficiencyRecommendation>();
            var dtos = new List<RecommendationDto>();

            foreach (var svc in byService.Where(s => s.TotalCost > threshold))
            {
                var deviationPercent = median > 0
                    ? (svc.TotalCost - median) / median * 100m
                    : 0m;

                var recommendationText =
                    $"Service '{svc.ServiceName}' costs {deviationPercent:F1}% more than similar services in this {context}. " +
                    "Consider reviewing resource allocation, scaling policies, or inefficient endpoints.";

                var result = EfficiencyRecommendation.Create(
                    svc.ServiceId,
                    svc.ServiceName,
                    request.Environment,
                    svc.TotalCost,
                    median,
                    recommendationText,
                    "Compute",
                    now);

                if (result.IsSuccess)
                {
                    recommendations.Add(result.Value);
                    dtos.Add(new RecommendationDto(
                        result.Value.Id.Value,
                        result.Value.ServiceId,
                        result.Value.ServiceName,
                        result.Value.ServiceCost,
                        result.Value.MedianPeerCost,
                        result.Value.DeviationPercent,
                        result.Value.RecommendationText,
                        result.Value.Priority));
                }
            }

            if (recommendations.Count > 0)
            {
                recommendationRepository.AddRange(recommendations);
                await unitOfWork.CommitAsync(cancellationToken);
            }

            return new Response(dtos, byService.Count, now);
        }

        private static decimal ComputeMedian(List<decimal> sorted)
        {
            if (sorted.Count == 0) return 0m;
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2m
                : sorted[mid];
        }
    }

    public sealed record RecommendationDto(
        Guid Id,
        string ServiceId,
        string ServiceName,
        decimal ServiceCost,
        decimal MedianPeerCost,
        decimal DeviationPercent,
        string RecommendationText,
        string Priority);

    public sealed record Response(
        IReadOnlyList<RecommendationDto> Recommendations,
        int TotalAnalyzed,
        DateTimeOffset GeneratedAt);
}
