using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

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
        IUnitOfWork unitOfWork,
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

            var targetServiceId = draft.ServiceId ?? Guid.NewGuid();

            var importResult = ContractVersion.Import(
                targetServiceId,
                draft.ProposedVersion,
                draft.SpecContent,
                draft.Format,
                $"{ImportedFromPrefix}:{request.PublishedBy}",
                draft.Protocol);

            if (importResult.IsFailure)
                return importResult.Error;

            var version = importResult.Value;
            versionRepository.Add(version);

            var now = dateTimeProvider.UtcNow;
            var publishResult = draft.MarkAsPublished(now);
            if (publishResult.IsFailure)
                return publishResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(version.Id.Value, draft.Id.Value);
        }
    }

    /// <summary>Resposta da publicação de draft com o id da versão oficial criada.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid DraftId);
}
