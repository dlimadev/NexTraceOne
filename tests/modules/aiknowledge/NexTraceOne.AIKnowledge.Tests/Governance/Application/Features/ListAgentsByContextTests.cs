using System.Collections.Generic;
using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgentsByContext;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

using NSubstitute;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para a feature ListAgentsByContext.
/// Valida o mapeamento de contextos de módulo para categorias de agents
/// e a integração com o repositório.
/// </summary>
public sealed class ListAgentsByContextTests
{
    private readonly IAiAgentRepository _repository = Substitute.For<IAiAgentRepository>();
    private readonly ListAgentsByContext.Handler _handler;

    public ListAgentsByContextTests()
    {
        _handler = new ListAgentsByContext.Handler(_repository);
    }

    // ── REST API context ──────────────────────────────────────────────────

    [Theory]
    [InlineData("rest-api")]
    [InlineData("rest")]
    [InlineData("openapi")]
    public async Task Handle_RestApiContext_ShouldQueryCorrectCategories(string context)
    {
        var capturedCategories = new List<AgentCategory>();
        _repository.ListByCategoriesAsync(
            Arg.Do<IReadOnlyList<AgentCategory>>(cats => capturedCategories.AddRange(cats)),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new ListAgentsByContext.Query(context), default);

        result.IsSuccess.Should().BeTrue();
        capturedCategories.Should().Contain(AgentCategory.ApiDesign);
        capturedCategories.Should().Contain(AgentCategory.TestGeneration);
        capturedCategories.Should().Contain(AgentCategory.ContractGovernance);
        result.Value.ModuleContext.Should().Be(context);
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RestApiContext_ShouldReturnAgentsForApiDesignAndTestGeneration()
    {
        var apiAgent = CreateAgent("api-contract-author", AgentCategory.ApiDesign, isOfficial: true, displayName: "API Contract Author");
        var testAgent = CreateAgent("api-test-scenario", AgentCategory.TestGeneration, isOfficial: true, displayName: "API Test Scenario Generator");

        _repository.ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            true,
            Arg.Any<CancellationToken>())
            .Returns([apiAgent, testAgent]);

        var result = await _handler.Handle(new ListAgentsByContext.Query("rest-api"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.Name).Should().Contain("api-contract-author");
        result.Value.Items.Select(i => i.Name).Should().Contain("api-test-scenario");
        result.Value.TotalCount.Should().Be(2);
    }

    // ── SOAP context ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("soap")]
    [InlineData("wsdl")]
    public async Task Handle_SoapContext_ShouldQueryCorrectCategories(string context)
    {
        var capturedCategories = new List<AgentCategory>();
        _repository.ListByCategoriesAsync(
            Arg.Do<IReadOnlyList<AgentCategory>>(cats => capturedCategories.AddRange(cats)),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new ListAgentsByContext.Query(context), default);

        result.IsSuccess.Should().BeTrue();
        capturedCategories.Should().Contain(AgentCategory.SoapDesign);
        capturedCategories.Should().Contain(AgentCategory.TestGeneration);
        result.Value.ModuleContext.Should().Be(context);
    }

    [Fact]
    public async Task Handle_SoapContext_ShouldReturnSoapDesignAgents()
    {
        var soapAgent = CreateAgent("soap-contract-author", AgentCategory.SoapDesign, isOfficial: true, displayName: "SOAP Contract Author");

        _repository.ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            true,
            Arg.Any<CancellationToken>())
            .Returns([soapAgent]);

        var result = await _handler.Handle(new ListAgentsByContext.Query("soap"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Category.Should().Be("SoapDesign");
        result.Value.Items[0].IsOfficial.Should().BeTrue();
    }

    // ── Kafka / event context ─────────────────────────────────────────────

    [Theory]
    [InlineData("kafka")]
    [InlineData("event")]
    [InlineData("asyncapi")]
    [InlineData("event-contract")]
    public async Task Handle_KafkaContext_ShouldQueryCorrectCategories(string context)
    {
        var capturedCategories = new List<AgentCategory>();
        _repository.ListByCategoriesAsync(
            Arg.Do<IReadOnlyList<AgentCategory>>(cats => capturedCategories.AddRange(cats)),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new ListAgentsByContext.Query(context), default);

        result.IsSuccess.Should().BeTrue();
        capturedCategories.Should().Contain(AgentCategory.EventDesign);
        capturedCategories.Should().Contain(AgentCategory.TestGeneration);
        capturedCategories.Should().Contain(AgentCategory.ContractGovernance);
    }

    [Fact]
    public async Task Handle_KafkaContext_ShouldReturnEventDesignAgents()
    {
        var kafkaAgent = CreateAgent("kafka-schema-contract", AgentCategory.EventDesign, isOfficial: true, displayName: "Kafka Schema Contract Designer");

        _repository.ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            true,
            Arg.Any<CancellationToken>())
            .Returns([kafkaAgent]);

        var result = await _handler.Handle(new ListAgentsByContext.Query("kafka"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Category.Should().Be("EventDesign");
    }

    // ── Testing context ───────────────────────────────────────────────────

    [Theory]
    [InlineData("testing")]
    [InlineData("test")]
    public async Task Handle_TestingContext_ShouldQueryCorrectCategories(string context)
    {
        var capturedCategories = new List<AgentCategory>();
        _repository.ListByCategoriesAsync(
            Arg.Do<IReadOnlyList<AgentCategory>>(cats => capturedCategories.AddRange(cats)),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new ListAgentsByContext.Query(context), default);

        result.IsSuccess.Should().BeTrue();
        capturedCategories.Should().Contain(AgentCategory.TestGeneration);
        result.Value.ModuleContext.Should().Be(context);
    }

    // ── Unknown context ───────────────────────────────────────────────────

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("xyz")]
    public async Task Handle_UnknownContext_ShouldReturnEmptyWithoutCallingRepository(string context)
    {
        var result = await _handler.Handle(new ListAgentsByContext.Query(context), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        await _repository.DidNotReceive().ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>());
    }

    // ── Case insensitive ─────────────────────────────────────────────────

    [Theory]
    [InlineData("REST-API")]
    [InlineData("Rest-Api")]
    [InlineData("SOAP")]
    [InlineData("KAFKA")]
    public async Task Handle_ContextIsCaseInsensitive(string context)
    {
        _repository.ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new ListAgentsByContext.Query(context), default);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>());
    }

