using NexTraceOne.BuildingBlocks.Application.Correlation;

namespace NexTraceOne.BuildingBlocks.Application.Tests.Correlation;

public sealed class PromotionRiskAssessmentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SourceEnvId = Guid.NewGuid();
    private static readonly Guid TargetEnvId = Guid.NewGuid();

    [Fact]
    public void ShouldBlock_WhenRiskLevelHigh_ShouldBeTrue()
    {
        var assessment = new PromotionRiskAssessment
        {
            TenantId = TenantId,
            SourceEnvironmentId = SourceEnvId,
            TargetEnvironmentId = TargetEnvId,
            ServiceName = "order-api",
            AssessedAt = DateTimeOffset.UtcNow,
            RiskLevel = PromotionRiskLevel.High,
            RiskScore = 0.8
        };

        assessment.ShouldBlock.Should().BeTrue();
    }

    [Fact]
    public void ShouldBlock_WhenRiskLevelCritical_ShouldBeTrue()
    {
        var assessment = new PromotionRiskAssessment
        {
            TenantId = TenantId,
            SourceEnvironmentId = SourceEnvId,
            TargetEnvironmentId = TargetEnvId,
            ServiceName = "payment-api",
            AssessedAt = DateTimeOffset.UtcNow,
            RiskLevel = PromotionRiskLevel.Critical,
            RiskScore = 1.0
        };

        assessment.ShouldBlock.Should().BeTrue();
    }

    [Theory]
    [InlineData(PromotionRiskLevel.None)]
    [InlineData(PromotionRiskLevel.Low)]
    [InlineData(PromotionRiskLevel.Medium)]
    public void ShouldBlock_WhenRiskLevelBelowHigh_ShouldBeFalse(PromotionRiskLevel level)
    {
        var assessment = new PromotionRiskAssessment
        {
            TenantId = TenantId,
            SourceEnvironmentId = SourceEnvId,
            TargetEnvironmentId = TargetEnvId,
            ServiceName = "catalog-api",
            AssessedAt = DateTimeOffset.UtcNow,
            RiskLevel = level,
            RiskScore = 0.1
        };

        assessment.ShouldBlock.Should().BeFalse();
    }

}
