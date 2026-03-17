using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft;

/// <summary>
/// Feature: CreateDraft — cria um novo rascunho de contrato no Contract Studio.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateDraft
{
    /// <summary>Comando de criação de draft de contrato.</summary>
    public sealed record Command(
        string Title,
        string Author,
        ContractType ContractType,
        ContractProtocol Protocol,
        Guid? ServiceId = null,
        string? Description = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de draft.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Author).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractType).IsInEnum();
            RuleFor(x => x.Protocol).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que cria um novo draft de contrato.
    /// Delega a criação da entidade ao factory method ContractDraft.Create,
    /// que aplica todas as regras de domínio e invariantes.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = ContractDraft.Create(
                request.Title,
                request.Author,
                request.ContractType,
                request.Protocol,
                request.ServiceId,
                request.Description);

            if (result.IsFailure)
                return result.Error;

            var draft = result.Value;
            repository.Add(draft);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                draft.Id.Value,
                draft.Title,
                draft.Status.ToString(),
                dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da criação de draft de contrato.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Status,
        DateTimeOffset CreatedAt);
}