    // ── Agent mapping ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldMapAgentFieldsCorrectly()
    {
        var agent = AiAgent.Register(
            "api-contract-author",
            "API Contract Author",
            "Generates OpenAPI 3.1 YAML specifications",
            AgentCategory.ApiDesign,
            isOfficial: true,
            "You are the API Contract Author agent.",
            icon: "📐",
            sortOrder: 70);

        _repository.ListByCategoriesAsync(
            Arg.Any<IReadOnlyList<AgentCategory>>(),
            Arg.Any<bool?>(),
            Arg.Any<CancellationToken>())
            .Returns([agent]);

        var result = await _handler.Handle(new ListAgentsByContext.Query("rest-api"), default);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.Name.Should().Be("api-contract-author");
        item.DisplayName.Should().Be("API Contract Author");
        item.Category.Should().Be("ApiDesign");
        item.IsOfficial.Should().BeTrue();
        item.IsActive.Should().BeTrue();
        item.Icon.Should().Be("📐");
        item.OwnershipType.Should().Be("System");
        item.PublicationStatus.Should().Be("Published");
        item.Version.Should().Be(1);
        item.ExecutionCount.Should().Be(0);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static AiAgent CreateAgent(
        string name,
        AgentCategory category,
        bool isOfficial = false,
        string icon = "🤖",
        string? displayName = null)
    {
        var resolvedDisplayName = displayName ?? name.Replace("-", " ");
        return AiAgent.Register(
            name,
            resolvedDisplayName,
            $"Test agent for {name}",
            category,
            isOfficial,
            $"System prompt for {name}",
            icon: icon,
            sortOrder: 10);
    }
}
