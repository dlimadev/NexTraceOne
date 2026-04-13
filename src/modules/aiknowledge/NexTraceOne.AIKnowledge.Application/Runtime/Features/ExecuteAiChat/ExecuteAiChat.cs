using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteAiChat;

/// <summary>
/// Feature: ExecuteAiChat — executa inferência real de chat via provider de IA.
/// Resolve provider e modelo, executa chamada real, salva request/response/metadados,
/// salva duração, status e correlaciona com usuário/tenant.
/// </summary>
public static class ExecuteAiChat
{
    public sealed record Command(
        Guid? ConversationId,
        string Message,
        Guid? PreferredModelId,
        string? SystemPrompt,
        double? Temperature,
        int? MaxTokens) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Message).NotEmpty().MaximumLength(32_000);
            RuleFor(x => x.Temperature).InclusiveBetween(0.0, 2.0).When(x => x.Temperature.HasValue);
            RuleFor(x => x.MaxTokens).GreaterThan(0).When(x => x.MaxTokens.HasValue);
        }
    }

    public sealed class Handler(
        IAiProviderFactory providerFactory,
        IAiModelCatalogService modelCatalogService,
        IAiAssistantConversationRepository conversationRepository,
        IAiMessageRepository messageRepository,
        IAiUsageEntryRepository usageEntryRepository,
        IAiTokenQuotaService tokenQuotaService,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var correlationId = Guid.NewGuid().ToString();
            var startTime = dateTimeProvider.UtcNow;

            // 1. Resolve model
            ResolvedModel? resolvedModel;
            if (request.PreferredModelId.HasValue)
            {
                resolvedModel = await modelCatalogService.ResolveModelByIdAsync(
                    request.PreferredModelId.Value, cancellationToken);
            }
            else
            {
                resolvedModel = await modelCatalogService.ResolveDefaultModelAsync(
                    "chat", cancellationToken);
            }

            if (resolvedModel is null)
            {
                return Error.NotFound(
                    "AI.ModelNotFound",
                    "No AI model available for chat. Please configure a model in the AI Model Registry.");
            }

            // 2. Get chat provider
            var chatProvider = providerFactory.GetChatProvider(resolvedModel.ProviderId);
            if (chatProvider is null)
            {
                return Error.NotFound(
                    "AI.ProviderNotFound",
                    "AI provider '{0}' is not available.",
                    resolvedModel.ProviderId);
            }

            // 3. Pre-validate token quota before inference
            var estimatedTokens = ContextWindowManager.EstimateTokens(request.Message) +
                                  ContextWindowManager.EstimateTokens(request.SystemPrompt ?? string.Empty);

            var quotaResult = await tokenQuotaService.ValidateQuotaAsync(
                currentUser.Id,
                currentTenant.Id,
                resolvedModel.ModelName,
                resolvedModel.ProviderId,
                estimatedTokens,
                cancellationToken);

            if (!quotaResult.IsAllowed)
            {
                return Error.Business(
                    "AIKnowledge.QuotaExceeded",
                    "Token quota exceeded: {0}",
                    quotaResult.BlockReason ?? "Policy limit reached");
            }

            // 4. Resolve or create conversation
            AiAssistantConversation? conversation = null;
            if (request.ConversationId.HasValue)
            {
                conversation = await conversationRepository.GetByIdAsync(
                    AiAssistantConversationId.From(request.ConversationId.Value), cancellationToken);
            }

            if (conversation is null)
            {
                var title = request.Message.Length > 80
                    ? string.Concat(request.Message.AsSpan(0, 77), "...")
                    : request.Message;

                conversation = AiAssistantConversation.Start(
                    title,
                    "default",
                    AIClientType.Web,
                    "general",
                    currentUser.IsAuthenticated ? currentUser.Id : "anonymous");
                await conversationRepository.AddAsync(conversation, cancellationToken);
            }

            // 5. Save user message
            var userMessage = AiMessage.UserMessage(
                conversation.Id.Value,
                request.Message,
                startTime);
            await messageRepository.AddAsync(userMessage, cancellationToken);
            conversation.RecordMessage(null, startTime);

            // 5. Build chat request with conversation history
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new ChatMessage("system", request.SystemPrompt));
            }

            messages.Add(new ChatMessage("user", request.Message));

            var chatRequest = new ChatCompletionRequest(
                resolvedModel.ModelName,
                messages,
                request.Temperature,
                request.MaxTokens,
                request.SystemPrompt);

            // 6. Execute real inference
            var result = await chatProvider.CompleteAsync(chatRequest, cancellationToken);
            var endTime = dateTimeProvider.UtcNow;

            // 7. Save assistant message
            if (result.Success && result.Content is not null)
            {
                var assistantMessage = AiMessage.AssistantMessage(
                    conversationId: conversation.Id.Value,
                    content: result.Content,
                    modelName: result.ModelId,
                    provider: result.ProviderId,
                    isInternal: resolvedModel.IsInternal,
                    promptTokens: result.PromptTokens,
                    completionTokens: result.CompletionTokens,
                    appliedPolicyName: null,
                    groundingSources: string.Empty,
                    contextReferences: string.Empty,
                    correlationId: correlationId,
                    timestamp: endTime);
                await messageRepository.AddAsync(assistantMessage, cancellationToken);
                conversation.RecordMessage(result.ModelId, endTime);
            }

            // 8. Record usage audit entry
            var usage = AIUsageEntry.Record(
                userId: currentUser.IsAuthenticated ? currentUser.Id : "anonymous",
                userDisplayName: currentUser.IsAuthenticated ? currentUser.Name : "Anonymous",
                modelId: resolvedModel.ModelId,
                modelName: resolvedModel.ModelName,
                provider: resolvedModel.ProviderId,
                isInternal: resolvedModel.IsInternal,
                timestamp: startTime,
                promptTokens: result.PromptTokens,
                completionTokens: result.CompletionTokens,
                policyId: null,
                policyName: null,
                result: result.Success ? UsageResult.Allowed : UsageResult.ModelUnavailable,
                contextScope: "chat",
                clientType: AIClientType.Web,
                correlationId: correlationId,
                conversationId: conversation.Id.Value);
            await usageEntryRepository.AddAsync(usage, cancellationToken);

            return new Response(
                conversation.Id.Value,
                result.Content ?? result.ErrorMessage ?? "No response from AI provider.",
                result.ModelId,
                result.ProviderId,
                resolvedModel.IsInternal,
                result.PromptTokens,
                result.CompletionTokens,
                result.Duration.TotalMilliseconds,
                result.Success,
                correlationId);
        }
    }

    public sealed record Response(
        Guid ConversationId,
        string Content,
        string ModelId,
        string ProviderId,
        bool IsInternalModel,
        int PromptTokens,
        int CompletionTokens,
        double DurationMs,
        bool Success,
        string CorrelationId);
}
