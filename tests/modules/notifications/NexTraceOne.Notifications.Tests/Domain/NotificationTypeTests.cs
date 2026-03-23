using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

public sealed class NotificationTypeTests
{
    [Fact]
    public void All_ShouldContainAllDeclaredTypes()
    {
        NotificationType.All.Should().HaveCount(11);
    }

    [Theory]
    [InlineData(NotificationType.IncidentCreated)]
    [InlineData(NotificationType.IncidentEscalated)]
    [InlineData(NotificationType.ApprovalPending)]
    [InlineData(NotificationType.ApprovalApproved)]
    [InlineData(NotificationType.ApprovalRejected)]
    [InlineData(NotificationType.BreakGlassActivated)]
    [InlineData(NotificationType.JitAccessPending)]
    [InlineData(NotificationType.ComplianceCheckFailed)]
    [InlineData(NotificationType.BudgetExceeded)]
    [InlineData(NotificationType.IntegrationFailed)]
    [InlineData(NotificationType.AiProviderUnavailable)]
    public void IsValid_ShouldReturnTrue_ForCatalogTypes(string eventType)
    {
        NotificationType.IsValid(eventType).Should().BeTrue();
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("")]
    [InlineData("incidentcreated")] // Case-sensitive
    public void IsValid_ShouldReturnFalse_ForInvalidTypes(string eventType)
    {
        NotificationType.IsValid(eventType).Should().BeFalse();
    }

    [Fact]
    public void All_ShouldContainUniqueTypes()
    {
        NotificationType.All.Distinct().Should().HaveCount(NotificationType.All.Count);
    }

    [Fact]
    public void All_ShouldNotContainEmptyOrNull()
    {
        NotificationType.All.Should().AllSatisfy(t =>
        {
            t.Should().NotBeNullOrWhiteSpace();
        });
    }
}
