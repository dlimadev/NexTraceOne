using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

public sealed class NotificationTypeTests
{
    [Fact]
    public void All_ShouldContainAllDeclaredTypes()
    {
        NotificationType.All.Should().HaveCount(36);
    }

    [Theory]
    [InlineData(NotificationType.IncidentCreated)]
    [InlineData(NotificationType.IncidentEscalated)]
    [InlineData(NotificationType.IncidentResolved)]
    [InlineData(NotificationType.AnomalyDetected)]
    [InlineData(NotificationType.HealthDegradation)]
    [InlineData(NotificationType.ApprovalPending)]
    [InlineData(NotificationType.ApprovalApproved)]
    [InlineData(NotificationType.ApprovalRejected)]
    [InlineData(NotificationType.ApprovalExpiring)]
    [InlineData(NotificationType.ContractPublished)]
    [InlineData(NotificationType.BreakingChangeDetected)]
    [InlineData(NotificationType.ContractValidationFailed)]
    [InlineData(NotificationType.BreakGlassActivated)]
    [InlineData(NotificationType.JitAccessPending)]
    [InlineData(NotificationType.JitAccessGranted)]
    [InlineData(NotificationType.UserRoleChanged)]
    [InlineData(NotificationType.AccessReviewPending)]
    [InlineData(NotificationType.ComplianceCheckFailed)]
    [InlineData(NotificationType.PolicyViolated)]
    [InlineData(NotificationType.EvidenceExpiring)]
    [InlineData(NotificationType.BudgetExceeded)]
    [InlineData(NotificationType.BudgetThresholdReached)]
    [InlineData(NotificationType.IntegrationFailed)]
    [InlineData(NotificationType.SyncFailed)]
    [InlineData(NotificationType.ConnectorAuthFailed)]
    [InlineData(NotificationType.AiProviderUnavailable)]
    [InlineData(NotificationType.TokenBudgetExceeded)]
    [InlineData(NotificationType.AiGenerationFailed)]
    [InlineData(NotificationType.AiActionBlockedByPolicy)]
    // Change Intelligence types
    [InlineData(NotificationType.PromotionCompleted)]
    [InlineData(NotificationType.PromotionBlocked)]
    [InlineData(NotificationType.RollbackTriggered)]
    [InlineData(NotificationType.DeploymentCompleted)]
    [InlineData(NotificationType.ChangeConfidenceScored)]
    [InlineData(NotificationType.BlastRadiusHigh)]
    [InlineData(NotificationType.PostChangeVerificationFailed)]
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
