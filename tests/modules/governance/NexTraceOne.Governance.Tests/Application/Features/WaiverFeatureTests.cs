using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ApproveGovernanceWaiver;
using NexTraceOne.Governance.Application.Features.CreateGovernanceWaiver;
using NexTraceOne.Governance.Application.Features.ListGovernanceWaivers;
using NexTraceOne.Governance.Application.Features.RejectGovernanceWaiver;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de governance waivers.
/// </summary>
public sealed class WaiverFeatureTests
{
    private readonly IGovernanceWaiverRepository _waiverRepository = Substitute.For<IGovernanceWaiverRepository>();
    private readonly IGovernancePackRepository _packRepository = Substitute.For<IGovernancePackRepository>();
    private readonly IGovernancePackVersionRepository _versionRepository = Substitute.For<IGovernancePackVersionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

    // ── CreateGovernanceWaiver ──

    [Fact]
    public async Task CreateWaiver_ValidData_ShouldReturnWaiverId()
    {
        // Arrange
        var pack = GovernancePack.Create("test-pack", "Test Pack", null, GovernanceRuleCategory.Contracts);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateGovernanceWaiver.Handler(_waiverRepository, _packRepository, _unitOfWork);
        var command = new CreateGovernanceWaiver.Command(
            PackId: pack.Id.Value.ToString(),
            RuleId: "rule-001",
            Scope: "payments",
            ScopeType: "Domain",
            Justification: "Legacy system cannot comply within deadline",
            RequestedBy: "engineer@company.com",
            ExpiresAt: DateTimeOffset.UtcNow.AddMonths(3),
            EvidenceLinks: new[] { "https://jira.company.com/PROJ-123" });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WaiverId.Should().NotBeNullOrWhiteSpace();
        await _waiverRepository.Received(1).AddAsync(Arg.Any<GovernanceWaiver>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWaiver_InvalidPackId_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new CreateGovernanceWaiver.Handler(_waiverRepository, _packRepository, _unitOfWork);
        var command = new CreateGovernanceWaiver.Command("not-guid", null, "scope", "Global", "reason", "user", null, []);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PACK_ID");
    }

    [Fact]
    public async Task CreateWaiver_PackNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var handler = new CreateGovernanceWaiver.Handler(_waiverRepository, _packRepository, _unitOfWork);
        var command = new CreateGovernanceWaiver.Command(Guid.NewGuid().ToString(), null, "scope", "Global", "reason", "user", null, []);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PACK_NOT_FOUND");
    }

    [Fact]
    public async Task CreateWaiver_InvalidScopeType_ShouldReturnValidationError()
    {
        // Arrange
        var pack = GovernancePack.Create("test", "Test", null, GovernanceRuleCategory.Contracts);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);

        var handler = new CreateGovernanceWaiver.Handler(_waiverRepository, _packRepository, _unitOfWork);
        var command = new CreateGovernanceWaiver.Command(pack.Id.Value.ToString(), null, "scope", "InvalidScope", "reason", "user", null, []);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_SCOPE_TYPE");
    }

    // ── ListGovernanceWaivers ──

    [Fact]
    public async Task ListWaivers_WithData_ShouldReturnItems()
    {
        // Arrange
        var packId = new GovernancePackId(Guid.NewGuid());
        var waivers = new List<GovernanceWaiver>
        {
            GovernanceWaiver.Create(packId, "rule-1", "payments", GovernanceScopeType.Domain, "reason", "user1", null, []),
            GovernanceWaiver.Create(packId, null, "global", GovernanceScopeType.Global, "reason2", "user2", null, [])
        };

        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(waivers);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(GovernancePack.Create("test", "Test Pack", null, GovernanceRuleCategory.Contracts));
        _versionRepository.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);

        var handler = new ListGovernanceWaivers.Handler(_waiverRepository, _packRepository, _versionRepository);
        var query = new ListGovernanceWaivers.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalWaivers.Should().Be(2);
        result.Value.PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task ListWaivers_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _waiverRepository.ListAsync(Arg.Any<GovernancePackId?>(), Arg.Any<WaiverStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceWaiver>());

        var handler = new ListGovernanceWaivers.Handler(_waiverRepository, _packRepository, _versionRepository);

        // Act
        var result = await handler.Handle(new ListGovernanceWaivers.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Waivers.Should().BeEmpty();
    }

    // ── ApproveGovernanceWaiver ──

    [Fact]
    public async Task ApproveWaiver_ValidData_ShouldSucceed()
    {
        // Arrange
        var waiver = GovernanceWaiver.Create(
            new GovernancePackId(Guid.NewGuid()), null, "scope", GovernanceScopeType.Global,
            "reason", "requester", null, []);
        _waiverRepository.GetByIdAsync(Arg.Any<GovernanceWaiverId>(), Arg.Any<CancellationToken>())
            .Returns(waiver);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new ApproveGovernanceWaiver.Handler(_waiverRepository, _unitOfWork);
        var command = new ApproveGovernanceWaiver.Command(waiver.Id.Value.ToString(), "reviewer@company.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WaiverId.Should().Be(waiver.Id.Value.ToString());
        await _waiverRepository.Received(1).UpdateAsync(Arg.Any<GovernanceWaiver>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveWaiver_InvalidGuid_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new ApproveGovernanceWaiver.Handler(_waiverRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(new ApproveGovernanceWaiver.Command("bad", "reviewer"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_WAIVER_ID");
    }

    // ── RejectGovernanceWaiver ──

    [Fact]
    public async Task RejectWaiver_ValidData_ShouldSucceed()
    {
        // Arrange
        var waiver = GovernanceWaiver.Create(
            new GovernancePackId(Guid.NewGuid()), null, "scope", GovernanceScopeType.Global,
            "reason", "requester", null, []);
        _waiverRepository.GetByIdAsync(Arg.Any<GovernanceWaiverId>(), Arg.Any<CancellationToken>())
            .Returns(waiver);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new RejectGovernanceWaiver.Handler(_waiverRepository, _unitOfWork);
        var command = new RejectGovernanceWaiver.Command(waiver.Id.Value.ToString(), "reviewer@company.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WaiverId.Should().Be(waiver.Id.Value.ToString());
    }

    [Fact]
    public async Task RejectWaiver_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _waiverRepository.GetByIdAsync(Arg.Any<GovernanceWaiverId>(), Arg.Any<CancellationToken>())
            .Returns((GovernanceWaiver?)null);

        var handler = new RejectGovernanceWaiver.Handler(_waiverRepository, _unitOfWork);

        // Act
        var result = await handler.Handle(new RejectGovernanceWaiver.Command(Guid.NewGuid().ToString(), "reviewer"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("WAIVER_NOT_FOUND");
    }
}
