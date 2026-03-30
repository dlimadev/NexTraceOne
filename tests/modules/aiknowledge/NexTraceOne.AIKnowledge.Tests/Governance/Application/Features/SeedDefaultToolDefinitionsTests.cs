using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultToolDefinitions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class SeedDefaultToolDefinitionsTests
{
    private readonly IAiToolDefinitionRepository _repository = Substitute.For<IAiToolDefinitionRepository>();

    [Fact]
    public async Task Handle_Empty_Registry_Seeds_All_Tools()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiToolDefinition>());

        var handler = new SeedDefaultToolDefinitions.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultToolDefinitions.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolsSeeded.Should().Be(DefaultToolDefinitionCatalog.GetAll().Count);
        result.Value.TotalInCatalog.Should().Be(DefaultToolDefinitionCatalog.GetAll().Count);

        await _repository.Received(DefaultToolDefinitionCatalog.GetAll().Count)
            .AddAsync(Arg.Any<AiToolDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_All_Exist_Seeds_Nothing()
    {
        var existing = DefaultToolDefinitionCatalog.GetAll()
            .Select(d => AiToolDefinition.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, parametersSchema: d.ParametersSchema,
                version: 1, isActive: true, requiresApproval: d.RequiresApproval,
                riskLevel: d.RiskLevel, isOfficial: true, timeoutMs: d.TimeoutMs))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultToolDefinitions.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultToolDefinitions.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolsSeeded.Should().Be(0);

        await _repository.DidNotReceive()
            .AddAsync(Arg.Any<AiToolDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Partial_Seeds_Only_Missing()
    {
        var catalog = DefaultToolDefinitionCatalog.GetAll();
        var firstTwo = catalog.Take(2)
            .Select(d => AiToolDefinition.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, parametersSchema: d.ParametersSchema,
                version: 1, isActive: true, requiresApproval: d.RequiresApproval,
                riskLevel: d.RiskLevel, isOfficial: true, timeoutMs: d.TimeoutMs))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(firstTwo.AsReadOnly());

        var handler = new SeedDefaultToolDefinitions.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultToolDefinitions.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolsSeeded.Should().Be(catalog.Count - 2);
    }

    [Fact]
    public async Task Handle_Case_Insensitive_Does_Not_Duplicate()
    {
        var first = DefaultToolDefinitionCatalog.GetAll().First();
        var existing = new List<AiToolDefinition>
        {
            AiToolDefinition.Create(
                name: first.Name.ToUpperInvariant(),
                displayName: first.DisplayName, description: first.Description,
                category: first.Category, parametersSchema: first.ParametersSchema,
                version: 1, isActive: true, requiresApproval: first.RequiresApproval,
                riskLevel: first.RiskLevel, isOfficial: true, timeoutMs: first.TimeoutMs)
        };

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultToolDefinitions.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultToolDefinitions.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ToolsSeeded.Should().Be(DefaultToolDefinitionCatalog.GetAll().Count - 1);
    }

    [Fact]
    public async Task Handle_Second_Run_Seeds_Nothing()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiToolDefinition>());

        var handler = new SeedDefaultToolDefinitions.Handler(_repository);
        var first = await handler.Handle(new SeedDefaultToolDefinitions.Command(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.ToolsSeeded.Should().BeGreaterThan(0);

        var existing = DefaultToolDefinitionCatalog.GetAll()
            .Select(d => AiToolDefinition.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, parametersSchema: d.ParametersSchema,
                version: 1, isActive: true, requiresApproval: d.RequiresApproval,
                riskLevel: d.RiskLevel, isOfficial: true, timeoutMs: d.TimeoutMs))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var second = await handler.Handle(new SeedDefaultToolDefinitions.Command(), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.ToolsSeeded.Should().Be(0);
    }
}
