using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultPromptTemplates;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

public sealed class SeedDefaultPromptTemplatesTests
{
    private readonly IPromptTemplateRepository _repository = Substitute.For<IPromptTemplateRepository>();

    // ── Seeds all when empty ────────────────────────────────────────────

    [Fact]
    public async Task Handle_Empty_Registry_Seeds_All_Templates()
    {
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PromptTemplate>());

        var handler = new SeedDefaultPromptTemplates.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultPromptTemplates.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplatesSeeded.Should().Be(DefaultPromptTemplateCatalog.GetAll().Count);
        result.Value.TotalInCatalog.Should().Be(DefaultPromptTemplateCatalog.GetAll().Count);

        await _repository.Received(DefaultPromptTemplateCatalog.GetAll().Count)
            .AddAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>());
    }

    // ── Seeds nothing when all exist ────────────────────────────────────

    [Fact]
    public async Task Handle_All_Exist_Seeds_Nothing()
    {
        var existing = DefaultPromptTemplateCatalog.GetAll()
            .Select(d => PromptTemplate.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, content: d.Content, variables: d.Variables,
                version: 1, isActive: true, isOfficial: true, agentId: null,
                targetPersonas: d.TargetPersonas, scopeHint: d.ScopeHint,
                relevance: d.Relevance))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultPromptTemplates.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultPromptTemplates.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplatesSeeded.Should().Be(0);

        await _repository.DidNotReceive()
            .AddAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>());
    }

    // ── Seeds only missing on partial ───────────────────────────────────

    [Fact]
    public async Task Handle_Partial_Seeds_Only_Missing()
    {
        var catalog = DefaultPromptTemplateCatalog.GetAll();
        var firstTwo = catalog.Take(2)
            .Select(d => PromptTemplate.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, content: d.Content, variables: d.Variables,
                version: 1, isActive: true, isOfficial: true, agentId: null,
                targetPersonas: d.TargetPersonas, scopeHint: d.ScopeHint,
                relevance: d.Relevance))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(firstTwo.AsReadOnly());

        var handler = new SeedDefaultPromptTemplates.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultPromptTemplates.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplatesSeeded.Should().Be(catalog.Count - 2);
    }

    // ── Case-insensitive name matching ──────────────────────────────────

    [Fact]
    public async Task Handle_Case_Insensitive_Does_Not_Duplicate()
    {
        var first = DefaultPromptTemplateCatalog.GetAll().First();
        var existing = new List<PromptTemplate>
        {
            PromptTemplate.Create(
                name: first.Name.ToUpperInvariant(),
                displayName: first.DisplayName, description: first.Description,
                category: first.Category, content: first.Content, variables: first.Variables,
                version: 1, isActive: true, isOfficial: true, agentId: null,
                targetPersonas: first.TargetPersonas, scopeHint: first.ScopeHint,
                relevance: first.Relevance)
        };

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var handler = new SeedDefaultPromptTemplates.Handler(_repository);

        var result = await handler.Handle(new SeedDefaultPromptTemplates.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TemplatesSeeded.Should().Be(DefaultPromptTemplateCatalog.GetAll().Count - 1);
    }

    // ── Idempotency ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Second_Run_Seeds_Nothing()
    {
        // First run: empty
        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PromptTemplate>());

        var handler = new SeedDefaultPromptTemplates.Handler(_repository);
        var first = await handler.Handle(new SeedDefaultPromptTemplates.Command(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.TemplatesSeeded.Should().BeGreaterThan(0);

        // Second run: simulate all exist
        var existing = DefaultPromptTemplateCatalog.GetAll()
            .Select(d => PromptTemplate.Create(
                name: d.Name, displayName: d.DisplayName, description: d.Description,
                category: d.Category, content: d.Content, variables: d.Variables,
                version: 1, isActive: true, isOfficial: true, agentId: null,
                targetPersonas: d.TargetPersonas, scopeHint: d.ScopeHint,
                relevance: d.Relevance))
            .ToList();

        _repository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(existing.AsReadOnly());

        var second = await handler.Handle(new SeedDefaultPromptTemplates.Command(), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.TemplatesSeeded.Should().Be(0);
    }
}
