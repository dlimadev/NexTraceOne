using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetApiUsageAnalytics;

/// <summary>Feature: GetApiUsageAnalytics — analytics de uso de APIs por consumidor, versão ou API.</summary>
public static class GetApiUsageAnalytics
{
    public sealed record Query(
        Guid? ApiAssetId,
        Guid? ConsumerId,
        string? ApiVersion,
        int DaysBack = 30) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DaysBack).InclusiveBetween(1, 365);
            RuleFor(x => x)
                .Must(x => x.ApiAssetId.HasValue || x.ConsumerId.HasValue || x.ApiVersion != null)
                .WithMessage("At least one filter (ApiAssetId, ConsumerId, ApiVersion) must be provided.");
        }
    }

    public sealed class Handler(
        IPortalAnalyticsRepository analytics,
        IApiKeyRepository apiKeyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var since = DateTimeOffset.UtcNow.AddDays(-request.DaysBack);

            int totalRequests = 0;
            int uniqueConsumers = 0;
            int activeApiKeys = 0;
            var topApis = new List<TopApiDto>();

            if (request.ApiAssetId.HasValue)
            {
                totalRequests = await analytics.CountByApiAssetAsync(request.ApiAssetId.Value, since, cancellationToken);
                uniqueConsumers = await analytics.CountDistinctConsumersByApiAsync(request.ApiAssetId.Value, since, cancellationToken);
            }

            if (request.ConsumerId.HasValue)
            {
                activeApiKeys = await apiKeyRepository.CountActiveByOwnerAsync(request.ConsumerId.Value, cancellationToken);
            }

            if (!request.ApiAssetId.HasValue)
            {
                var topRaw = await analytics.GetTopApisByViewsAsync(5, since, cancellationToken);
                topApis = topRaw.Select(t => new TopApiDto(t.ApiAssetId, t.ApiAssetId.ToString(), t.Count)).ToList();
            }

            return new Response(
                totalRequests,
                uniqueConsumers,
                activeApiKeys,
                topApis,
                [],
                request.DaysBack);
        }
    }

    public sealed record Response(
        int TotalRequests,
        int UniqueConsumers,
        int ActiveApiKeys,
        IReadOnlyList<TopApiDto> TopApis,
        IReadOnlyList<ConsumerBreakdownDto> ConsumerBreakdown,
        int DaysBack);

    public sealed record TopApiDto(Guid ApiAssetId, string ApiName, int RequestCount);
    public sealed record ConsumerBreakdownDto(Guid ConsumerId, string ConsumerService, int RequestCount);
}
