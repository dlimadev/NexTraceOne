using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para o handler de IA e governança de IA da Fase 5:
/// AiProviderUnavailable, TokenBudgetExceeded, AiGenerationFailed, AiActionBlockedByPolicy.
/// </summary>
public sealed class AiGovernanceNotificationHandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly AiGovernanceNotificationHandler _handler;

    public AiGovernanceNotificationHandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
        _handler = new AiGovernanceNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<AiGovernanceNotificationHandler>());
    }

    // ── AiProviderUnavailable ──

    [Fact]
    public async Task HandleAsync_AiProviderUnavailable_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var @event = new AiProviderUnavailableIntegrationEvent(
            ProviderName: "OpenAI GPT-4",
            ErrorMessage: "Service temporarily unavailable (503)",
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.AiProviderUnavailable);
        r.Category.Should().Be(nameof(NotificationCategory.AI));
        r.Severity.Should().Be(nameof(NotificationSeverity.Critical));
        r.SourceModule.Should().Be("AIKnowledge");
        r.SourceEntityType.Should().Be("AiProvider");
        r.SourceEntityId.Should().Be("OpenAI GPT-4");
        r.ActionUrl.Should().Be("/ai/providers");
        r.RequiresAction.Should().BeTrue();
        r.RecipientRoles.Should().Contain("AiAdmin");
        r.Title.Should().Contain("OpenAI GPT-4");
        r.Message.Should().Contain("503");
    }

    [Fact]
    public async Task HandleAsync_AiProviderUnavailable_MissingTenant_Skips()
    {
        var @event = new AiProviderUnavailableIntegrationEvent(
            ProviderName: "Provider",
            ErrorMessage: "Error",
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── TokenBudgetExceeded ──

    [Fact]
    public async Task HandleAsync_TokenBudgetExceeded_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var @event = new TokenBudgetExceededIntegrationEvent(
            UserId: userId,
            ProviderName: "Claude",
            TokensUsed: 150000,
            TokenLimit: 100000,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.TokenBudgetExceeded);
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceEntityType.Should().Be("TokenBudget");
        r.ActionUrl.Should().Be("/ai/usage");
        r.RequiresAction.Should().BeFalse();
        r.RecipientUserIds.Should().Contain(userId);
        r.Title.Should().Contain("Claude");
        r.Message.Should().Contain("150,000");
        r.Message.Should().Contain("100,000");
    }

    [Fact]
    public async Task HandleAsync_TokenBudgetExceeded_MissingTenant_Skips()
    {
        var @event = new TokenBudgetExceededIntegrationEvent(
            UserId: Guid.NewGuid(),
            ProviderName: "Provider",
            TokensUsed: 100,
            TokenLimit: 50,
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── AiGenerationFailed ──

    [Fact]
    public async Task HandleAsync_AiGenerationFailed_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var @event = new AiGenerationFailedIntegrationEvent(
            RequestId: requestId,
            ProviderName: "Azure OpenAI",
            ErrorMessage: "Content filter triggered",
            RequestingUserId: requestingUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.AiGenerationFailed);
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceEntityType.Should().Be("AiRequest");
        r.SourceEntityId.Should().Be(requestId.ToString());
        r.ActionUrl.Should().Be("/ai/history");
        r.RequiresAction.Should().BeFalse();
        r.RecipientUserIds.Should().Contain(requestingUserId);
        r.Title.Should().Contain("Azure OpenAI");
        r.Message.Should().Contain("Content filter");
    }

    [Fact]
    public async Task HandleAsync_AiGenerationFailed_MissingRequestingUser_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new AiGenerationFailedIntegrationEvent(
            RequestId: Guid.NewGuid(),
            ProviderName: "Provider",
            ErrorMessage: "Error",
            RequestingUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── AiActionBlockedByPolicy ──

    [Fact]
    public async Task HandleAsync_AiActionBlockedByPolicy_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var @event = new AiActionBlockedByPolicyIntegrationEvent(
            PolicyName: "DataClassification",
            ActionDescription: "Generate contract from production data",
            UserId: userId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.AiActionBlockedByPolicy);
        r.Severity.Should().Be(nameof(NotificationSeverity.Info));
        r.SourceEntityType.Should().Be("AiPolicy");
        r.SourceEntityId.Should().Be("DataClassification");
        r.ActionUrl.Should().Be("/ai/policies");
        r.RequiresAction.Should().BeFalse();
        r.RecipientUserIds.Should().Contain(userId);
        r.Title.Should().Contain("DataClassification");
        r.Message.Should().Contain("Generate contract from production data");
    }

    [Fact]
    public async Task HandleAsync_AiActionBlockedByPolicy_MissingUser_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new AiActionBlockedByPolicyIntegrationEvent(
            PolicyName: "Policy",
            ActionDescription: "Action",
            UserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
