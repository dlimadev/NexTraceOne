using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.GetUserAiPreferenceByFeature;

public static class GetUserAiPreferenceByFeature
{
    public sealed record Query(string FeatureKey) : IQuery<Response>;

    public sealed record Response(
        Guid Id,
        string FeatureKey,
        int PreferenceType,
        Guid? PreferredModelId,
        string? PreferredProviderId,
        int? ExternalProduct,
        string? ExternalProductModel,
        string? DisableReason,
        bool IsActive,
        DateTimeOffset CreatedAt);

    internal sealed class Handler(
        IUserAiPreferenceRepository preferenceRepository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(currentUser.Id);
            var preference = await preferenceRepository.GetByUserAndFeatureAsync(
                userId, currentTenant.Id, request.FeatureKey, cancellationToken);

            if (preference is null)
                return UserAiPreferenceErrors.NotFoundForFeature(request.FeatureKey);

            return new Response(
                preference.Id.Value,
                preference.FeatureKey,
                (int)preference.PreferenceType,
                preference.PreferredModelId,
                preference.PreferredProviderId,
                preference.ExternalProduct.HasValue ? (int)preference.ExternalProduct.Value : null,
                preference.ExternalProductModel,
                preference.DisableReason,
                preference.IsActive,
                preference.CreatedAt);
        }
    }
}
