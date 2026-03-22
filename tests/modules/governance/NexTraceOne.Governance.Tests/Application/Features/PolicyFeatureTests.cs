using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetPolicy;
using NexTraceOne.Governance.Application.Features.ListPolicies;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de políticas de governança.
/// </summary>
public sealed class PolicyFeatureTests
{
    private readonly IGovernancePackRepository _packRepository = Substitute.For<IGovernancePackRepository>();
    private readonly IGovernancePackVersionRepository _versionRepository = Substitute.For<IGovernancePackVersionRepository>();
    private readonly IGovernanceWaiverRepository _waiverRepository = Substitute.For<IGovernanceWaiverRepository>();
    private readonly IGovernanceRolloutRecordRepository _rolloutRepository = Substitute.For<IGovernanceRolloutRecordRepository>();

    // ── GetPolicy ──

    [Fact]
    public async Task GetPolicy_ValidGuidId_ShouldReturnPolicy()
    {
        // Arrange
        var pack = GovernancePack.Create("ownership-required", "Ownership Required", "Desc", GovernanceRuleCategory.SourceOfTruth);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _versionRepository.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<GovernanceScopeType?>(), Arg.Any<string?>(), Arg.Any<RolloutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());
        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());

        var handler = new GetPolicy.Handler(_packRepository, _versionRepository, _waiverRepository, _rolloutRepository);
        var query = new GetPolicy.Query(pack.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Policy.Name.Should().Be("ownership-required");
    }

    [Fact]
    public async Task GetPolicy_ByName_ShouldReturnPolicy()
    {
        // Arrange
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);
        var pack = GovernancePack.Create("contract-standards", "Contract Standards", null, GovernanceRuleCategory.Contracts);
        _packRepository.GetByNameAsync("contract-standards", Arg.Any<CancellationToken>())
            .Returns(pack);
        _versionRepository.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<GovernanceScopeType?>(), Arg.Any<string?>(), Arg.Any<RolloutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());
        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());

        var handler = new GetPolicy.Handler(_packRepository, _versionRepository, _waiverRepository, _rolloutRepository);

        // Act
        var result = await handler.Handle(new GetPolicy.Query("contract-standards"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Policy.Name.Should().Be("contract-standards");
    }

    [Fact]
    public async Task GetPolicy_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);
        _packRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var handler = new GetPolicy.Handler(_packRepository, _versionRepository, _waiverRepository, _rolloutRepository);

        // Act
        var result = await handler.Handle(new GetPolicy.Query("nonexistent"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("POLICY_NOT_FOUND");
    }

    // ── ListPolicies ──

    [Fact]
    public async Task ListPolicies_WithData_ShouldReturnPolicies()
    {
        // Arrange
        var packs = new List<GovernancePack>
        {
            GovernancePack.Create("pack-a", "Pack A", null, GovernanceRuleCategory.Contracts),
            GovernancePack.Create("pack-b", "Pack B", null, GovernanceRuleCategory.Changes)
        };

        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(packs);
        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _rolloutRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<GovernanceScopeType?>(), Arg.Any<string?>(), Arg.Any<RolloutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());

        var handler = new ListPolicies.Handler(_packRepository, _waiverRepository, _rolloutRepository);
        var query = new ListPolicies.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().HaveCount(2);
        result.Value.TotalPolicies.Should().Be(2);
    }

    [Fact]
    public async Task ListPolicies_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack>());
        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _rolloutRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<GovernanceScopeType?>(), Arg.Any<string?>(), Arg.Any<RolloutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());

        var handler = new ListPolicies.Handler(_packRepository, _waiverRepository, _rolloutRepository);

        // Act
        var result = await handler.Handle(new ListPolicies.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().BeEmpty();
    }

    [Fact]
    public async Task ListPolicies_ShouldReturnCorrectDraftAndActiveCount()
    {
        // Arrange
        var packs = new List<GovernancePack>
        {
            GovernancePack.Create("pack-draft", "Draft Pack", null, GovernanceRuleCategory.Contracts),
        };

        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(packs);
        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());
        _rolloutRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<GovernanceScopeType?>(), Arg.Any<string?>(), Arg.Any<RolloutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());

        var handler = new ListPolicies.Handler(_packRepository, _waiverRepository, _rolloutRepository);

        // Act
        var result = await handler.Handle(new ListPolicies.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DraftCount.Should().Be(1);
    }
}
