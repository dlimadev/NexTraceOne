using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAgentMarketplace;
using NexTraceOne.AIKnowledge.Application.Orchestration.Features.SuggestSemanticVersionWithAI;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Features;

/// <summary>
/// Testes unitários para SuggestSemanticVersionWithAI e GetAgentMarketplace.
/// Completa a cobertura P14 das 18 features de Orchestration.
/// </summary>
public sealed class MissingOrchestrationFeaturesTests
{
    // ── SuggestSemanticVersionWithAI ──────────────────────────────────────

    [Fact]
    public async Task SuggestSemanticVersion_ShouldReturnParsedVersion_WhenProviderResponds()
    {
        var port = Substitute.For<IExternalAIRoutingPort>();
        port.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("For this minor change with backward-compatible additions, I suggest version 2.3.0. " +
                     "The change adds new optional fields which does not break existing consumers.");

        var dateProvider = Substitute.For<IDateTimeProvider>();
        dateProvider.UtcNow.Returns(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

        var handler = new SuggestSemanticVersionWithAI.Handler(port, dateProvider, NullLogger<SuggestSemanticVersionWithAI.Handler>.Instance);

        var result = await handler.Handle(
            new SuggestSemanticVersionWithAI.Command(
                ContractName: "payment-api",
                CurrentVersion: "2.2.0",
                ChangeDescription: "Added optional discount field to PaymentRequest",
                ChangeType: "minor",
                PreferredProvider: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuggestedVersion.Should().Be("2.3.0");
        result.Value.Rationale.Should().Contain("2.3.0");
        result.Value.IsFallback.Should().BeFalse();
        result.Value.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SuggestSemanticVersion_ShouldReturnFallback_WhenProviderUnavailable()
    {
        var port = Substitute.For<IExternalAIRoutingPort>();
        port.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Provider unreachable")));

        var dateProvider = Substitute.For<IDateTimeProvider>();
        var handler = new SuggestSemanticVersionWithAI.Handler(port, dateProvider, NullLogger<SuggestSemanticVersionWithAI.Handler>.Instance);

        var result = await handler.Handle(
            new SuggestSemanticVersionWithAI.Command("orders-api", "1.0.0", null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("AIKnowledge.Provider.Unavailable");
    }

    [Fact]
    public async Task SuggestSemanticVersion_ShouldFallbackToCurrentVersion_WhenNoVersionInResponse()
    {
        var port = Substitute.For<IExternalAIRoutingPort>();
        port.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Based on the context, you should increment the version appropriately for this change.");

        var dateProvider = Substitute.For<IDateTimeProvider>();
        var handler = new SuggestSemanticVersionWithAI.Handler(port, dateProvider, NullLogger<SuggestSemanticVersionWithAI.Handler>.Instance);

        var result = await handler.Handle(
            new SuggestSemanticVersionWithAI.Command("catalog-api", "3.1.5", null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // When no version found in AI response, falls back to current version or default
        result.Value!.SuggestedVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SuggestSemanticVersion_Validator_ShouldFail_WhenContractNameEmpty()
    {
        var validator = new SuggestSemanticVersionWithAI.Validator();

        var result = validator.Validate(
            new SuggestSemanticVersionWithAI.Command(
                ContractName: string.Empty,
                CurrentVersion: "1.0.0",
                ChangeDescription: null,
                ChangeType: null,
                PreferredProvider: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SuggestSemanticVersionWithAI.Command.ContractName));
    }

    [Fact]
    public async Task SuggestSemanticVersion_ShouldDetectMajorVersionInResponse()
    {
        var port = Substitute.For<IExternalAIRoutingPort>();
        port.RouteQueryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("This change breaks backward compatibility. I recommend bumping to 3.0.0 as it removes endpoints.");

        var dateProvider = Substitute.For<IDateTimeProvider>();
        var handler = new SuggestSemanticVersionWithAI.Handler(port, dateProvider, NullLogger<SuggestSemanticVersionWithAI.Handler>.Instance);

        var result = await handler.Handle(
            new SuggestSemanticVersionWithAI.Command("user-api", "2.9.1", "Removed deprecated /users/old endpoint", "breaking", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SuggestedVersion.Should().Be("3.0.0");
    }

    // ── GetAgentMarketplace ───────────────────────────────────────────────

    [Fact]
    public async Task GetAgentMarketplace_ShouldReturnAllActiveAgents()
    {
        var repo = Substitute.For<IAiAgentRepository>();
        repo.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent>
            {
                AiAgent.Register("change-analyst", "Change Analyst", "Analyzes change impact",
                    AgentCategory.ChangeIntelligence, isOfficial: true, systemPrompt: "Analyze changes"),
                AiAgent.Register("api-designer", "API Designer", "Helps design REST APIs",
                    AgentCategory.ApiDesign, isOfficial: false, systemPrompt: "Design APIs"),
                AiAgent.Register("incident-responder", "Incident Responder", "Helps investigate incidents",
                    AgentCategory.OperationalIntelligence, isOfficial: true, systemPrompt: "Investigate incidents"),
            } as IReadOnlyList<AiAgent>);

        var handler = new GetAgentMarketplace.Handler(repo);

        var result = await handler.Handle(
            new GetAgentMarketplace.Query(null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAgentMarketplace_ShouldFilterByCategory()
    {
        var repo = Substitute.For<IAiAgentRepository>();
        repo.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent>
            {
                AiAgent.Register("change-analyst", "Change Analyst", "Analyzes change impact",
                    AgentCategory.ChangeIntelligence, isOfficial: true, systemPrompt: "Analyze"),
                AiAgent.Register("api-designer", "API Designer", "Helps design REST APIs",
                    AgentCategory.ApiDesign, isOfficial: false, systemPrompt: "Design"),
            } as IReadOnlyList<AiAgent>);

        var handler = new GetAgentMarketplace.Handler(repo);

        var result = await handler.Handle(
            new GetAgentMarketplace.Query(Category: "ChangeIntelligence", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("change-analyst");
    }

    [Fact]
    public async Task GetAgentMarketplace_ShouldFilterByOfficialFlag()
    {
        var repo = Substitute.For<IAiAgentRepository>();
        repo.ListAsync(true, true, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent>
            {
                AiAgent.Register("change-analyst", "Change Analyst", "Analyzes change impact",
                    AgentCategory.ChangeIntelligence, isOfficial: true, systemPrompt: "Analyze"),
            } as IReadOnlyList<AiAgent>);

        var handler = new GetAgentMarketplace.Handler(repo);

        var result = await handler.Handle(
            new GetAgentMarketplace.Query(null, null, IsOfficial: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().AllSatisfy(a => a.IsOfficial.Should().BeTrue());
    }

    [Fact]
    public async Task GetAgentMarketplace_ShouldFilterBySearchTerm()
    {
        var repo = Substitute.For<IAiAgentRepository>();
        repo.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgent>
            {
                AiAgent.Register("change-analyst", "Change Analyst", "Analyzes change impact",
                    AgentCategory.ChangeIntelligence, isOfficial: true, systemPrompt: "Analyze"),
                AiAgent.Register("api-designer", "API Designer", "Helps design REST APIs",
                    AgentCategory.ApiDesign, isOfficial: false, systemPrompt: "Design"),
            } as IReadOnlyList<AiAgent>);

        var handler = new GetAgentMarketplace.Handler(repo);

        var result = await handler.Handle(
            new GetAgentMarketplace.Query(null, Search: "api", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("api-designer");
    }

    [Fact]
    public async Task GetAgentMarketplace_ShouldReturnEmptyMarketplace_WhenNoAgents()
    {
        var repo = Substitute.For<IAiAgentRepository>();
        repo.ListAsync(true, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiAgent>() as IReadOnlyList<AiAgent>);

        var handler = new GetAgentMarketplace.Handler(repo);

        var result = await handler.Handle(
            new GetAgentMarketplace.Query(null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Categories.Should().BeEmpty();
    }

    [Fact]
    public void GetAgentMarketplace_Validator_ShouldFail_WhenPageIsZero()
    {
        var validator = new GetAgentMarketplace.Validator();

        var result = validator.Validate(new GetAgentMarketplace.Query(null, null, null, Page: 0, PageSize: 20));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetAgentMarketplace.Query.Page));
    }

    [Fact]
    public void GetAgentMarketplace_Validator_ShouldFail_WhenPageSizeExceedsMax()
    {
        var validator = new GetAgentMarketplace.Validator();

        var result = validator.Validate(new GetAgentMarketplace.Query(null, null, null, Page: 1, PageSize: 100));

        result.IsValid.Should().BeFalse();
    }
}
