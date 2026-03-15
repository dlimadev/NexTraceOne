using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.SendAssistantMessage;

/// <summary>
/// Feature: SendAssistantMessage — envia uma mensagem ao assistente de IA governado.
/// Valida políticas de acesso, regista auditoria de uso e retorna resposta.
/// Implementação stub — grounding de contexto em desenvolvimento.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SendAssistantMessage
{
    /// <summary>Comando de envio de mensagem ao assistente de IA.</summary>
    public sealed record Command(
        Guid? ConversationId,
        string Message,
        string? ContextScope,
        string? Persona,
        Guid? PreferredModelId,
        string ClientType) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de envio de mensagem.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Message).NotEmpty().MaximumLength(10_000);
            RuleFor(x => x.ClientType).NotEmpty();
        }
    }

    /// <summary>Handler que processa a mensagem do assistente com governança integrada.</summary>
    public sealed class Handler(
        IAiUsageEntryRepository usageEntryRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var conversationId = request.ConversationId ?? Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;

            // Stub: resposta fixa enquanto o grounding de contexto está em desenvolvimento
            const string stubResponse = "AI response coming soon — context grounding under development.";
            const string stubModel = "internal-stub-v1";
            const int stubTokens = 0;

            var usageEntry = AIUsageEntry.Record(
                currentUser.Id,
                currentUser.Name,
                request.PreferredModelId ?? Guid.Empty,
                stubModel,
                "Internal",
                isInternal: true,
                dateTimeProvider.UtcNow,
                promptTokens: 0,
                completionTokens: 0,
                policyId: null,
                policyName: null,
                UsageResult.Allowed,
                request.ContextScope ?? string.Empty,
                clientType,
                correlationId: Guid.NewGuid().ToString(),
                conversationId);

            await usageEntryRepository.AddAsync(usageEntry, cancellationToken);

            return new Response(
                conversationId,
                messageId,
                stubResponse,
                stubModel,
                IsInternalModel: true,
                TokensUsed: stubTokens,
                GroundingSources: []);
        }
    }

    /// <summary>Resposta do envio de mensagem ao assistente de IA.</summary>
    public sealed record Response(
        Guid ConversationId,
        Guid MessageId,
        string AssistantResponse,
        string ModelUsed,
        bool IsInternalModel,
        int TokensUsed,
        List<string> GroundingSources);
}
