using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeGraphSnapshot;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a feature GetKnowledgeGraphSnapshot.
/// Valida obtenção de snapshots por ID e cenários de não encontrado.
/// </summary>
public sealed class GetKnowledgeGraphSnapshotTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IKnowledgeGraphSnapshotRepository _snapshotRepo = Substitute.For<IKnowledgeGraphSnapshotRepository>();

    [Fact]
    public async Task Handle_ExistingSnapshot_ShouldReturnDetails()
    {
        // Arrange
        var snapshot = KnowledgeGraphSnapshot.Generate(
            totalNodes: 20, totalEdges: 30, connectedComponents: 2, isolatedNodes: 3,
            coverageScore: 75, nodeTypeDistribution: "{\"Document\":12,\"Service\":8}",
            edgeTypeDistribution: "{\"Service\":15,\"Contract\":15}",
            topConnectedEntities: "[{\"id\":\"svc-1\",\"connections\":8}]",
            orphanEntities: "[{\"id\":\"doc-orphan\"}]",
            recommendations: "[\"Connect orphan docs\"]",
            tenantId: Guid.NewGuid(), generatedAt: FixedNow);

        _snapshotRepo
            .GetByIdAsync(Arg.Is<KnowledgeGraphSnapshotId>(id => id.Value == snapshot.Id.Value), Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var handler = new GetKnowledgeGraphSnapshot.Handler(_snapshotRepo);
        var query = new GetKnowledgeGraphSnapshot.Query(snapshot.Id.Value);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SnapshotId.Should().Be(snapshot.Id.Value);
        result.Value.TotalNodes.Should().Be(20);
        result.Value.TotalEdges.Should().Be(30);
        result.Value.ConnectedComponents.Should().Be(2);
        result.Value.IsolatedNodes.Should().Be(3);
        result.Value.CoverageScore.Should().Be(75);
        result.Value.NodeTypeDistribution.Should().Be("{\"Document\":12,\"Service\":8}");
        result.Value.EdgeTypeDistribution.Should().Be("{\"Service\":15,\"Contract\":15}");
        result.Value.TopConnectedEntities.Should().NotBeNull();
        result.Value.OrphanEntities.Should().NotBeNull();
        result.Value.Recommendations.Should().NotBeNull();
        result.Value.Status.Should().Be(nameof(KnowledgeGraphSnapshotStatus.Generated));
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.ReviewedAt.Should().BeNull();
        result.Value.ReviewComment.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistingSnapshot_ShouldReturnNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _snapshotRepo
            .GetByIdAsync(Arg.Is<KnowledgeGraphSnapshotId>(id => id.Value == missingId), Arg.Any<CancellationToken>())
            .Returns((KnowledgeGraphSnapshot?)null);

        var handler = new GetKnowledgeGraphSnapshot.Handler(_snapshotRepo);
        var query = new GetKnowledgeGraphSnapshot.Query(missingId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KNOWLEDGE_GRAPH_SNAPSHOT_NOT_FOUND");
    }

    [Fact]
    public void Validator_EmptyId_ShouldFail()
    {
        // Arrange
        var validator = new GetKnowledgeGraphSnapshot.Validator();
        var query = new GetKnowledgeGraphSnapshot.Query(Guid.Empty);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SnapshotId");
    }
}
