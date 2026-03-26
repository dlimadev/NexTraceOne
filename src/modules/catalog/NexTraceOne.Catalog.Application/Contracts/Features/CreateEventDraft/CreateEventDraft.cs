using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateEventDraft;

/// <summary>
/// Feature: CreateEventDraft — cria um draft Event/AsyncAPI no Contract Studio com metadados específicos.
/// Distingue-se do CreateDraft genérico: para além de criar o ContractDraft com ContractType=Event e Protocol=AsyncApi,
/// cria também um EventDraftMetadata com os campos específicos de eventos (título, versão AsyncAPI, channels, mensagens).
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class CreateEventDraft
{
    /// <summary>
    /// Comando de criação de draft Event/AsyncAPI com metadados específicos.
    /// </summary>
    public sealed record Command(
        string Title,
        string Author,
        string AsyncApiVersion = "2.6.0",
        Guid? ServiceId = null,
        string? Description = null,
        string DefaultContentType = "application/json",
        string? ChannelsJson = null,
        string? MessagesJson = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de draft de evento.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Author).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AsyncApiVersion).NotEmpty().MaximumLength(20);
            RuleFor(x => x.DefaultContentType).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que cria um draft Event/AsyncAPI com metadados específicos:
    /// 1. Cria ContractDraft com ContractType=Event e Protocol=AsyncApi
    /// 2. Cria EventDraftMetadata com os campos AsyncAPI informados
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository draftRepository,
        IEventDraftMetadataRepository eventMetadataRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Cria o ContractDraft com tipo e protocolo Event/AsyncAPI
            var draftResult = ContractDraft.Create(
                request.Title,
                request.Author,
                ContractType.Event,
                ContractProtocol.AsyncApi,
                request.ServiceId,
                request.Description);

            if (draftResult.IsFailure)
                return draftResult.Error;

            var draft = draftResult.Value;
            draftRepository.Add(draft);

            // 2. Cria EventDraftMetadata com metadados AsyncAPI específicos
            var metadata = EventDraftMetadata.Create(
                draft.Id,
                request.Title,
                request.AsyncApiVersion,
                request.DefaultContentType,
                request.ChannelsJson ?? "{}",
                request.MessagesJson ?? "{}");

            eventMetadataRepository.Add(metadata);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                DraftId: draft.Id.Value,
                Title: draft.Title,
                Status: draft.Status.ToString(),
                AsyncApiVersion: request.AsyncApiVersion,
                DefaultContentType: request.DefaultContentType,
                CreatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da criação de draft de evento, incluindo metadados AsyncAPI inicializados.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Status,
        string AsyncApiVersion,
        string DefaultContentType,
        DateTimeOffset CreatedAt);
}
