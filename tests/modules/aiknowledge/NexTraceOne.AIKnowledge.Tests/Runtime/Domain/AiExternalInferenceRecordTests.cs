using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Domain;

/// <summary>Testes unitários da entidade AiExternalInferenceRecord.</summary>
public sealed class AiExternalInferenceRecordTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_WithValidData_ShouldCreateRecord()
    {
        var record = AiExternalInferenceRecord.Create(
            userId: "user-1",
            tenantId: TestTenantId,
            providerId: "openai",
            modelName: "gpt-4o",
            originalPrompt: "Explain this API contract",
            additionalContext: "Service: order-api",
            response: "The API contract defines...",
            sensitivityClassification: "internal",
            qualityScore: 4);

        record.UserId.Should().Be("user-1");
        record.TenantId.Should().Be(TestTenantId);
        record.ProviderId.Should().Be("openai");
        record.ModelName.Should().Be("gpt-4o");
        record.OriginalPrompt.Should().Be("Explain this API contract");
        record.AdditionalContext.Should().Be("Service: order-api");
        record.Response.Should().Be("The API contract defines...");
        record.SensitivityClassification.Should().Be("internal");
        record.QualityScore.Should().Be(4);
    }

    [Fact]
    public void Create_DefaultStatus_ShouldBePending()
    {
        var record = CreateDefaultRecord();

        record.PromotionStatus.Should().Be(AiKnowledgePromotionStatus.Pending);
        record.CanPromoteToSharedMemory.Should().BeFalse();
        record.ReviewedAt.Should().BeNull();
        record.ReviewedBy.Should().BeNull();
    }

    [Fact]
    public void Approve_ShouldChangeStatus()
    {
        var record = CreateDefaultRecord();

        var result = record.Approve("reviewer-1", FixedNow);

        result.IsSuccess.Should().BeTrue();
        record.PromotionStatus.Should().Be(AiKnowledgePromotionStatus.Approved);
        record.CanPromoteToSharedMemory.Should().BeTrue();
        record.ReviewedAt.Should().Be(FixedNow);
        record.ReviewedBy.Should().Be("reviewer-1");
    }

    [Fact]
    public void Reject_ShouldChangeStatus()
    {
        var record = CreateDefaultRecord();

        var result = record.Reject("reviewer-2", FixedNow);

        result.IsSuccess.Should().BeTrue();
        record.PromotionStatus.Should().Be(AiKnowledgePromotionStatus.Rejected);
        record.CanPromoteToSharedMemory.Should().BeFalse();
        record.ReviewedAt.Should().Be(FixedNow);
        record.ReviewedBy.Should().Be("reviewer-2");
    }

    [Fact]
    public void MarkForReview_ShouldChangeStatus()
    {
        var record = CreateDefaultRecord();

        var result = record.MarkForReview();

        result.IsSuccess.Should().BeTrue();
        record.PromotionStatus.Should().Be(AiKnowledgePromotionStatus.UnderReview);
    }

    private static AiExternalInferenceRecord CreateDefaultRecord() =>
        AiExternalInferenceRecord.Create(
            "user-1", TestTenantId, "openai", "gpt-4o",
            "Test prompt", null, "Test response",
            "internal", null);
}
