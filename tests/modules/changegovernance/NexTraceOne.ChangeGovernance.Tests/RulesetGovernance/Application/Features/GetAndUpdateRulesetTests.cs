using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRuleset;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.UpdateRuleset;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.RulesetGovernance.Application.Features;

/// <summary>
/// Testes unitários para GetRuleset (detalhe por id) e UpdateRuleset (actualização de conteúdo).
/// </summary>
public sealed class GetAndUpdateRulesetTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static Ruleset MakeRuleset(string content = "rules: {}") =>
        Ruleset.Create("API Naming", "Regras de nomenclatura", content, RulesetType.Custom, FixedNow);

    // ── GetRuleset ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRuleset_Existing_ReturnsDto()
    {
        var ruleset = MakeRuleset();
        var repo = Substitute.For<IRulesetRepository>();
        repo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);
        var handler = new GetRuleset.Handler(repo);

        var result = await handler.Handle(new GetRuleset.Query(ruleset.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RulesetId.Should().Be(ruleset.Id.Value);
        result.Value.Name.Should().Be("API Naming");
        result.Value.Content.Should().Be("rules: {}");
        result.Value.RulesetType.Should().Be("Custom");
    }

    [Fact]
    public async Task GetRuleset_Unknown_ReturnsNotFound()
    {
        var repo = Substitute.For<IRulesetRepository>();
        repo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns((Ruleset?)null);
        var handler = new GetRuleset.Handler(repo);

        var result = await handler.Handle(new GetRuleset.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ruleset.NotFound");
    }

    [Fact]
    public void GetRuleset_EmptyId_ValidationFails()
    {
        new GetRuleset.Validator().Validate(new GetRuleset.Query(Guid.Empty)).IsValid.Should().BeFalse();
    }

    // ── UpdateRuleset ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRuleset_Existing_UpdatesContentAndCommits()
    {
        var ruleset = MakeRuleset("old");
        var repo = Substitute.For<IRulesetRepository>();
        repo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new UpdateRuleset.Handler(repo, uow);

        var result = await handler.Handle(
            new UpdateRuleset.Command(ruleset.Id.Value, "new-content"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ruleset.Content.Should().Be("new-content");
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRuleset_Unknown_ReturnsNotFound()
    {
        var repo = Substitute.For<IRulesetRepository>();
        repo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns((Ruleset?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new UpdateRuleset.Handler(repo, uow);

        var result = await handler.Handle(
            new UpdateRuleset.Command(Guid.NewGuid(), "x"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ruleset.NotFound");
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    public void UpdateRuleset_EmptyContent_ValidationFails(string content)
    {
        new UpdateRuleset.Validator()
            .Validate(new UpdateRuleset.Command(Guid.NewGuid(), content))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateRuleset_ValidCommand_Passes()
    {
        new UpdateRuleset.Validator()
            .Validate(new UpdateRuleset.Command(Guid.NewGuid(), "rules: {}"))
            .IsValid.Should().BeTrue();
    }
}
