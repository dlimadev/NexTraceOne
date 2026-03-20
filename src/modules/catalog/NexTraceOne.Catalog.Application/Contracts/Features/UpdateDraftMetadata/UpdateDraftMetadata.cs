using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftMetadata;

/// <summary>
/// Feature: UpdateDraftMetadata — atualiza os metadados de um draft de contrato.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateDraftMetadata
{
    /// <summary>Comando de atualização de metadados de draft.</summary>
    public sealed record Command(
        Guid DraftId,
        string? Title,
        string? Description,
        string? ProposedVersion,
        Guid? ServiceId,
        string EditedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização de metadados.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.EditedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.ProposedVersion).MaximumLength(50).When(x => x.ProposedVersion is not null);
        }
    }

    /// <summary>
    /// Handler que atualiza os metadados de um draft de contrato.
    /// Carrega o draft pelo id, delega a edição ao método de domínio UpdateMetadata,
    /// e persiste a alteração.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var draft = await repository.GetByIdAsync(
                ContractDraftId.From(request.DraftId), cancellationToken);

            if (draft is null)
                return ContractsErrors.DraftNotFound(request.DraftId.ToString());

            var result = draft.UpdateMetadata(
                request.Title,
                request.Description,
                request.ProposedVersion,
                request.ServiceId,
                request.EditedBy,
                dateTimeProvider.UtcNow);

            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(draft.Id.Value);
        }
    }

    /// <summary>Resposta da atualização de metadados de draft.</summary>
    public sealed record Response(Guid DraftId);
}
