using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Services;

/// <summary>
/// Testes unitários para ContextGroundingService.
/// Valida classificação de use case, resolução de fontes, avaliação de confidence
/// e construção do system prompt com base em persona, contexto e grounding context.
/// </summary>
public sealed class ContextGroundingServiceTests
{
    // ── ClassifyUseCase ───────────────────────────────────────────────────

    [Theory]
    [InlineData("generate a REST contract", null, AIUseCaseType.ContractGeneration)]
    [InlineData("explain this contract", "contracts", AIUseCaseType.ContractExplanation)]
    [InlineData("what happened in the incident", "incidents", AIUseCaseType.IncidentExplanation)]
    [InlineData("how do I mitigate this runbook?", null, AIUseCaseType.MitigationGuidance)]
    [InlineData("analyze the change blast radius", null, AIUseCaseType.ChangeAnalysis)]
    [InlineData("executive summary of operations", null, AIUseCaseType.ExecutiveSummary)]
    [InlineData("what is the compliance risk?", null, AIUseCaseType.RiskComplianceExplanation)]
    [InlineData("show cost and finops waste", null, AIUseCaseType.FinOpsExplanation)]
    [InlineData("what does this service depend on?", null, AIUseCaseType.DependencyReasoning)]
    [InlineData("list services in this team", "services", AIUseCaseType.ServiceLookup)]
    [InlineData("random general question", null, AIUseCaseType.General)]
    public void ClassifyUseCase_ShouldReturnCorrectUseCaseType(string query, string? scope, AIUseCaseType expected)
    {
        var result = ContextGroundingService.ClassifyUseCase(query, scope);
        result.Should().Be(expected);
    }

    // ── ResolveGroundingSources ───────────────────────────────────────────

    [Fact]
    public void ResolveGroundingSources_WhenNoSourcesAndNoScope_ReturnsDefaults()
    {
        var sources = ContextGroundingService.ResolveGroundingSources(
            null, Array.Empty<AIKnowledgeSource>(), AIUseCaseType.General);

        sources.Should().Contain("Service Catalog");
        sources.Should().Contain("Contract Registry");
    }

    [Fact]
    public void ResolveGroundingSources_WhenScopeProvided_MapsToSourceNames()
    {
        var sources = ContextGroundingService.ResolveGroundingSources(
            "services,contracts,incidents", Array.Empty<AIKnowledgeSource>(), AIUseCaseType.General);

        sources.Should().Contain("Service Catalog");
        sources.Should().Contain("Contract Registry");
        sources.Should().Contain("Incident History");
    }

    [Fact]
    public void ResolveGroundingSources_WhenAvailableSourcesPresent_PrioritizesByUseCase()
    {
        var sources = new[]
        {
            CreateKnowledgeSource("Contract Registry", KnowledgeSourceType.Contract),
            CreateKnowledgeSource("Service Catalog", KnowledgeSourceType.Service),
        };

        var result = ContextGroundingService.ResolveGroundingSources(
            null, sources, AIUseCaseType.ContractExplanation);

        result[0].Should().Be("Contract Registry");
    }

    // ── EvaluateSourceWeights ─────────────────────────────────────────────

    [Fact]
    public void EvaluateSourceWeights_WhenNoSourcesMatch_ReturnsUnknownConfidence()
    {
        var (summary, confidence) = ContextGroundingService.EvaluateSourceWeights(
            Array.Empty<AIKnowledgeSource>(), AIUseCaseType.IncidentExplanation);

        confidence.Should().Be(AIConfidenceLevel.Unknown.ToString());
        summary.Should().Be("no-matches");
    }

    [Fact]
    public void EvaluateSourceWeights_WhenThreeSourcesMatch_ReturnsHighConfidence()
    {
        var sources = new[]
        {
            CreateKnowledgeSource("Incident History", KnowledgeSourceType.Incident),
            CreateKnowledgeSource("Change Intelligence", KnowledgeSourceType.Change),
            CreateKnowledgeSource("Runbook Library", KnowledgeSourceType.Runbook),
        };

        var (_, confidence) = ContextGroundingService.EvaluateSourceWeights(
            sources, AIUseCaseType.IncidentExplanation);

        confidence.Should().Be(AIConfidenceLevel.High.ToString());
    }

    [Fact]
    public void EvaluateSourceWeights_WhenTwoSourcesMatch_ReturnsMediumConfidence()
    {
        var sources = new[]
        {
            CreateKnowledgeSource("Incident History", KnowledgeSourceType.Incident),
            CreateKnowledgeSource("Change Intelligence", KnowledgeSourceType.Change),
        };

        var (_, confidence) = ContextGroundingService.EvaluateSourceWeights(
            sources, AIUseCaseType.IncidentExplanation);

        confidence.Should().Be(AIConfidenceLevel.Medium.ToString());
    }

    // ── BuildAssistantSystemPrompt ────────────────────────────────────────

    [Fact]
    public void BuildAssistantSystemPrompt_ContainsPersonaInPrompt()
    {
        var prompt = ContextGroundingService.BuildAssistantSystemPrompt(
            "Auditor", "compliance", "some context");

        prompt.Should().Contain("Auditor");
        prompt.Should().Contain("compliance");
    }

    [Fact]
    public void BuildAssistantSystemPrompt_WhenContextScopeNull_UsesGeneral()
    {
        var prompt = ContextGroundingService.BuildAssistantSystemPrompt(
            "Engineer", null, "grounding");

        prompt.Should().Contain("general");
    }

    [Fact]
    public void BuildAssistantSystemPrompt_WhenNoGroundingContext_StatesNotAvailable()
    {
        var prompt = ContextGroundingService.BuildAssistantSystemPrompt(
            "Engineer", "services", string.Empty);

        prompt.Should().Contain("No grounding context available");
    }

    // ── GetSourcePrioritiesByUseCase ──────────────────────────────────────

    [Theory]
    [InlineData(AIUseCaseType.IncidentExplanation, KnowledgeSourceType.Incident)]
    [InlineData(AIUseCaseType.ContractExplanation, KnowledgeSourceType.Contract)]
    [InlineData(AIUseCaseType.ChangeAnalysis, KnowledgeSourceType.Change)]
    [InlineData(AIUseCaseType.MitigationGuidance, KnowledgeSourceType.Runbook)]
    public void GetSourcePrioritiesByUseCase_FirstPriorityMatchesUseCase(
        AIUseCaseType useCaseType, KnowledgeSourceType expectedFirst)
    {
        var priorities = ContextGroundingService.GetSourcePrioritiesByUseCase(useCaseType);

        priorities.Should().NotBeEmpty();
        priorities[0].Should().Be(expectedFirst);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AIKnowledgeSource CreateKnowledgeSource(string name, KnowledgeSourceType sourceType)
        => AIKnowledgeSource.Register(name, $"Description for {name}", sourceType, "test-endpoint", 1, DateTimeOffset.UtcNow);
}
