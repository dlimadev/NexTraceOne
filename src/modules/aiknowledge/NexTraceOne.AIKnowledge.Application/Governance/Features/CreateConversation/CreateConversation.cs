using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateConversation;

/// <summary>
/// Feature: CreateConversation — cria uma nova conversa do assistente de IA.
/// Suporta contexto de persona, escopo padrão e associação a entidades do produto.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateConversation
{
    /// <summary>Comando de criação de conversa do assistente de IA.</summary>
    public sealed record Command(
        string Title,
        string? Persona,
        string? ClientType,
        string? DefaultContextScope,
        string? Tags,
        Guid? ServiceId,
        Guid? ContractId,
        Guid? IncidentId,
        Guid? TeamId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de conversa.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que cria uma conversa madura do assistente de IA.</summary>
    public sealed class Handler(
        IAiAssistantConversationRepository conversationRepository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;

            var conversation = AiAssistantConversation.Start(
                request.Title,
                request.Persona ?? "Engineer",
                clientType,
                request.DefaultContextScope ?? string.Empty,
                currentUser.Id,
                request.ServiceId,
                request.ContractId,
                request.IncidentId,
                request.TeamId);

            if (!string.IsNullOrWhiteSpace(request.Tags))
                conversation.UpdateMetadata(request.Title, request.Tags);

            await conversationRepository.AddAsync(conversation, cancellationToken);

            return new Response(
                conversation.Id.Value,
                conversation.Title,
                conversation.Persona,
                conversation.ClientType.ToString(),
                conversation.DefaultContextScope,
                conversation.IsActive);
        }
    }

    /// <summary>Resposta da criação de conversa do assistente de IA.</summary>
    public sealed record Response(
        Guid ConversationId,
        string Title,
        string Persona,
        string ClientType,
        string DefaultContextScope,
        bool IsActive);
}
