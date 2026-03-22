using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateGovernancePack;
using NexTraceOne.Governance.Application.Features.GetGovernancePack;
using NexTraceOne.Governance.Application.Features.ListGovernancePacks;
using NexTraceOne.Governance.Application.Features.UpdateGovernancePack;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de gestão de governance packs.
/// </summary>
public sealed class GovernancePackFeatureTests
{
    private readonly IGovernancePackRepository _packRepository = Substitute.For<IGovernancePackRepository>();
    private readonly IGovernancePackVersionRepository _versionRepository = Substitute.For<IGovernancePackVersionRepository>();
    private readonly IGovernanceRolloutRecordRepository _rolloutRecordRepository = Substitute.For<IGovernanceRolloutRecordRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // ── CreateGovernancePack ──

    [Fact]
    public async Task CreatePack_ValidData_ShouldReturnPackId()
    {
        // Arrange
        _packRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new CreateGovernancePack.Command("contract-standards", "Contract Standards", "Ensures contracts meet standards", "Contracts");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PackId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.Value.PackId, out _).Should().BeTrue();
        await _packRepository.Received(1).AddAsync(Arg.Any<GovernancePack>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePack_DuplicateName_ShouldReturnConflictError()
    {
        // Arrange
        var existing = GovernancePack.Create("contract-standards", "Contract Standards", null, GovernanceRuleCategory.Contracts);
        _packRepository.GetByNameAsync("contract-standards", Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new CreateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new CreateGovernancePack.Command("contract-standards", "Contract Standards", null, "Contracts");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PACK_NAME_EXISTS");
    }

    [Fact]
    public async Task CreatePack_InvalidCategory_ShouldReturnValidationError()
    {
        // Arrange
        _packRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var handler = new CreateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new CreateGovernancePack.Command("test", "Test", null, "NonExistentCategory");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CATEGORY");
    }

    [Theory]
    [InlineData("Contracts")]
    [InlineData("SourceOfTruth")]
    [InlineData("Changes")]
    [InlineData("Incidents")]
    [InlineData("AIGovernance")]
    [InlineData("Reliability")]
    [InlineData("Operations")]
    public async Task CreatePack_AllValidCategories_ShouldSucceed(string category)
    {
        // Arrange
        _packRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new CreateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new CreateGovernancePack.Command($"pack-{category}", $"Pack {category}", null, category);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ── ListGovernancePacks ──

    [Fact]
    public async Task ListPacks_WithData_ShouldReturnItems()
    {
        // Arrange
        var packs = new List<GovernancePack>
        {
            GovernancePack.Create("pack-a", "Pack A", "Desc A", GovernanceRuleCategory.Contracts),
            GovernancePack.Create("pack-b", "Pack B", "Desc B", GovernanceRuleCategory.Changes)
        };

        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(packs);
        _versionRepository.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRecordRepository.ListByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());

        var handler = new ListGovernancePacks.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);
        var query = new ListGovernancePacks.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPacks.Should().Be(2);
        result.Value.Packs.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListPacks_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        _packRepository.ListAsync(Arg.Any<GovernanceRuleCategory?>(), Arg.Any<GovernancePackStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePack>());

        var handler = new ListGovernancePacks.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);

        // Act
        var result = await handler.Handle(new ListGovernancePacks.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Packs.Should().BeEmpty();
        result.Value.TotalPacks.Should().Be(0);
    }

    // ── GetGovernancePack ──

    [Fact]
    public async Task GetPack_ValidId_ShouldReturnDetail()
    {
        // Arrange
        var pack = GovernancePack.Create("contract-standards", "Contract Standards", "Desc", GovernanceRuleCategory.Contracts);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _versionRepository.ListByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernancePackVersion>());
        _versionRepository.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);
        _rolloutRecordRepository.ListByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(new List<GovernanceRolloutRecord>());

        var handler = new GetGovernancePack.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);
        var query = new GetGovernancePack.Query(pack.Id.Value.ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Pack.Name.Should().Be("contract-standards");
        result.Value.Pack.Category.Should().Be(GovernanceRuleCategory.Contracts);
    }

    [Fact]
    public async Task GetPack_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new GetGovernancePack.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);

        // Act
        var result = await handler.Handle(new GetGovernancePack.Query("not-a-guid"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PACK_ID");
    }

    [Fact]
    public async Task GetPack_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var handler = new GetGovernancePack.Handler(_packRepository, _versionRepository, _rolloutRecordRepository);

        // Act
        var result = await handler.Handle(new GetGovernancePack.Query(Guid.NewGuid().ToString()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PACK_NOT_FOUND");
    }

    // ── UpdateGovernancePack ──

    [Fact]
    public async Task UpdatePack_ValidData_ShouldSucceed()
    {
        // Arrange
        var pack = GovernancePack.Create("test", "Test Pack", null, GovernanceRuleCategory.Contracts);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var handler = new UpdateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new UpdateGovernancePack.Command(pack.Id.Value.ToString(), "Updated Pack", "New desc", "Changes");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PackId.Should().Be(pack.Id.Value.ToString());
        await _packRepository.Received(1).UpdateAsync(Arg.Any<GovernancePack>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePack_InvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange
        var handler = new UpdateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new UpdateGovernancePack.Command("bad", "Name", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PACK_ID");
    }

    [Fact]
    public async Task UpdatePack_InvalidCategory_ShouldReturnValidationError()
    {
        // Arrange
        var pack = GovernancePack.Create("test", "Test Pack", null, GovernanceRuleCategory.Contracts);
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns(pack);

        var handler = new UpdateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new UpdateGovernancePack.Command(pack.Id.Value.ToString(), "Name", null, "InvalidCat");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CATEGORY");
    }

    [Fact]
    public async Task UpdatePack_NotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _packRepository.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var handler = new UpdateGovernancePack.Handler(_packRepository, _unitOfWork);
        var command = new UpdateGovernancePack.Command(Guid.NewGuid().ToString(), "Name", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PACK_NOT_FOUND");
    }
}
