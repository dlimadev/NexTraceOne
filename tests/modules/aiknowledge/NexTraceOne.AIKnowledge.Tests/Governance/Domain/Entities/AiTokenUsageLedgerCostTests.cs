using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class AiTokenUsageLedgerCostTests
{
    // ── Cost attribution ────────────────────────────────────────────────

    [Fact]
    public void Record_With_Cost_Fields_Stores_Values()
    {
        var entry = AiTokenUsageLedger.Record(
            userId: "user-1",
            tenantId: Guid.NewGuid(),
            providerId: "openai",
            modelId: "gpt-4o",
            modelName: "GPT-4o",
            promptTokens: 500,
            completionTokens: 200,
            totalTokens: 700,
            policyId: null,
            policyName: null,
            isBlocked: false,
            blockReason: null,
            requestId: "req-001",
            executionId: "exec-001",
            timestamp: DateTimeOffset.UtcNow,
            status: "Success",
            durationMs: 1500.0,
            costPerInputToken: 0.000005m,
            costPerOutputToken: 0.000015m,
            estimatedCostUsd: 0.005500m,
            costCurrency: "USD");

        entry.CostPerInputToken.Should().Be(0.000005m);
        entry.CostPerOutputToken.Should().Be(0.000015m);
        entry.EstimatedCostUsd.Should().Be(0.005500m);
        entry.CostCurrency.Should().Be("USD");
    }

    [Fact]
    public void Record_Without_Cost_Fields_Has_Null_Costs()
    {
        var entry = AiTokenUsageLedger.Record(
            userId: "user-1",
            tenantId: Guid.NewGuid(),
            providerId: "ollama",
            modelId: "llama3.2:3b",
            modelName: "Llama 3.2 3B",
            promptTokens: 100,
            completionTokens: 50,
            totalTokens: 150,
            policyId: null,
            policyName: null,
            isBlocked: false,
            blockReason: null,
            requestId: "req-002",
            executionId: "exec-002",
            timestamp: DateTimeOffset.UtcNow,
            status: "Success",
            durationMs: 500.0);

        entry.CostPerInputToken.Should().BeNull();
        entry.CostPerOutputToken.Should().BeNull();
        entry.EstimatedCostUsd.Should().BeNull();
        entry.CostCurrency.Should().BeNull();
    }

    [Fact]
    public void Record_With_Zero_Cost_Is_Valid()
    {
        var entry = AiTokenUsageLedger.Record(
            userId: "user-1",
            tenantId: Guid.NewGuid(),
            providerId: "ollama",
            modelId: "llama3.2:3b",
            modelName: "Llama 3.2 3B",
            promptTokens: 100,
            completionTokens: 50,
            totalTokens: 150,
            policyId: null,
            policyName: null,
            isBlocked: false,
            blockReason: null,
            requestId: "req-003",
            executionId: "exec-003",
            timestamp: DateTimeOffset.UtcNow,
            status: "Success",
            durationMs: 500.0,
            costPerInputToken: 0m,
            costPerOutputToken: 0m,
            estimatedCostUsd: 0m,
            costCurrency: "USD");

        entry.CostPerInputToken.Should().Be(0m);
        entry.CostPerOutputToken.Should().Be(0m);
        entry.EstimatedCostUsd.Should().Be(0m);
    }

    [Fact]
    public void Record_Backward_Compatible_Without_Cost_Parameters()
    {
        // Existing callers that don't pass cost params should still work
        var entry = AiTokenUsageLedger.Record(
            userId: "user-1",
            tenantId: Guid.NewGuid(),
            providerId: "provider-1",
            modelId: "model-1",
            modelName: "Model One",
            promptTokens: 10,
            completionTokens: 20,
            totalTokens: 30,
            policyId: Guid.NewGuid(),
            policyName: "policy-1",
            isBlocked: false,
            blockReason: null,
            requestId: "req-004",
            executionId: "exec-004",
            timestamp: DateTimeOffset.UtcNow,
            status: "Success",
            durationMs: 100.0);

        entry.PromptTokens.Should().Be(10);
        entry.CompletionTokens.Should().Be(20);
        entry.TotalTokens.Should().Be(30);
        entry.CostPerInputToken.Should().BeNull("cost fields are optional for backward compatibility");
        entry.CostPerOutputToken.Should().BeNull();
        entry.EstimatedCostUsd.Should().BeNull();
        entry.CostCurrency.Should().BeNull();
    }
}
