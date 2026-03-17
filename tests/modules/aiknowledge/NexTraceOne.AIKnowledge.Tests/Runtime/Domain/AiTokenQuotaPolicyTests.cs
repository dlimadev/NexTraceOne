using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Domain;

/// <summary>Testes unitários da entidade AiTokenQuotaPolicy.</summary>
public sealed class AiTokenQuotaPolicyTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreatePolicy()
    {
        var policy = AiTokenQuotaPolicy.Create(
            "default-user-quota", "Standard user quota",
            "user", "user-123", null, null,
            maxInputTokensPerRequest: 4000,
            maxOutputTokensPerRequest: 4000,
            maxTotalTokensPerRequest: 8000,
            maxTokensPerDay: 100_000,
            maxTokensPerMonth: 2_000_000,
            maxTokensAccumulated: 50_000_000,
            isHardLimit: true,
            allowSensitiveData: false,
            allowKnowledgePromotion: true);

        policy.Name.Should().Be("default-user-quota");
        policy.Description.Should().Be("Standard user quota");
        policy.Scope.Should().Be("user");
        policy.ScopeValue.Should().Be("user-123");
        policy.ProviderId.Should().BeNull();
        policy.ModelId.Should().BeNull();
        policy.MaxInputTokensPerRequest.Should().Be(4000);
        policy.MaxOutputTokensPerRequest.Should().Be(4000);
        policy.MaxTotalTokensPerRequest.Should().Be(8000);
        policy.MaxTokensPerDay.Should().Be(100_000);
        policy.MaxTokensPerMonth.Should().Be(2_000_000);
        policy.MaxTokensAccumulated.Should().Be(50_000_000);
        policy.IsHardLimit.Should().BeTrue();
        policy.AllowSensitiveData.Should().BeFalse();
        policy.AllowKnowledgePromotion.Should().BeTrue();
        policy.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => AiTokenQuotaPolicy.Create(
            null!, "desc", "user", "u1", null, null,
            1000, 1000, 2000, 50_000, 1_000_000, 10_000_000,
            true, false, false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabled()
    {
        var policy = CreateDefaultPolicy();
        policy.Disable();

        var result = policy.Enable();

        result.IsSuccess.Should().BeTrue();
        policy.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabled()
    {
        var policy = CreateDefaultPolicy();

        var result = policy.Disable();

        result.IsSuccess.Should().BeTrue();
        policy.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldModifyFields()
    {
        var policy = CreateDefaultPolicy();

        var result = policy.Update(
            "Updated description",
            maxInputTokensPerRequest: 8000,
            maxOutputTokensPerRequest: 8000,
            maxTotalTokensPerRequest: 16000,
            maxTokensPerDay: 200_000,
            maxTokensPerMonth: 4_000_000,
            maxTokensAccumulated: 100_000_000,
            isHardLimit: false,
            allowSensitiveData: true,
            allowKnowledgePromotion: false);

        result.IsSuccess.Should().BeTrue();
        policy.Description.Should().Be("Updated description");
        policy.MaxInputTokensPerRequest.Should().Be(8000);
        policy.MaxOutputTokensPerRequest.Should().Be(8000);
        policy.MaxTotalTokensPerRequest.Should().Be(16000);
        policy.MaxTokensPerDay.Should().Be(200_000);
        policy.MaxTokensPerMonth.Should().Be(4_000_000);
        policy.MaxTokensAccumulated.Should().Be(100_000_000);
        policy.IsHardLimit.Should().BeFalse();
        policy.AllowSensitiveData.Should().BeTrue();
        policy.AllowKnowledgePromotion.Should().BeFalse();
    }

    private static AiTokenQuotaPolicy CreateDefaultPolicy() =>
        AiTokenQuotaPolicy.Create(
            "test-policy", "Test policy",
            "user", "user-1", null, null,
            4000, 4000, 8000,
            100_000, 2_000_000, 50_000_000,
            true, false, true);
}
