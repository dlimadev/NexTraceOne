using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftContent;

/// <summary>
/// Feature: UpdateDraftContent — atualiza o conteúdo do artefato de um draft de contrato.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateDraftContent
{
    /// <summary>Comando de atualização de conteúdo de draft.</summary>
    public sealed record Command(
        Guid DraftId,
        string SpecContent,
        string Format,
        string EditedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização de conteúdo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.SpecContent).NotEmpty()
                .MaximumLength(5_242_880)
                .WithMessage("Spec content exceeds maximum allowed size of 5MB.");
            RuleFor(x => x.Format).NotEmpty()
                .Must(f => f is "json" or "yaml" or "xml")
                .WithMessage("Format must be 'json', 'yaml' or 'xml'.");
            RuleFor(x => x.EditedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que atualiza o conteúdo do artefato de um draft.
    /// Carrega o draft pelo id, delega a edição ao método de domínio UpdateContent,
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

            var result = draft.UpdateContent(
                request.SpecContent,
                request.Format,
                request.EditedBy,
                dateTimeProvider.UtcNow);

            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(draft.Id.Value);
        }
    }

    /// <summary>Resposta da atualização de conteúdo de draft.</summary>
    public sealed record Response(Guid DraftId);
}
