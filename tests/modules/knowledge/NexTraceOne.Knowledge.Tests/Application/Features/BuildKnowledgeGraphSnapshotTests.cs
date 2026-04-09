using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.BuildKnowledgeGraphSnapshot;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a feature BuildKnowledgeGraphSnapshot.
/// Valida criação de snapshots, marcação de anteriores como stale e validação.
/// </summary>
public sealed class BuildKnowledgeGraphSnapshotTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IKnowledgeGraphSnapshotRepository _snapshotRepo = Substitute.For<IKnowledgeGraphSnapshotRepository>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public BuildKnowledgeGraphSnapshotTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateSnapshotAndCommit()
    {
        // Arrange
        _snapshotRepo.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((KnowledgeGraphSnapshot?)null);
        var handler = new BuildKnowledgeGraphSnapshot.Handler(_snapshotRepo, _currentTenant, _clock, _unitOfWork);
        var command = new BuildKnowledgeGraphSnapshot.Command(
            TotalNodes: 25,
            TotalEdges: 40,
            ConnectedComponents: 3,
            IsolatedNodes: 2,
            CoverageScore: 85,
            NodeTypeDistribution: "{\"Document\":15,\"Service\":10}",
            EdgeTypeDistribution: "{\"Service\":20,\"Contract\":20}",
            TopConnectedEntities: "[{\"id\":\"abc\"}]",
            OrphanEntities: null,
            Recommendations: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SnapshotId.Should().NotBeEmpty();
        result.Value.TotalNodes.Should().Be(25);
        result.Value.TotalEdges.Should().Be(40);
        result.Value.ConnectedComponents.Should().Be(3);
        result.Value.IsolatedNodes.Should().Be(2);
        result.Value.CoverageScore.Should().Be(85);
        result.Value.Status.Should().Be(nameof(KnowledgeGraphSnapshotStatus.Generated));
        result.Value.GeneratedAt.Should().Be(FixedNow);

        _snapshotRepo.Received(1).Add(Arg.Any<KnowledgeGraphSnapshot>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPreviousSnapshot_ShouldMarkAsStale()
    {
        // Arrange
        var previousSnapshot = KnowledgeGraphSnapshot.Generate(
            totalNodes: 10, totalEdges: 5, connectedComponents: 2, isolatedNodes: 1,
            coverageScore: 50, nodeTypeDistribution: "{\"Document\":10}",
            edgeTypeDistribution: "{\"Service\":5}", topConnectedEntities: null,
            orphanEntities: null, recommendations: null,
            tenantId: Guid.NewGuid(), generatedAt: FixedNow.AddDays(-1));

        _snapshotRepo.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(previousSnapshot);

        var handler = new BuildKnowledgeGraphSnapshot.Handler(_snapshotRepo, _currentTenant, _clock, _unitOfWork);
        var command = new BuildKnowledgeGraphSnapshot.Command(
            TotalNodes: 30, TotalEdges: 50, ConnectedComponents: 2, IsolatedNodes: 0,
            CoverageScore: 90, NodeTypeDistribution: "{\"Document\":20,\"Service\":10}",
            EdgeTypeDistribution: "{\"Service\":25,\"Contract\":25}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        previousSnapshot.Status.Should().Be(KnowledgeGraphSnapshotStatus.Stale);
        _snapshotRepo.Received(1).Update(previousSnapshot);
        _snapshotRepo.Received(1).Add(Arg.Any<KnowledgeGraphSnapshot>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoPreviousSnapshot_ShouldCreateDirectly()
    {
        // Arrange
        _snapshotRepo.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((KnowledgeGraphSnapshot?)null);

        var handler = new BuildKnowledgeGraphSnapshot.Handler(_snapshotRepo, _currentTenant, _clock, _unitOfWork);
        var command = new BuildKnowledgeGraphSnapshot.Command(
            TotalNodes: 5, TotalEdges: 3, ConnectedComponents: 1, IsolatedNodes: 0,
            CoverageScore: 60, NodeTypeDistribution: "{\"Document\":5}",
            EdgeTypeDistribution: "{\"Service\":3}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _snapshotRepo.DidNotReceive().Update(Arg.Any<KnowledgeGraphSnapshot>());
        _snapshotRepo.Received(1).Add(Arg.Any<KnowledgeGraphSnapshot>());
    }

    [Fact]
    public void Validator_InvalidCoverageScore_ShouldFail()
    {
        // Arrange
        var validator = new BuildKnowledgeGraphSnapshot.Validator();
        var command = new BuildKnowledgeGraphSnapshot.Command(
            TotalNodes: 10, TotalEdges: 5, ConnectedComponents: 1, IsolatedNodes: 0,
            CoverageScore: 150, NodeTypeDistribution: "{\"Document\":10}",
            EdgeTypeDistribution: "{\"Service\":5}");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CoverageScore");
    }

    [Fact]
    public void Validator_NegativeNodes_ShouldFail()
    {
        // Arrange
        var validator = new BuildKnowledgeGraphSnapshot.Validator();
        var command = new BuildKnowledgeGraphSnapshot.Command(
            TotalNodes: -1, TotalEdges: 0, ConnectedComponents: 0, IsolatedNodes: 0,
            CoverageScore: 0, NodeTypeDistribution: "{}",
            EdgeTypeDistribution: "{}");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalNodes");
    }

    [Fact]
    public void Validator_EmptyDistributions_ShouldFail()
    {
        // Arrange
        var validator = new BuildKnowledgeGraphSnapshot.Validator();
        var command = new BuildKnowledgeGraphSnapshot.Command(
            TotalNodes: 10, TotalEdges: 5, ConnectedComponents: 1, IsolatedNodes: 0,
            CoverageScore: 50, NodeTypeDistribution: "",
            EdgeTypeDistribution: "");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NodeTypeDistribution");
        result.Errors.Should().Contain(e => e.PropertyName == "EdgeTypeDistribution");
    }
}
