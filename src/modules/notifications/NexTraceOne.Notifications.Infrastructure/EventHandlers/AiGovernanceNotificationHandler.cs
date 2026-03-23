using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de IA e governança de IA do módulo AIKnowledge.
/// Gera notificações internas quando providers ficam indisponíveis, budgets de tokens
/// são excedidos, gerações falham ou ações são bloqueadas por política.
/// Fase 5: handler completo de domínio de IA.
/// </summary>
internal sealed class AiGovernanceNotificationHandler(
    INotificationModule notificationModule,
    ILogger<AiGovernanceNotificationHandler> logger)
    : IIntegrationEventHandler<AiProviderUnavailableIntegrationEvent>,
      IIntegrationEventHandler<TokenBudgetExceededIntegrationEvent>,
      IIntegrationEventHandler<AiGenerationFailedIntegrationEvent>,
      IIntegrationEventHandler<AiActionBlockedByPolicyIntegrationEvent>
{
    public async Task HandleAsync(AiProviderUnavailableIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing AiProviderUnavailable notification for provider {ProviderName}",
            @event.ProviderName);

        if (@event.TenantId is null)
        {
            logger.LogWarning("AiProviderUnavailable event missing TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ProviderName,
            @event.ErrorMessage
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.AiProviderUnavailable,
            Category = nameof(NotificationCategory.AI),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"AI provider unavailable — {@event.ProviderName}",
            Message = $"AI provider {@event.ProviderName} is unavailable: {@event.ErrorMessage}. AI-assisted features may be impacted.",
            SourceModule = "AIKnowledge",
            SourceEntityType = "AiProvider",
            SourceEntityId = @event.ProviderName,
            ActionUrl = "/ai/providers",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientRoles = ["AiAdmin", "PlatformAdmin"],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(TokenBudgetExceededIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing TokenBudgetExceeded notification for user {UserId}, provider {ProviderName}",
            @event.UserId, @event.ProviderName);

        if (@event.TenantId is null)
        {
            logger.LogWarning("TokenBudgetExceeded event missing TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ProviderName,
            @event.TokensUsed,
            @event.TokenLimit
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.TokenBudgetExceeded,
            Category = nameof(NotificationCategory.AI),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Token budget exceeded — {@event.ProviderName}",
            Message = $"You have used {@event.TokensUsed:N0} of {@event.TokenLimit:N0} allocated tokens for {@event.ProviderName}. Further AI requests may be limited.",
            SourceModule = "AIKnowledge",
            SourceEntityType = "TokenBudget",
            SourceEntityId = @event.UserId.ToString(),
            ActionUrl = "/ai/usage",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.UserId],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(AiGenerationFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing AiGenerationFailed notification for request {RequestId}, provider {ProviderName}",
            @event.RequestId, @event.ProviderName);

        if (@event.RequestingUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("AiGenerationFailed event missing RequestingUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ProviderName,
            @event.ErrorMessage
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.AiGenerationFailed,
            Category = nameof(NotificationCategory.AI),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"AI generation failed — {@event.ProviderName}",
            Message = $"AI generation via {@event.ProviderName} has failed: {@event.ErrorMessage}. Try again or use a different provider.",
            SourceModule = "AIKnowledge",
            SourceEntityType = "AiRequest",
            SourceEntityId = @event.RequestId.ToString(),
            ActionUrl = "/ai/history",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.RequestingUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(AiActionBlockedByPolicyIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing AiActionBlockedByPolicy notification for policy {PolicyName}",
            @event.PolicyName);

        if (@event.UserId is null || @event.TenantId is null)
        {
            logger.LogWarning("AiActionBlockedByPolicy event missing UserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.PolicyName,
            @event.ActionDescription
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.AiActionBlockedByPolicy,
            Category = nameof(NotificationCategory.AI),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"AI action blocked — {@event.PolicyName}",
            Message = $"The AI action '{@event.ActionDescription}' was blocked by policy {@event.PolicyName}. Contact your AI administrator for access.",
            SourceModule = "AIKnowledge",
            SourceEntityType = "AiPolicy",
            SourceEntityId = @event.PolicyName,
            ActionUrl = "/ai/policies",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.UserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
