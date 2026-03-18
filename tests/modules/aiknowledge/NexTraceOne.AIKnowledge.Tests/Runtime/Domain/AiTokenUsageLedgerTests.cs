using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Domain;

/// <summary>Testes unitários da entidade AiTokenUsageLedger.</summary>
public sealed class AiTokenUsageLedgerTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_WithValidData_ShouldCreateEntry()
    {
        var policyId = Guid.NewGuid();

        var entry = AiTokenUsageLedger.Record(
            userId: "user-1",
            tenantId: "tenant-1",
            providerId: "openai",
            modelId: "gpt-4o",
            modelName: "GPT-4o",
            promptTokens: 200,
            completionTokens: 300,
            totalTokens: 500,
            policyId: policyId,
            policyName: "Default Policy",
            isBlocked: false,
            blockReason: null,
            requestId: "req-001",
            executionId: "exec-001",
            timestamp: FixedNow,
            status: "Success",
            durationMs: 1234.5);

        entry.UserId.Should().Be("user-1");
        entry.TenantId.Should().Be("tenant-1");
        entry.ProviderId.Should().Be("openai");
        entry.ModelId.Should().Be("gpt-4o");
        entry.ModelName.Should().Be("GPT-4o");
        entry.PromptTokens.Should().Be(200);
        entry.CompletionTokens.Should().Be(300);
        entry.TotalTokens.Should().Be(500);
        entry.PolicyId.Should().Be(policyId);
        entry.PolicyName.Should().Be("Default Policy");
        entry.IsBlocked.Should().BeFalse();
        entry.BlockReason.Should().BeNull();
        entry.RequestId.Should().Be("req-001");
        entry.ExecutionId.Should().Be("exec-001");
        entry.Timestamp.Should().Be(FixedNow);
        entry.Status.Should().Be("Success");
        entry.DurationMs.Should().Be(1234.5);
    }

    [Fact]
    public void Record_TotalTokens_ShouldSumPromptAndCompletion()
    {
        var entry = AiTokenUsageLedger.Record(
            "user-1", "tenant-1", "ollama", "llama3", "Llama 3",
            promptTokens: 1000, completionTokens: 2500, totalTokens: 3500,
            null, null, false, null,
            "req-002", "exec-002", FixedNow, "Success", 500.0);

        entry.PromptTokens.Should().Be(1000);
        entry.CompletionTokens.Should().Be(2500);
        entry.TotalTokens.Should().Be(3500);
        (entry.PromptTokens + entry.CompletionTokens).Should().Be(entry.TotalTokens);
    }

    [Fact]
    public void Record_WithBlocking_ShouldSetBlockFields()
    {
        var policyId = Guid.NewGuid();

        var entry = AiTokenUsageLedger.Record(
            "user-1", "tenant-1", "openai", "gpt-4o", "GPT-4o",
            0, 0, 0,
            policyId, "Strict Policy",
            isBlocked: true, blockReason: "Daily limit exceeded",
            "req-003", "exec-003", FixedNow, "Blocked", 0.0);

        entry.IsBlocked.Should().BeTrue();
        entry.BlockReason.Should().Be("Daily limit exceeded");
        entry.PolicyId.Should().Be(policyId);
        entry.PolicyName.Should().Be("Strict Policy");
        entry.Status.Should().Be("Blocked");
    }
}
