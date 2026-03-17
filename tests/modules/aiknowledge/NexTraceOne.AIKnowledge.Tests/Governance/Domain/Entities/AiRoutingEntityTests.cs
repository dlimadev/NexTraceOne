using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

/// <summary>Testes unitários das entidades de AI Routing &amp; Enrichment.</summary>
public sealed class AiRoutingEntityTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── AIRoutingStrategy ────────────────────────────────────────────────────

    [Fact]
    public void Strategy_Create_ShouldSetProperties()
    {
        var strategy = AIRoutingStrategy.Create(
            "default-routing", "Default routing strategy",
            "Engineer", "ChangeAnalysis", "Web",
            AIRoutingPath.InternalPreferred,
            maxSensitivityLevel: 3,
            allowExternalEscalation: true,
            priority: 10,
            FixedNow);

        strategy.Name.Should().Be("default-routing");
        strategy.Description.Should().Be("Default routing strategy");
        strategy.TargetPersona.Should().Be("Engineer");
        strategy.TargetUseCase.Should().Be("ChangeAnalysis");
        strategy.TargetClientType.Should().Be("Web");
        strategy.PreferredPath.Should().Be(AIRoutingPath.InternalPreferred);
        strategy.MaxSensitivityLevel.Should().Be(3);
        strategy.AllowExternalEscalation.Should().BeTrue();
        strategy.IsActive.Should().BeTrue();
        strategy.Priority.Should().Be(10);
        strategy.RegisteredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Strategy_Activate_ShouldSetActiveTrue()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "Engineer", "General", "Api",
            AIRoutingPath.InternalOnly, 1, false, 0, FixedNow);
        strategy.Deactivate();

        strategy.Activate();

        strategy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Strategy_Deactivate_ShouldSetActiveFalse()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "Engineer", "General", "Api",
            AIRoutingPath.InternalOnly, 1, false, 0, FixedNow);

        strategy.Deactivate();

        strategy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Strategy_Update_ShouldModifyProperties()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "Old desc", "*", "*", "*",
            AIRoutingPath.InternalOnly, 2, false, 5, FixedNow);

        var result = strategy.Update(
            "New description",
            AIRoutingPath.ExternalEscalation,
            maxSensitivityLevel: 4,
            allowExternalEscalation: true,
            priority: 1);

        result.IsSuccess.Should().BeTrue();
        strategy.Description.Should().Be("New description");
        strategy.PreferredPath.Should().Be(AIRoutingPath.ExternalEscalation);
        strategy.MaxSensitivityLevel.Should().Be(4);
        strategy.AllowExternalEscalation.Should().BeTrue();
        strategy.Priority.Should().Be(1);
    }

    [Fact]
    public void Strategy_IsApplicable_ShouldReturnTrue_WhenValuesMatch()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "TechLead", "ChangeAnalysis", "VsCode",
            AIRoutingPath.InternalPreferred, 3, true, 0, FixedNow);

        strategy.IsApplicable("TechLead", "ChangeAnalysis", "VsCode")
            .Should().BeTrue();
    }

    [Fact]
    public void Strategy_IsApplicable_ShouldReturnTrue_ForWildcard()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "*", "*", "*",
            AIRoutingPath.InternalOnly, 1, false, 0, FixedNow);

        strategy.IsApplicable("Architect", "ContractGeneration", "Api")
            .Should().BeTrue();
    }

    [Fact]
    public void Strategy_IsApplicable_ShouldReturnFalse_WhenPersonaDoesNotMatch()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "Engineer", "General", "Web",
            AIRoutingPath.InternalOnly, 1, false, 0, FixedNow);

        strategy.IsApplicable("Executive", "General", "Web")
            .Should().BeFalse();
    }

    [Fact]
    public void Strategy_IsApplicable_ShouldReturnFalse_WhenUseCaseDoesNotMatch()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "Engineer", "ChangeAnalysis", "Web",
            AIRoutingPath.InternalOnly, 1, false, 0, FixedNow);

        strategy.IsApplicable("Engineer", "IncidentExplanation", "Web")
            .Should().BeFalse();
    }

    [Fact]
    public void Strategy_IsApplicable_ShouldReturnFalse_WhenClientTypeDoesNotMatch()
    {
        var strategy = AIRoutingStrategy.Create(
            "s", "desc", "Engineer", "General", "Web",
            AIRoutingPath.InternalOnly, 1, false, 0, FixedNow);

        strategy.IsApplicable("Engineer", "General", "Api")
            .Should().BeFalse();
    }

    // ── AIRoutingDecision ────────────────────────────────────────────────────

    [Fact]
    public void Decision_Record_ShouldSetProperties()
    {
        var strategyId = Guid.NewGuid();

        var decision = AIRoutingDecision.Record(
            "corr-101",
            "Engineer",
            AIUseCaseType.ChangeAnalysis,
            "Web",
            AIRoutingPath.InternalPreferred,
            "gpt-4o", "OpenAI",
            isInternalModel: false,
            strategyId,
            "Default Policy",
            AIEscalationReason.InsufficientInternalCapability,
            "Model escalated due to complexity",
            "medium",
            AIConfidenceLevel.High,
            "ServiceCatalog,Contracts",
            "Service:80,Contracts:60",
            FixedNow);

        decision.CorrelationId.Should().Be("corr-101");
        decision.Persona.Should().Be("Engineer");
        decision.UseCaseType.Should().Be(AIUseCaseType.ChangeAnalysis);
        decision.ClientType.Should().Be("Web");
        decision.SelectedPath.Should().Be(AIRoutingPath.InternalPreferred);
        decision.SelectedModelName.Should().Be("gpt-4o");
        decision.SelectedProvider.Should().Be("OpenAI");
        decision.IsInternalModel.Should().BeFalse();
        decision.AppliedStrategyId.Should().Be(strategyId);
        decision.AppliedPolicyName.Should().Be("Default Policy");
        decision.EscalationReason.Should().Be(AIEscalationReason.InsufficientInternalCapability);
        decision.Rationale.Should().Be("Model escalated due to complexity");
        decision.EstimatedCostClass.Should().Be("medium");
        decision.ConfidenceLevel.Should().Be(AIConfidenceLevel.High);
        decision.SelectedSources.Should().Be("ServiceCatalog,Contracts");
        decision.SourceWeightingSummary.Should().Be("Service:80,Contracts:60");
        decision.DecidedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Decision_Record_WithNoEscalation_ShouldSetNone()
    {
        var decision = AIRoutingDecision.Record(
            "corr-102",
            "TechLead",
            AIUseCaseType.ServiceLookup,
            "VsCode",
            AIRoutingPath.InternalOnly,
            "local-llm", "Internal",
            isInternalModel: true,
            appliedStrategyId: null,
            appliedPolicyName: null,
            AIEscalationReason.None,
            "Internal model sufficient",
            "low",
            AIConfidenceLevel.Medium,
            "ServiceCatalog",
            "Service:100",
            FixedNow);

        decision.EscalationReason.Should().Be(AIEscalationReason.None);
        decision.IsInternalModel.Should().BeTrue();
        decision.AppliedStrategyId.Should().BeNull();
        decision.AppliedPolicyName.Should().BeNull();
    }

    // ── AIKnowledgeSourceWeight ──────────────────────────────────────────────

    [Fact]
    public void SourceWeight_Configure_ShouldSetProperties()
    {
        var weight = AIKnowledgeSourceWeight.Configure(
            KnowledgeSourceType.Service,
            AIUseCaseType.ChangeAnalysis,
            AISourceRelevance.Primary,
            weight: 85,
            trustLevel: 4,
            FixedNow);

        weight.SourceType.Should().Be(KnowledgeSourceType.Service);
        weight.UseCaseType.Should().Be(AIUseCaseType.ChangeAnalysis);
        weight.Relevance.Should().Be(AISourceRelevance.Primary);
        weight.Weight.Should().Be(85);
        weight.TrustLevel.Should().Be(4);
        weight.IsActive.Should().BeTrue();
        weight.ConfiguredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void SourceWeight_UpdateWeight_ShouldModifyProperties()
    {
        var weight = AIKnowledgeSourceWeight.Configure(
            KnowledgeSourceType.Contract,
            AIUseCaseType.ContractExplanation,
            AISourceRelevance.Primary,
            90, 5, FixedNow);

        var result = weight.UpdateWeight(AISourceRelevance.Secondary, 60, 3);

        result.IsSuccess.Should().BeTrue();
        weight.Relevance.Should().Be(AISourceRelevance.Secondary);
        weight.Weight.Should().Be(60);
        weight.TrustLevel.Should().Be(3);
    }

    [Fact]
    public void SourceWeight_Activate_ShouldSetActiveTrue()
    {
        var weight = AIKnowledgeSourceWeight.Configure(
            KnowledgeSourceType.Incident,
            AIUseCaseType.IncidentExplanation,
            AISourceRelevance.Primary,
            70, 3, FixedNow);
        weight.Deactivate();

        weight.Activate();

        weight.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SourceWeight_Deactivate_ShouldSetActiveFalse()
    {
        var weight = AIKnowledgeSourceWeight.Configure(
            KnowledgeSourceType.Runbook,
            AIUseCaseType.MitigationGuidance,
            AISourceRelevance.Secondary,
            50, 2, FixedNow);

        weight.Deactivate();

        weight.IsActive.Should().BeFalse();
    }

    // ── AIExecutionPlan ──────────────────────────────────────────────────────

    [Fact]
    public void ExecutionPlan_Create_ShouldSetProperties()
    {
        var plan = AIExecutionPlan.Create(
            "corr-201",
            "What changed in the payment service?",
            "Engineer",
            AIUseCaseType.ChangeAnalysis,
            "gpt-4o", "OpenAI",
            isInternal: false,
            AIRoutingPath.InternalPreferred,
            "ServiceCatalog,Changes",
            "Service:80,Changes:90",
            "Allowed by default policy",
            "medium",
            "Internal preferred with external fallback",
            AIConfidenceLevel.High,
            AIEscalationReason.None,
            FixedNow);

        plan.CorrelationId.Should().Be("corr-201");
        plan.InputQuery.Should().Be("What changed in the payment service?");
        plan.Persona.Should().Be("Engineer");
        plan.UseCaseType.Should().Be(AIUseCaseType.ChangeAnalysis);
        plan.SelectedModel.Should().Be("gpt-4o");
        plan.SelectedProvider.Should().Be("OpenAI");
        plan.IsInternal.Should().BeFalse();
        plan.RoutingPath.Should().Be(AIRoutingPath.InternalPreferred);
        plan.SelectedSources.Should().Be("ServiceCatalog,Changes");
        plan.SourceWeightingSummary.Should().Be("Service:80,Changes:90");
        plan.PolicyDecision.Should().Be("Allowed by default policy");
        plan.EstimatedCostClass.Should().Be("medium");
        plan.RationaleSummary.Should().Be("Internal preferred with external fallback");
        plan.ConfidenceLevel.Should().Be(AIConfidenceLevel.High);
        plan.EscalationReason.Should().Be(AIEscalationReason.None);
        plan.PlannedAt.Should().Be(FixedNow);
    }

    // ── AIEnrichmentResult ───────────────────────────────────────────────────

    [Fact]
    public void EnrichmentResult_Record_ShouldSetProperties()
    {
        var result = AIEnrichmentResult.Record(
            "corr-301",
            "Explain the payment-api contract",
            "TechLead",
            AIUseCaseType.ContractExplanation,
            "ServiceCatalog,Contracts,Documentation",
            "ServiceCatalog,Contracts",
            totalContextItems: 12,
            AIConfidenceLevel.Medium,
            "Found 8 contract fields and 4 service attributes",
            processingTimeMs: 245,
            FixedNow);

        result.CorrelationId.Should().Be("corr-301");
        result.InputQuery.Should().Be("Explain the payment-api contract");
        result.Persona.Should().Be("TechLead");
        result.UseCaseType.Should().Be(AIUseCaseType.ContractExplanation);
        result.QueriedSources.Should().Be("ServiceCatalog,Contracts,Documentation");
        result.ResolvedSources.Should().Be("ServiceCatalog,Contracts");
        result.TotalContextItems.Should().Be(12);
        result.ConfidenceLevel.Should().Be(AIConfidenceLevel.Medium);
        result.ContextSummary.Should().Be("Found 8 contract fields and 4 service attributes");
        result.ProcessingTimeMs.Should().Be(245);
        result.EnrichedAt.Should().Be(FixedNow);
    }
}
