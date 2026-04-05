using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetRateLimitPolicy;

/// <summary>Feature: GetRateLimitPolicy — obtém a política de rate limit para uma API.</summary>
public static class GetRateLimitPolicy
{
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    public sealed class Handler(IApiRateLimitPolicyRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var policy = await repository.GetByApiAssetIdAsync(request.ApiAssetId, cancellationToken);

            if (policy is null)
                return DeveloperPortalErrors.RateLimitPolicyNotFound(request.ApiAssetId);

            return new Response(
                policy.ApiAssetId,
                policy.RequestsPerMinute,
                policy.RequestsPerHour,
                policy.RequestsPerDay,
                policy.BurstLimit,
                policy.IsEnabled,
                policy.Notes,
                policy.CreatedAt,
                policy.UpdatedAt);
        }
    }

    public sealed record Response(
        Guid ApiAssetId,
        int RequestsPerMinute,
        int RequestsPerHour,
        int RequestsPerDay,
        int BurstLimit,
        bool IsEnabled,
        string? Notes,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
