using Microsoft.Extensions.Logging;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Engine;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class OrchestratorExternalDeliveryIntegrationTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly INotificationTemplateResolver _templateResolver = new NotificationTemplateResolver();
    private readonly INotificationDeduplicationService _dedup = Substitute.For<INotificationDeduplicationService>();
    private readonly INotificationSuppressionService _suppression = Substitute.For<INotificationSuppressionService>();
    private readonly INotificationGroupingService _grouping = Substitute.For<INotificationGroupingService>();
    private readonly INotificationAuditService _auditService = Substitute.For<INotificationAuditService>();
    private readonly IExternalDeliveryService _externalDelivery = Substitute.For<IExternalDeliveryService>();
    private readonly IEnvironmentBehaviorService _envBehavior = Substitute.For<IEnvironmentBehaviorService>();
    private readonly IConfigurationResolutionService _configResolution = Substitute.For<IConfigurationResolutionService>();
    private readonly ILogger<NotificationOrchestrator> _logger = Substitute.For<ILogger<NotificationOrchestrator>>();

    public OrchestratorExternalDeliveryIntegrationTests()
    {
        _dedup.IsDuplicateAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Suppression fail-open
        _suppression.EvaluateAsync(Arg.Any<NotificationRequest>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SuppressionResult.Allow());

        // Grouping: deterministic values
        _grouping.GenerateCorrelationKey(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>())
            .Returns("test-key");
        _grouping.ResolveGroupAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Config resolution fail-open
        _configResolution.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.Configuration.Contracts.DTOs.EffectiveConfigurationDto?)null);

        // Fail-open: external channels enabled, minimum severity = 0 (all pass)
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _envBehavior.GetIntAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(0);
    }

    private NotificationOrchestrator CreateOrchestrator(IExternalDeliveryService? externalDelivery)
        => new(_store, _templateResolver, _dedup, _suppression, _grouping,
               _auditService, _envBehavior, _configResolution, externalDelivery, _logger);

    [Fact]
    public async Task ProcessAsync_WithExternalDeliveryService_TriggersExternalDelivery()
    {
        var orchestrator = CreateOrchestrator(_externalDelivery);

        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Critical),
            Title = "Incident created — payments-api",
            Message = "Critical incident.",
            SourceModule = "OperationalIntelligence",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = [Guid.NewGuid()]
        };

        await orchestrator.ProcessAsync(request);

        await _externalDelivery.Received(1).ProcessExternalDeliveryAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithoutExternalDeliveryService_StillSucceeds()
    {
        var orchestrator = CreateOrchestrator(null);

        var request = new NotificationRequest
        {
            EventType = NotificationType.ApprovalPending,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.ActionRequired),
            Title = "Approval required",
            Message = "Review and decide.",
            SourceModule = "ChangeGovernance",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = [Guid.NewGuid()]
        };

        var result = await orchestrator.ProcessAsync(request);

        result.Success.Should().BeTrue();
        result.NotificationIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessAsync_ExternalDeliveryFails_InternalNotificationStillCreated()
    {
        _externalDelivery
            .ProcessExternalDeliveryAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("External delivery failed"));

        var orchestrator = CreateOrchestrator(_externalDelivery);

        var request = new NotificationRequest
        {
            EventType = NotificationType.BreakGlassActivated,
            Category = nameof(NotificationCategory.Security),
            Severity = nameof(NotificationSeverity.Critical),
            Title = "Break-glass activated",
            Message = "Emergency access activated.",
            SourceModule = "Identity",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = [Guid.NewGuid()]
        };

        var result = await orchestrator.ProcessAsync(request);

        // Internal notification should still be created
        result.Success.Should().BeTrue();
        result.NotificationIds.Should().HaveCount(1);
        await _store.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MultipleRecipients_TriggersExternalDeliveryForEach()
    {
        var orchestrator = CreateOrchestrator(_externalDelivery);

        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Critical),
            Title = "Test",
            Message = "Test",
            SourceModule = "TestModule",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]
        };

        await orchestrator.ProcessAsync(request);

        await _externalDelivery.Received(3).ProcessExternalDeliveryAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
