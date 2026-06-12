using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.GetUserAiPreferences;

public static class GetUserAiPreferences
{
    public sealed record Query : IQuery<IReadOnlyList<Response>>;

    public sealed record Response(
        Guid Id,
        string FeatureKey,
        int PreferenceType,
        string? PreferredModelName,
        string? PreferredProviderId,
        int? ExternalProduct,
        string? ExternalProductModel,
        string? DisableReason,
        bool IsActive,
        DateTimeOffset CreatedAt);

    internal sealed class Handler(
        IUserAiPreferenceRepository preferenceRepository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, IReadOnlyList<Response>>
    {
        public async Task<Result<IReadOnlyList<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(currentUser.Id);
            var preferences = await preferenceRepository.ListByUserAsync(
                userId, currentTenant.Id, cancellationToken);

            var responses = preferences.Select(p => new Response(
                p.Id.Value,
                p.FeatureKey,
                (int)p.PreferenceType,
                p.PreferredProviderId,
                p.PreferredProviderId,
                p.ExternalProduct.HasValue ? (int)p.ExternalProduct.Value : null,
                p.ExternalProductModel,
                p.DisableReason,
                p.IsActive,
                p.CreatedAt)).ToList();

            return responses;
        }
    }
}
