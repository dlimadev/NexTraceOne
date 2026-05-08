using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPerformanceBenchmarkReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiCapabilityMaturityReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetOrganizationalMemoryHealthReport;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Unit tests for Wave BD — AI Organizational Intelligence &amp; Memory Analytics.
/// Covers: BD.1 (GetOrganizationalMemoryHealthReport), BD.2 (GetAgentPerformanceBenchmarkReport),
/// BD.3 (GetAiCapabilityMaturityReport) — tier classification, scoring logic, validation.
/// </summary>
public sealed class WaveBdIntelligenceReportTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken Ct = CancellationToken.None;

    // ── BD.1: GetOrganizationalMemoryHealthReport ─────────────────────────

    [Fact]
    public async Task BD1_EmptyMemory_ReturnsTierEmpty()
    {
        var repo = Substitute.For<IOrganizationalMemoryRepository>();
        repo.ListByTypeAsync(Arg.Any<string>(), TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<OrganizationalMemoryNode>>([]));

        var handler = new GetOrganizationalMemoryHealthReport.Handler(repo);
        var result = await handler.Handle(new GetOrganizationalMemoryHealthReport.Query(TenantId), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MemoryHealthTier.Should().Be("Empty");
        result.Value.TotalNodes.Should().Be(0);
    }

    [Fact]
    public async Task BD1_FewNodes_ReturnsTierSparse()
    {
        var nodes = new List<OrganizationalMemoryNode>
        {
            CreateMemoryNode("decision", DateTimeOffset.UtcNow.AddDays(-5), 0.5)
        };

        var repo = SetupMemoryRepo(TenantId, nodes, forType: "decision");

        var handler = new GetOrganizationalMemoryHealthReport.Handler(repo);
        var result = await handler.Handle(
            new GetOrganizationalMemoryHealthReport.Query(TenantId, LookbackDays: 90, StaleThresholdDays: 30), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MemoryHealthTier.Should().Be("Sparse");
    }

    [Theory]
    [InlineData(70.0, 45.0, 0.75, "Thriving")]
    [InlineData(55.0, 25.0, 0.6, "Active")]
    [InlineData(35.0, 10.0, 0.4, "Building")]
    [InlineData(20.0, 5.0, 0.3, "Sparse")]
    public void BD1_ClassifyTier_VariousInputs_ReturnsExpectedTier(
        double freshnessRate, double connectRate, double avgRel, string expectedTier)
    {
        var method = typeof(GetOrganizationalMemoryHealthReport.Handler)
            .GetMethod("ClassifyTier",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull();

        var result = (string)method!.Invoke(null, [5, freshnessRate, connectRate, avgRel])!;
        result.Should().Be(expectedTier);
    }

    [Fact]
    public async Task BD1_FreshnessRateCalculatedCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var nodes = new List<OrganizationalMemoryNode>
        {
            CreateMemoryNode("decision", now.AddDays(-5), 0.8),
            CreateMemoryNode("decision", now.AddDays(-5), 0.7),
            CreateMemoryNode("decision", now.AddDays(-40), 0.5),
            CreateMemoryNode("decision", now.AddDays(-40), 0.4),
        };

        var repo = SetupMemoryRepo(TenantId, nodes, forType: "decision");

        var handler = new GetOrganizationalMemoryHealthReport.Handler(repo);
        var result = await handler.Handle(
            new GetOrganizationalMemoryHealthReport.Query(TenantId, LookbackDays: 90, StaleThresholdDays: 30), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FreshNodes.Should().Be(2);
        result.Value.StaleNodes.Should().Be(2);
        result.Value.FreshnessRatePct.Should().BeApproximately(50.0, 0.1);
    }

    [Fact]
    public void BD1_Validator_InvalidTenantId_Fails()
    {
        var validator = new GetOrganizationalMemoryHealthReport.Validator();
        var result = validator.Validate(new GetOrganizationalMemoryHealthReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BD1_Validator_LookbackDaysOutOfRange_Fails()
    {
        var validator = new GetOrganizationalMemoryHealthReport.Validator();
        var result = validator.Validate(
            new GetOrganizationalMemoryHealthReport.Query(TenantId, LookbackDays: 400));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BD1_Validator_ValidQuery_Passes()
    {
        var validator = new GetOrganizationalMemoryHealthReport.Validator();
        var result = validator.Validate(
            new GetOrganizationalMemoryHealthReport.Query(TenantId, LookbackDays: 90, StaleThresholdDays: 30));
        result.IsValid.Should().BeTrue();
    }

    // ── BD.2: GetAgentPerformanceBenchmarkReport ──────────────────────────

    [Fact]
    public async Task BD2_NoAgentMetrics_ReturnsEmptyReport()
    {
        var repo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        repo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([]));

        var handler = new GetAgentPerformanceBenchmarkReport.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPerformanceBenchmarkReport.Query(TenantId), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QualifiedAgents.Should().Be(0);
        result.Value.AgentBenchmarks.Should().BeEmpty();
        result.Value.TopPerformerName.Should().BeNull();
    }

    [Fact]
    public async Task BD2_AgentBelowMinExecutions_ExcludedFromBenchmark()
    {
        var agent = CreateMetric("low-exec-agent", totalExecutions: 2, accuracy: 0.9, rating: 5.0);
        var repo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        repo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([agent]));

        var handler = new GetAgentPerformanceBenchmarkReport.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPerformanceBenchmarkReport.Query(TenantId, MinExecutions: 5), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.QualifiedAgents.Should().Be(0);
        result.Value.TotalAgentsEvaluated.Should().Be(1);
    }

    [Theory]
    [InlineData(0.95, 5.0, 1.0, 10, "Champion")]
    [InlineData(0.70, 4.0, 0.70, 3, "HighPerformer")]
    [InlineData(0.55, 3.0, 0.50, 0, "Active")]
    [InlineData(0.30, 2.0, 0.30, 0, "Developing")]
    [InlineData(0.05, 1.0, 0.10, 0, "Underperforming")]
    public async Task BD2_BenchmarkTier_ClassifiedCorrectly(
        double accuracy, double rating, double feedbackCoverage, int rlCycles, string expectedTier)
    {
        var executionsWithFeedback = (long)(100 * feedbackCoverage);
        var agent = CreateMetric("agent-x", totalExecutions: 100, accuracy: accuracy,
            rating: rating, execWithFeedback: executionsWithFeedback, rlCycles: rlCycles);

        var repo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        repo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([agent]));

        var handler = new GetAgentPerformanceBenchmarkReport.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPerformanceBenchmarkReport.Query(TenantId, MinExecutions: 5), Ct);

        result.Value!.AgentBenchmarks.Should().HaveCount(1);
        result.Value.AgentBenchmarks[0].BenchmarkTier.Should().Be(expectedTier);
    }

    [Fact]
    public async Task BD2_TopPerformerIsHighestScoringAgent()
    {
        var agentA = CreateMetric("agent-a", totalExecutions: 100, accuracy: 0.9, rating: 4.8, execWithFeedback: 90);
        var agentB = CreateMetric("agent-b", totalExecutions: 100, accuracy: 0.3, rating: 2.0, execWithFeedback: 20);

        var repo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        repo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([agentA, agentB]));

        var handler = new GetAgentPerformanceBenchmarkReport.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPerformanceBenchmarkReport.Query(TenantId, MinExecutions: 5), Ct);

        result.Value!.TopPerformerName.Should().Be("agent-a");
    }

    [Fact]
    public void BD2_Validator_InvalidTenantId_Fails()
    {
        var validator = new GetAgentPerformanceBenchmarkReport.Validator();
        var result = validator.Validate(new GetAgentPerformanceBenchmarkReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BD2_Validator_ValidQuery_Passes()
    {
        var validator = new GetAgentPerformanceBenchmarkReport.Validator();
        var result = validator.Validate(new GetAgentPerformanceBenchmarkReport.Query(TenantId, MinExecutions: 10));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BD2_Validator_MinExecutionsTooLow_Fails()
    {
        var validator = new GetAgentPerformanceBenchmarkReport.Validator();
        var result = validator.Validate(new GetAgentPerformanceBenchmarkReport.Query(TenantId, MinExecutions: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task BD2_TierSummaryContainsAllFiveTiers()
    {
        var repo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        repo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([]));

        var handler = new GetAgentPerformanceBenchmarkReport.Handler(repo);
        var result = await handler.Handle(
            new GetAgentPerformanceBenchmarkReport.Query(TenantId), Ct);

        result.Value!.TierSummary.Should().ContainKey("Champion");
        result.Value.TierSummary.Should().ContainKey("HighPerformer");
        result.Value.TierSummary.Should().ContainKey("Active");
        result.Value.TierSummary.Should().ContainKey("Developing");
        result.Value.TierSummary.Should().ContainKey("Underperforming");
    }

    // ── BD.3: GetAiCapabilityMaturityReport ───────────────────────────────

    [Fact]
    public async Task BD3_NoAgentsNoSkills_ReturnsTierInitiating()
    {
        var metricRepo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        var skillRepo = Substitute.For<IAiSkillRepository>();
        var memoryRepo = Substitute.For<IOrganizationalMemoryRepository>();

        metricRepo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([]));
        skillRepo.ListAsync(null, null, TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiSkill>>([]));
        memoryRepo.ListByTypeAsync("decision", TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<OrganizationalMemoryNode>>([]));

        var handler = new GetAiCapabilityMaturityReport.Handler(metricRepo, skillRepo, memoryRepo);
        var result = await handler.Handle(
            new GetAiCapabilityMaturityReport.Query(TenantId), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MaturityLevel.Should().Be("Initiating");
        result.Value.MaturityScore.Should().Be(0.0);
    }

    [Fact]
    public async Task BD3_FiveActiveAgentsTenSkills_MaturityScoreAbove40()
    {
        var metrics = Enumerable.Range(0, 5).Select(i =>
            CreateMetric($"agent-{i}", totalExecutions: 50, accuracy: 0.7, rating: 4.0, execWithFeedback: 30))
            .ToList();

        var skills = Enumerable.Range(0, 10).Select(i => CreateSkill(SkillStatus.Active)).ToList();

        var metricRepo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        var skillRepo = Substitute.For<IAiSkillRepository>();
        var memoryRepo = Substitute.For<IOrganizationalMemoryRepository>();

        metricRepo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>(metrics));
        skillRepo.ListAsync(null, null, TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiSkill>>(skills));
        memoryRepo.ListByTypeAsync("decision", TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<OrganizationalMemoryNode>>([]));

        var handler = new GetAiCapabilityMaturityReport.Handler(metricRepo, skillRepo, memoryRepo);
        var result = await handler.Handle(
            new GetAiCapabilityMaturityReport.Query(TenantId, MinTeamExecutions: 10), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MaturityScore.Should().BeGreaterThanOrEqualTo(40.0);
    }

    [Fact]
    public async Task BD3_PioneerAdoptionFlagSetWhenRlAboveThreshold()
    {
        var metrics = Enumerable.Range(0, 5).Select(i =>
            CreateMetric($"agent-{i}", totalExecutions: 50, accuracy: 0.7, rating: 4.0,
                execWithFeedback: 30, rlCycles: i < 2 ? 3 : 0))
            .ToList();

        var metricRepo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        var skillRepo = Substitute.For<IAiSkillRepository>();
        var memoryRepo = Substitute.For<IOrganizationalMemoryRepository>();

        metricRepo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>(metrics));
        skillRepo.ListAsync(null, null, TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiSkill>>([]));
        memoryRepo.ListByTypeAsync("decision", TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<OrganizationalMemoryNode>>([]));

        var handler = new GetAiCapabilityMaturityReport.Handler(metricRepo, skillRepo, memoryRepo);
        var result = await handler.Handle(
            new GetAiCapabilityMaturityReport.Query(TenantId, PioneerThresholdPct: 20), Ct);

        // 2 of 5 agents have RL cycles = 40% adoption, above 20% threshold
        result.Value!.HasPioneerAdoption.Should().BeTrue();
        result.Value.RlAdoptionPct.Should().BeApproximately(40.0, 0.1);
    }

    [Fact]
    public async Task BD3_MaturityDimensionsFourItems()
    {
        var metricRepo = Substitute.For<IAiAgentPerformanceMetricRepository>();
        var skillRepo = Substitute.For<IAiSkillRepository>();
        var memoryRepo = Substitute.For<IOrganizationalMemoryRepository>();

        metricRepo.ListByTenantAsync(TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiAgentPerformanceMetric>>([]));
        skillRepo.ListAsync(null, null, TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<AiSkill>>([]));
        memoryRepo.ListByTypeAsync("decision", TenantId, Ct)
            .Returns(Task.FromResult<IReadOnlyList<OrganizationalMemoryNode>>([]));

        var handler = new GetAiCapabilityMaturityReport.Handler(metricRepo, skillRepo, memoryRepo);
        var result = await handler.Handle(
            new GetAiCapabilityMaturityReport.Query(TenantId), Ct);

        result.Value!.MaturityDimensions.Should().HaveCount(4);
    }

    [Fact]
    public void BD3_Validator_InvalidTenantId_Fails()
    {
        var validator = new GetAiCapabilityMaturityReport.Validator();
        var result = validator.Validate(new GetAiCapabilityMaturityReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BD3_Validator_PioneerThresholdOutOfRange_Fails()
    {
        var validator = new GetAiCapabilityMaturityReport.Validator();
        var result = validator.Validate(
            new GetAiCapabilityMaturityReport.Query(TenantId, PioneerThresholdPct: 150));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BD3_Validator_ValidQuery_Passes()
    {
        var validator = new GetAiCapabilityMaturityReport.Validator();
        var result = validator.Validate(
            new GetAiCapabilityMaturityReport.Query(TenantId,
                LookbackDays: 90, PioneerThresholdPct: 20, MinTeamExecutions: 10));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(85.0, "Innovating")]
    [InlineData(65.0, "Scaling")]
    [InlineData(45.0, "Adopting")]
    [InlineData(25.0, "Exploring")]
    [InlineData(10.0, "Initiating")]
    public async Task BD3_MaturityLevel_ClassifiedFromScore(double expectedScore, string expectedLevel)
    {
        // We test the level labels by checking the thresholds via ClassifyMaturityLevel
        var method = typeof(GetAiCapabilityMaturityReport.Handler)
            .GetMethod("ClassifyMaturityLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull();
        var level = (string)method!.Invoke(null, [expectedScore])!;
        level.Should().Be(expectedLevel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static OrganizationalMemoryNode CreateMemoryNode(
        string nodeType, DateTimeOffset recordedAt, double relevance)
    {
        var node = OrganizationalMemoryNode.Create(
            nodeType: nodeType,
            subject: "test-subject",
            title: $"Node {Guid.NewGuid():N}",
            content: "content",
            context: "ctx",
            actorId: "actor-1",
            tags: [],
            sourceType: "manual",
            sourceId: "src-1",
            tenantId: TenantId,
            recordedAt: recordedAt);
        node.UpdateRelevanceScore(relevance);
        return node;
    }

    private static IOrganizationalMemoryRepository SetupMemoryRepo(
        Guid tenantId,
        IReadOnlyList<OrganizationalMemoryNode> nodesForDecision,
        string forType = "decision")
    {
        var repo = Substitute.For<IOrganizationalMemoryRepository>();

        repo.ListByTypeAsync(forType, tenantId, Ct)
            .Returns(Task.FromResult(nodesForDecision));

        // Other types return empty
        repo.ListByTypeAsync(
                Arg.Is<string>(t => t != forType),
                tenantId,
                Ct)
            .Returns(Task.FromResult<IReadOnlyList<OrganizationalMemoryNode>>([]));

        return repo;
    }

    private static AiAgentPerformanceMetric CreateMetric(
        string agentName,
        long totalExecutions = 100,
        double accuracy = 0.7,
        double rating = 4.0,
        long execWithFeedback = 50,
        int rlCycles = 0)
    {
        var metric = AiAgentPerformanceMetric.Create(
            agentId: AiAgentId.New(),
            agentName: agentName,
            periodStart: DateTimeOffset.UtcNow.AddDays(-30),
            periodEnd: DateTimeOffset.UtcNow,
            totalExecutions: totalExecutions,
            executionsWithFeedback: execWithFeedback,
            averageRating: rating,
            accuracyRate: accuracy,
            tenantId: TenantId);

        if (rlCycles > 0)
            metric.UpdateRlCycles(rlCycles);

        return metric;
    }

    private static AiSkill CreateSkill(SkillStatus status)
    {
        return AiSkill.CreateSystem(
            name: $"skill-{Guid.NewGuid():N}",
            displayName: "Test Skill",
            description: "test skill",
            skillContent: "return {};");
    }
}
