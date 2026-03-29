using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultGuardrails;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class SeedDefaultGuardrailsTests
{
    private readonly IAiGuardrailRepository _repository = Substitute.For<IAiGuardrailRepository>();

    [Fact]
    public async Task Handle_Empty_Registry_Seeds_All_Guardrails()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiGuardrail>());

        var handler = new SeedDefaultGuardrails.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultGuardrails.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GuardrailsSeeded.Should().Be(DefaultGuardrailCatalog.GetAll().Count);
        result.Value.TotalInCatalog.Should().Be(DefaultGuardrailCatalog.GetAll().Count);

        await _repository.Received(DefaultGuardrailCatalog.GetAll().Count)
            .AddAsync(Arg.Any<AiGuardrail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_All_Exist_Seeds_Nothing()
    {
        var existing = DefaultGuardrailCatalog.GetAll()
            .Select(d => AiGuardrail.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, guardType: d.GuardType, pattern: d.Pattern,
                patternType: d.PatternType, severity: d.Severity, action: d.Action,
                userMessage: d.UserMessage, isActive: true, isOfficial: true,
                agentId: null, modelId: null, priority: d.Priority))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultGuardrails.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultGuardrails.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GuardrailsSeeded.Should().Be(0);

        await _repository.DidNotReceive()
            .AddAsync(Arg.Any<AiGuardrail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Partial_Seeds_Only_Missing()
    {
        var catalog = DefaultGuardrailCatalog.GetAll();
        var firstThree = catalog.Take(3)
            .Select(d => AiGuardrail.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, guardType: d.GuardType, pattern: d.Pattern,
                patternType: d.PatternType, severity: d.Severity, action: d.Action,
                userMessage: d.UserMessage, isActive: true, isOfficial: true,
                agentId: null, modelId: null, priority: d.Priority))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(firstThree.AsReadOnly());

        var handler = new SeedDefaultGuardrails.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultGuardrails.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GuardrailsSeeded.Should().Be(catalog.Count - 3);
    }

    [Fact]
    public async Task Handle_Case_Insensitive_Does_Not_Duplicate()
    {
        var first = DefaultGuardrailCatalog.GetAll().First();
        var existing = new List<AiGuardrail>
        {
            AiGuardrail.Create(
                name: first.Name.ToUpperInvariant(),
                displayName: first.DisplayName, description: first.Description,
                category: first.Category, guardType: first.GuardType, pattern: first.Pattern,
                patternType: first.PatternType, severity: first.Severity, action: first.Action,
                userMessage: first.UserMessage, isActive: true, isOfficial: true,
                agentId: null, modelId: null, priority: first.Priority)
        };

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultGuardrails.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultGuardrails.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GuardrailsSeeded.Should().Be(DefaultGuardrailCatalog.GetAll().Count - 1);
    }

    [Fact]
    public async Task Handle_Second_Run_Seeds_Nothing()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AiGuardrail>());

        var handler = new SeedDefaultGuardrails.Handler(_repository);
        var first = await handler.Handle(new SeedDefaultGuardrails.Command(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.GuardrailsSeeded.Should().BeGreaterThan(0);

        var existing = DefaultGuardrailCatalog.GetAll()
            .Select(d => AiGuardrail.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, guardType: d.GuardType, pattern: d.Pattern,
                patternType: d.PatternType, severity: d.Severity, action: d.Action,
                userMessage: d.UserMessage, isActive: true, isOfficial: true,
                agentId: null, modelId: null, priority: d.Priority))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var second = await handler.Handle(new SeedDefaultGuardrails.Command(), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.GuardrailsSeeded.Should().Be(0);
    }
}
