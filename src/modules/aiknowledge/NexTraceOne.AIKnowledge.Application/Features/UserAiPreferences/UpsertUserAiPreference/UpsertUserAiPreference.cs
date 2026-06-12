using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.UpsertUserAiPreference;

public static class UpsertUserAiPreference
{
    public sealed record Command(
        string FeatureKey,
        int PreferenceType,
        Guid? PreferredModelId = null,
        string? PreferredProviderId = null,
        int? ExternalProduct = null,
        string? ExternalProductModel = null,
        string? DisableReason = null) : ICommand<Response>;

    public sealed record Response(Guid PreferenceId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FeatureKey).NotEmpty().MaximumLength(128);
            RuleFor(x => x.PreferenceType).InclusiveBetween(0, 2);

            When(x => x.PreferenceType == (int)AiPreferenceType.Internal, () =>
            {
                RuleFor(x => x.PreferredModelId).NotEmpty();
                RuleFor(x => x.PreferredProviderId).NotEmpty().MaximumLength(64);
            });

            When(x => x.PreferenceType == (int)AiPreferenceType.ExternalProduct, () =>
            {
                RuleFor(x => x.ExternalProduct).NotNull();
            });
        }
    }

    internal sealed class Handler(
        IUserAiPreferenceRepository preferenceRepository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var userId = Guid.Parse(currentUser.Id);
            var tenantId = currentTenant.Id;
            var featureKey = request.FeatureKey.Trim().ToLowerInvariant();
            var preferenceType = (AiPreferenceType)request.PreferenceType;

            ExternalAiProductType? externalProduct = request.ExternalProduct.HasValue
                ? (ExternalAiProductType)request.ExternalProduct.Value
                : null;

            var existing = await preferenceRepository.GetByUserAndFeatureAsync(
                userId, tenantId, featureKey, cancellationToken);

            if (existing is not null)
            {
                var updateResult = preferenceType switch
                {
                    AiPreferenceType.Disabled => existing.SetDisabledPreference(request.DisableReason),
                    AiPreferenceType.Internal when request.PreferredModelId.HasValue
                        => existing.SetInternalPreference(request.PreferredModelId.Value, request.PreferredProviderId!),
                    AiPreferenceType.ExternalProduct when externalProduct.HasValue
                        => existing.SetExternalProductPreference(externalProduct.Value, request.ExternalProductModel),
                    _ => Error.Validation("UpsertUserAiPreference.InvalidType", "Tipo de preferência inválido ou incompleto.")
                };

                if (updateResult.IsFailure)
                    return updateResult.Error;

                existing.Activate();
                await preferenceRepository.UpdateAsync(existing, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return new Response(existing.Id.Value);
            }

            var createResult = UserAiPreference.Create(
                userId,
                tenantId,
                featureKey,
                preferenceType,
                request.PreferredModelId,
                request.PreferredProviderId,
                externalProduct,
                request.ExternalProductModel,
                request.DisableReason);

            if (createResult.IsFailure)
                return createResult.Error;

            await preferenceRepository.AddAsync(createResult.Value, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(createResult.Value.Id.Value);
        }
    }
}
