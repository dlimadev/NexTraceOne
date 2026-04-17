using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateDelegatedAdministration;
using NexTraceOne.Governance.Application.Features.ListDelegatedAdministrations;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de administração delegada.
/// </summary>
public sealed class DelegatedAdminFeatureTests
{
    private readonly IDelegatedAdministrationRepository _delegationRepository = Substitute.For<IDelegatedAdministrationRepository>();
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly IGovernanceDomainRepository _domainRepository = Substitute.For<IGovernanceDomainRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

    // ── CreateDelegatedAdministration ──

    [Fact]
    public async Task CreateDelegation_ValidData_ShouldReturnDelegationId()
    {
        // Arrange
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateDelegatedAdministration.Handler(_delegationRepository, _unitOfWork);
        var command = new CreateDelegatedAdministration.Command(
            GranteeUserId: "user-123",
            GranteeDisplayName: "John Doe",
            Scope: "TeamAdmin",
            TeamId: Guid.NewGuid().ToString(),
            DomainId: null,
            Reason: "Temporary admin access for migration",
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(30));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DelegationId.Should().NotBeNullOrWhiteSpace();
        await _delegationRepository.Received(1).AddAsync(Arg.Any<DelegatedAdministration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDelegation_InvalidScope_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new CreateDelegatedAdministration.Handler(_delegationRepository, _unitOfWork);
        var command = new CreateDelegatedAdministration.Command(
            "user", "User", "InvalidScope", null, null, "reason", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_SCOPE");
    }

    [Theory]
    [InlineData("TeamAdmin")]
    [InlineData("DomainAdmin")]
    [InlineData("ReadOnly")]
    [InlineData("FullAdmin")]
    public async Task CreateDelegation_AllValidScopes_ShouldSucceed(string scope)
    {
        // Arrange
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateDelegatedAdministration.Handler(_delegationRepository, _unitOfWork);
        var command = new CreateDelegatedAdministration.Command("user", "User", scope, null, null, "reason", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ── ListDelegatedAdministrations ──

    [Fact]
    public async Task ListDelegations_WithData_ShouldReturnItems()
    {
        // Arrange
        var delegations = new List<DelegatedAdministration>
        {
            DelegatedAdministration.Create("user-1", "User One", DelegationScope.TeamAdmin, null, null, "reason1"),
            DelegatedAdministration.Create("user-2", "User Two", DelegationScope.DomainAdmin, null, null, "reason2")
        };

        _delegationRepository.ListAsync(Arg.Any<DelegationScope?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(delegations);

        var handler = new ListDelegatedAdministrations.Handler(_delegationRepository, _teamRepository, _domainRepository);
        var query = new ListDelegatedAdministrations.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Delegations.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListDelegations_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _delegationRepository.ListAsync(Arg.Any<DelegationScope?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new List<DelegatedAdministration>());

        var handler = new ListDelegatedAdministrations.Handler(_delegationRepository, _teamRepository, _domainRepository);

        // Act
        var result = await handler.Handle(new ListDelegatedAdministrations.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Delegations.Should().BeEmpty();
    }
}
