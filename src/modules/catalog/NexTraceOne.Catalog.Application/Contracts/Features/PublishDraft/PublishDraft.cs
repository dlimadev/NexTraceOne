using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.PublishDraft;

/// <summary>
/// Feature: PublishDraft — publica um draft aprovado, criando a versão oficial de contrato.
/// O draft deve estar no estado Approved. Cria ContractVersion via ContractVersion.Import
/// com os dados do draft e marca o draft como Published.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class PublishDraft
{
    /// <summary>Comando de publicação de draft de contrato.</summary>
    public sealed record Command(
        Guid DraftId,
        string PublishedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de publicação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.PublishedBy).NotEmpty().MaximumLength(200);
        }
    }

    private const string ImportedFromPrefix = "contract-studio";

    /// <summary>
    /// Handler que publica um draft aprovado como versão oficial de contrato.
    /// Verifica que o draft está Approved, cria ContractVersion via Import,
    /// marca o draft como Published e persiste ambas as entidades.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository draftRepository,
        IContractVersionRepository versionRepository,
        IApiAssetRepository apiAssetRepository,
        IServiceAssetRepository serviceAssetRepository,
        IContractsUnitOfWork unitOfWork,
        ICatalogGraphUnitOfWork catalogGraphUnitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var draft = await draftRepository.GetByIdAsync(
                ContractDraftId.From(request.DraftId), cancellationToken);

            if (draft is null)
                return ContractsErrors.DraftNotFound(request.DraftId.ToString());

            if (draft.Status != DraftStatus.Approved)
                return ContractsErrors.InvalidDraftTransition(
                    draft.Status.ToString(), DraftStatus.Published.ToString());

            var publishTargetResult = await ResolvePublishTargetAsync(draft, cancellationToken);
            if (publishTargetResult.IsFailure)
                return publishTargetResult.Error;

            var importResult = ContractVersion.Import(
                publishTargetResult.Value.ApiAssetId,
                draft.ProposedVersion,
                draft.SpecContent,
                draft.Format,
                $"{ImportedFromPrefix}:{request.PublishedBy}",
                draft.Protocol);

            if (importResult.IsFailure)
                return importResult.Error;

            var version = importResult.Value;
            var now = dateTimeProvider.UtcNow;
            var moveToReviewResult = version.TransitionTo(ContractLifecycleState.InReview, now);
            if (moveToReviewResult.IsFailure)
                return moveToReviewResult.Error;

            var approveVersionResult = version.TransitionTo(ContractLifecycleState.Approved, now);
            if (approveVersionResult.IsFailure)
                return approveVersionResult.Error;

            versionRepository.Add(version);

            var publishResult = draft.MarkAsPublished(now);
            if (publishResult.IsFailure)
                return publishResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);
            await publishTargetResult.Value.CommitGraphChangesAsync(cancellationToken);

            return new Response(version.Id.Value, draft.Id.Value);
        }

        private async Task<Result<PublishTarget>> ResolvePublishTargetAsync(ContractDraft draft, CancellationToken cancellationToken)
        {
            if (!draft.ServiceId.HasValue)
                return ContractsErrors.DraftMissingCatalogLink(draft.Id.Value.ToString());

            var linkedId = draft.ServiceId.Value;

            var existingApiAsset = await apiAssetRepository.GetByIdAsync(ApiAssetId.From(linkedId), cancellationToken);
            if (existingApiAsset is not null)
            {
                return PublishTarget.ForExisting(existingApiAsset.Id.Value);
            }

            var serviceAsset = await serviceAssetRepository.GetByIdAsync(ServiceAssetId.From(linkedId), cancellationToken);
            if (serviceAsset is null)
                return ContractsErrors.CatalogLinkNotFound(linkedId.ToString());

            var existingOwnedApiAsset = await apiAssetRepository.GetByNameAndOwnerAsync(
                draft.Title,
                serviceAsset.Id,
                cancellationToken);

            if (existingOwnedApiAsset is not null)
            {
                return PublishTarget.ForExisting(existingOwnedApiAsset.Id.Value);
            }

            var apiAsset = ApiAsset.Register(
                draft.Title,
                BuildRoutePattern(draft.Title),
                draft.ProposedVersion,
                serviceAsset.ExposureType.ToString(),
                serviceAsset);

            apiAssetRepository.Add(apiAsset);
            return PublishTarget.ForNew(apiAsset.Id.Value, catalogGraphUnitOfWork);
        }

        private static string BuildRoutePattern(string title)
        {
            var trimmed = (title ?? string.Empty).Trim().ToLowerInvariant();
            var sanitized = string.Concat(trimmed.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'));
            var compact = string.Join('-', sanitized.Split('-', StringSplitOptions.RemoveEmptyEntries));
            return $"/api/{(string.IsNullOrWhiteSpace(compact) ? "contracts" : compact)}";
        }

        private sealed record PublishTarget(Guid ApiAssetId, Func<CancellationToken, Task> CommitGraphChangesAsync)
        {
            public static PublishTarget ForExisting(Guid apiAssetId)
                => new(apiAssetId, _ => Task.CompletedTask);

            public static PublishTarget ForNew(Guid apiAssetId, ICatalogGraphUnitOfWork graphUnitOfWork)
                => new(apiAssetId, async cancellationToken =>
                {
                    await graphUnitOfWork.CommitAsync(cancellationToken);
                });
        }
    }

    /// <summary>Resposta da publicação de draft com o id da versão oficial criada.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid DraftId);
}
