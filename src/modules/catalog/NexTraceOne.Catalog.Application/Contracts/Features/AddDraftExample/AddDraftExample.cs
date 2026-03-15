using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.AddDraftExample;

/// <summary>
/// Feature: AddDraftExample — adiciona um exemplo a um draft de contrato.
/// Exemplos documentam cenários de uso com payloads de request/response.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AddDraftExample
{
    /// <summary>Comando de adição de exemplo a um draft.</summary>
    public sealed record Command(
        Guid DraftId,
        string Name,
        string Content,
        string ContentFormat,
        string ExampleType,
        string CreatedBy,
        string? Description = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de adição de exemplo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Content).NotEmpty()
                .MaximumLength(1_048_576)
                .WithMessage("Example content exceeds maximum allowed size of 1MB.");
            RuleFor(x => x.ContentFormat).NotEmpty()
                .Must(f => f is "json" or "yaml" or "xml")
                .WithMessage("Content format must be 'json', 'yaml' or 'xml'.");
            RuleFor(x => x.ExampleType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        }
    }

    /// <summary>
    /// Handler que adiciona um exemplo a um draft de contrato.
    /// Carrega o draft, cria o exemplo via factory method ContractExample.CreateForDraft,
    /// adiciona ao draft e persiste a alteração.
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

            var example = ContractExample.CreateForDraft(
                draft.Id,
                request.Name,
                request.Content,
                request.ContentFormat,
                request.ExampleType,
                request.CreatedBy,
                dateTimeProvider.UtcNow,
                request.Description);

            draft.AddExample(example);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(example.Id.Value, draft.Id.Value);
        }
    }

    /// <summary>Resposta da adição de exemplo com o id do exemplo criado.</summary>
    public sealed record Response(
        Guid ExampleId,
        Guid DraftId);
}
