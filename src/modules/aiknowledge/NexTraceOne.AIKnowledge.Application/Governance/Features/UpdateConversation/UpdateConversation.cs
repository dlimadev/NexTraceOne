using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using MediatR;

namespace NexTraceOne.AiGovernance.Application.Features.UpdateConversation;

/// <summary>
/// Feature: UpdateConversation — atualiza metadados de uma conversa do assistente de IA.
/// Permite alterar título, tags e arquivar conversas.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class UpdateConversation
{
    /// <summary>Comando de atualização de conversa do assistente de IA.</summary>
    public sealed record Command(
        Guid ConversationId,
        string? Title,
        string? Tags,
        bool? Archive) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ConversationId).NotEmpty();
            RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title is not null);
        }
    }

    /// <summary>Handler que atualiza metadados da conversa.</summary>
    public sealed class Handler(
        IAiAssistantConversationRepository conversationRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var conversationId = AiAssistantConversationId.From(request.ConversationId);
            var conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);

            if (conversation is null)
                return AiGovernanceErrors.ConversationNotFound(request.ConversationId.ToString());

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var result = conversation.UpdateMetadata(request.Title, request.Tags);
                if (result.IsFailure)
                    return result.Error;
            }

            if (request.Archive == true)
            {
                var archiveResult = conversation.Archive();
                if (archiveResult.IsFailure)
                    return archiveResult.Error;
            }

            await conversationRepository.UpdateAsync(conversation, cancellationToken);

            return new Response(
                conversation.Id.Value,
                conversation.Title,
                conversation.Tags,
                conversation.IsActive);
        }
    }

    /// <summary>Resposta da atualização de conversa.</summary>
    public sealed record Response(
        Guid ConversationId,
        string Title,
        string Tags,
        bool IsActive);
}
