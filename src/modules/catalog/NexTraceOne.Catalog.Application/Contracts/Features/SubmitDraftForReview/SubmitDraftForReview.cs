using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SubmitDraftForReview;

/// <summary>
/// Feature: SubmitDraftForReview — submete um draft de contrato para revisão.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SubmitDraftForReview
{
    /// <summary>Comando de submissão de draft para revisão.</summary>
    public sealed record Command(Guid DraftId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de submissão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que submete um draft para revisão.
    /// Carrega o draft pelo id, delega a transição ao método de domínio SubmitForReview,
    /// e persiste a alteração.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var draft = await repository.GetByIdAsync(
                ContractDraftId.From(request.DraftId), cancellationToken);

            if (draft is null)
                return ContractsErrors.DraftNotFound(request.DraftId.ToString());

            var result = draft.SubmitForReview(dateTimeProvider.UtcNow);

            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(draft.Id.Value);
        }
    }

    /// <summary>Resposta da submissão de draft para revisão.</summary>
    public sealed record Response(Guid DraftId);
}
