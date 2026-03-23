using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Engine;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Engine;

public sealed class NotificationTemplateResolverTests
{
    private readonly INotificationTemplateResolver _resolver = new NotificationTemplateResolver();

    [Fact]
    public void Resolve_IncidentCreated_ReturnsCorrectTemplate()
    {
        var parameters = new Dictionary<string, string>
        {
            ["ServiceName"] = "payments-api",
            ["IncidentSeverity"] = "Critical"
        };

        var template = _resolver.Resolve(NotificationType.IncidentCreated, parameters);

        template.Title.Should().Contain("payments-api");
        template.Message.Should().Contain("Critical");
        template.Category.Should().Be(NotificationCategory.Incident);
        template.Severity.Should().Be(NotificationSeverity.Critical);
        template.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public void Resolve_IncidentEscalated_ReturnsCorrectTemplate()
    {
        var parameters = new Dictionary<string, string>
        {
            ["ServiceName"] = "auth-service"
        };

        var template = _resolver.Resolve(NotificationType.IncidentEscalated, parameters);

        template.Title.Should().Contain("auth-service");
        template.Category.Should().Be(NotificationCategory.Incident);
        template.Severity.Should().Be(NotificationSeverity.Critical);
        template.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public void Resolve_ApprovalPending_ReturnsActionRequired()
    {
        var parameters = new Dictionary<string, string>
        {
            ["EntityName"] = "Release v2.0",
            ["RequestedBy"] = "john.doe"
        };

        var template = _resolver.Resolve(NotificationType.ApprovalPending, parameters);

        template.Title.Should().Contain("Release v2.0");
        template.Message.Should().Contain("john.doe");
        template.Category.Should().Be(NotificationCategory.Approval);
        template.Severity.Should().Be(NotificationSeverity.ActionRequired);
        template.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public void Resolve_ApprovalApproved_ReturnsInfoSeverity()
    {
        var parameters = new Dictionary<string, string>
        {
            ["EntityName"] = "Release v2.0",
            ["ApprovedBy"] = "manager.smith"
        };

        var template = _resolver.Resolve(NotificationType.ApprovalApproved, parameters);

        template.Title.Should().Contain("Release v2.0");
        template.Message.Should().Contain("manager.smith");
        template.Category.Should().Be(NotificationCategory.Approval);
        template.Severity.Should().Be(NotificationSeverity.Info);
        template.RequiresAction.Should().BeFalse();
    }

    [Fact]
    public void Resolve_ApprovalRejected_ReturnsWarningWithReason()
    {
        var parameters = new Dictionary<string, string>
        {
            ["EntityName"] = "Deployment",
            ["RejectedBy"] = "security.lead",
            ["Reason"] = "Missing tests"
        };

        var template = _resolver.Resolve(NotificationType.ApprovalRejected, parameters);

        template.Title.Should().Contain("Deployment");
        template.Message.Should().Contain("security.lead");
        template.Message.Should().Contain("Missing tests");
        template.Category.Should().Be(NotificationCategory.Approval);
        template.Severity.Should().Be(NotificationSeverity.Warning);
    }

    [Fact]
    public void Resolve_BreakGlassActivated_ReturnsCritical()
    {
        var parameters = new Dictionary<string, string>
        {
            ["ActivatedBy"] = "admin.user"
        };

        var template = _resolver.Resolve(NotificationType.BreakGlassActivated, parameters);

        template.Title.Should().Contain("Break-glass");
        template.Message.Should().Contain("admin.user");
        template.Category.Should().Be(NotificationCategory.Security);
        template.Severity.Should().Be(NotificationSeverity.Critical);
        template.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public void Resolve_ComplianceCheckFailed_ReturnsWarning()
    {
        var parameters = new Dictionary<string, string>
        {
            ["ServiceName"] = "billing-service",
            ["GapCount"] = "3"
        };

        var template = _resolver.Resolve(NotificationType.ComplianceCheckFailed, parameters);

        template.Title.Should().Contain("billing-service");
        template.Message.Should().Contain("3");
        template.Category.Should().Be(NotificationCategory.Compliance);
        template.Severity.Should().Be(NotificationSeverity.Warning);
        template.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public void Resolve_BudgetExceeded_ReturnsCostContext()
    {
        var parameters = new Dictionary<string, string>
        {
            ["ServiceName"] = "data-pipeline",
            ["ExpectedCost"] = "$500.00",
            ["ActualCost"] = "$1,200.00"
        };

        var template = _resolver.Resolve(NotificationType.BudgetExceeded, parameters);

        template.Title.Should().Contain("data-pipeline");
        template.Message.Should().Contain("$500.00");
        template.Message.Should().Contain("$1,200.00");
        template.Category.Should().Be(NotificationCategory.FinOps);
        template.Severity.Should().Be(NotificationSeverity.Warning);
    }

    [Fact]
    public void Resolve_IntegrationFailed_ReturnsErrorContext()
    {
        var parameters = new Dictionary<string, string>
        {
            ["IntegrationName"] = "Azure DevOps",
            ["ErrorMessage"] = "Connection timeout"
        };

        var template = _resolver.Resolve(NotificationType.IntegrationFailed, parameters);

        template.Title.Should().Contain("Azure DevOps");
        template.Message.Should().Contain("Connection timeout");
        template.Category.Should().Be(NotificationCategory.Integration);
        template.Severity.Should().Be(NotificationSeverity.Warning);
    }

    [Fact]
    public void Resolve_AiProviderUnavailable_ReturnsProviderContext()
    {
        var parameters = new Dictionary<string, string>
        {
            ["ProviderName"] = "OpenAI"
        };

        var template = _resolver.Resolve(NotificationType.AiProviderUnavailable, parameters);

        template.Title.Should().Contain("OpenAI");
        template.Category.Should().Be(NotificationCategory.AI);
        template.Severity.Should().Be(NotificationSeverity.Warning);
        template.RequiresAction.Should().BeFalse();
    }

    [Fact]
    public void Resolve_UnknownType_ReturnsGenericTemplate()
    {
        var parameters = new Dictionary<string, string>();

        var template = _resolver.Resolve("UnknownEventType", parameters);

        template.Title.Should().Be("UnknownEventType");
        template.Category.Should().Be(NotificationCategory.Informational);
        template.Severity.Should().Be(NotificationSeverity.Info);
        template.RequiresAction.Should().BeFalse();
    }

    [Fact]
    public void Resolve_MissingParameters_UsesFallbackValues()
    {
        var parameters = new Dictionary<string, string>();

        var template = _resolver.Resolve(NotificationType.IncidentCreated, parameters);

        template.Title.Should().Contain("Unknown service");
        template.Message.Should().Contain("Unknown");
        template.Category.Should().Be(NotificationCategory.Incident);
    }

    [Theory]
    [MemberData(nameof(AllNotificationTypes))]
    public void Resolve_AllCatalogTypes_ShouldReturnNonEmptyTemplates(string eventType)
    {
        var parameters = new Dictionary<string, string>();

        var template = _resolver.Resolve(eventType, parameters);

        template.Title.Should().NotBeNullOrWhiteSpace();
        template.Message.Should().NotBeNullOrWhiteSpace();
    }

    public static TheoryData<string> AllNotificationTypes()
    {
        var data = new TheoryData<string>();
        foreach (var type in NotificationType.All)
        {
            data.Add(type);
        }
        return data;
    }
}
