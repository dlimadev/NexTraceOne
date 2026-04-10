using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Domain;

/// <summary>
/// Testes de unidade para o domínio KnowledgeGraphSnapshot.
/// Valida criação, transições de estado e invariantes de negócio.
/// </summary>
public sealed class KnowledgeGraphSnapshotTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_WithValidInputs_ShouldCreateSnapshot()
    {
        // Act
        var snapshot = KnowledgeGraphSnapshot.Generate(
            totalNodes: 25,
            totalEdges: 40,
            connectedComponents: 3,
            isolatedNodes: 2,
            coverageScore: 85,
            nodeTypeDistribution: "{\"Document\":15,\"Service\":10}",
            edgeTypeDistribution: "{\"Service\":20,\"Contract\":20}",
            topConnectedEntities: "[{\"id\":\"abc\",\"connections\":10}]",
            orphanEntities: "[{\"id\":\"xyz\"}]",
            recommendations: "[\"Link orphan documents to services\"]",
            tenantId: Guid.NewGuid(),
            generatedAt: FixedNow);

        // Assert
        snapshot.Id.Should().NotBeNull();
        snapshot.Id.Value.Should().NotBeEmpty();
        snapshot.TotalNodes.Should().Be(25);
        snapshot.TotalEdges.Should().Be(40);
        snapshot.ConnectedComponents.Should().Be(3);
        snapshot.IsolatedNodes.Should().Be(2);
        snapshot.CoverageScore.Should().Be(85);
        snapshot.NodeTypeDistribution.Should().Be("{\"Document\":15,\"Service\":10}");
        snapshot.EdgeTypeDistribution.Should().Be("{\"Service\":20,\"Contract\":20}");
        snapshot.TopConnectedEntities.Should().NotBeNull();
        snapshot.OrphanEntities.Should().NotBeNull();
        snapshot.Recommendations.Should().NotBeNull();
        snapshot.Status.Should().Be(KnowledgeGraphSnapshotStatus.Generated);
        snapshot.GeneratedAt.Should().Be(FixedNow);
        snapshot.ReviewedAt.Should().BeNull();
        snapshot.ReviewComment.Should().BeNull();
    }

    [Fact]
    public void Generate_WithNegativeNodes_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeGraphSnapshot.Generate(
            totalNodes: -1,
            totalEdges: 0,
            connectedComponents: 0,
            isolatedNodes: 0,
            coverageScore: 0,
            nodeTypeDistribution: "{}",
            edgeTypeDistribution: "{}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_WithCoverageScoreAbove100_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeGraphSnapshot.Generate(
            totalNodes: 10,
            totalEdges: 5,
            connectedComponents: 1,
            isolatedNodes: 0,
            coverageScore: 101,
            nodeTypeDistribution: "{\"Document\":10}",
            edgeTypeDistribution: "{\"Service\":5}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("coverageScore");
    }

    [Fact]
    public void Generate_WithCoverageScoreBelow0_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeGraphSnapshot.Generate(
            totalNodes: 10,
            totalEdges: 5,
            connectedComponents: 1,
            isolatedNodes: 0,
            coverageScore: -1,
            nodeTypeDistribution: "{\"Document\":10}",
            edgeTypeDistribution: "{\"Service\":5}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("coverageScore");
    }

    [Fact]
    public void Generate_WithIsolatedNodesGreaterThanTotal_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeGraphSnapshot.Generate(
            totalNodes: 5,
            totalEdges: 0,
            connectedComponents: 5,
            isolatedNodes: 10,
            coverageScore: 0,
            nodeTypeDistribution: "{\"Document\":5}",
            edgeTypeDistribution: "{}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("isolatedNodes");
    }

    [Fact]
    public void Generate_WithEmptyNodeTypeDistribution_ShouldThrow()
    {
        // Act & Assert
        var act = () => KnowledgeGraphSnapshot.Generate(
            totalNodes: 10,
            totalEdges: 5,
            connectedComponents: 1,
            isolatedNodes: 0,
            coverageScore: 50,
            nodeTypeDistribution: "",
            edgeTypeDistribution: "{\"Service\":5}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Review_ShouldTransitionToReviewed()
    {
        // Arrange
        var snapshot = CreateValidSnapshot();
        var reviewedAt = FixedNow.AddHours(2);

        // Act
        var result = snapshot.Review("Looks good, coverage is improving.", reviewedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        snapshot.Status.Should().Be(KnowledgeGraphSnapshotStatus.Reviewed);
        snapshot.ReviewComment.Should().Be("Looks good, coverage is improving.");
        snapshot.ReviewedAt.Should().Be(reviewedAt);
    }

    [Fact]
    public void Review_AlreadyReviewed_ShouldReturnError()
    {
        // Arrange
        var snapshot = CreateValidSnapshot();
        snapshot.Review("First review", FixedNow.AddHours(1));

        // Act
        var result = snapshot.Review("Second review", FixedNow.AddHours(2));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KNOWLEDGE_GRAPH_SNAPSHOT_ALREADY_REVIEWED");
    }

    [Fact]
    public void MarkAsStale_ShouldTransitionToStale()
    {
        // Arrange
        var snapshot = CreateValidSnapshot();

        // Act
        var result = snapshot.MarkAsStale();

        // Assert
        result.IsSuccess.Should().BeTrue();
        snapshot.Status.Should().Be(KnowledgeGraphSnapshotStatus.Stale);
    }

    [Fact]
    public void MarkAsStale_AlreadyStale_ShouldReturnError()
    {
        // Arrange
        var snapshot = CreateValidSnapshot();
        snapshot.MarkAsStale();

        // Act
        var result = snapshot.MarkAsStale();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KNOWLEDGE_GRAPH_SNAPSHOT_ALREADY_STALE");
    }

    [Fact]
    public void Generate_WithZeroNodesZeroEdges_ShouldSucceed()
    {
        // Act
        var snapshot = KnowledgeGraphSnapshot.Generate(
            totalNodes: 0,
            totalEdges: 0,
            connectedComponents: 0,
            isolatedNodes: 0,
            coverageScore: 0,
            nodeTypeDistribution: "{}",
            edgeTypeDistribution: "{}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        // Assert
        snapshot.TotalNodes.Should().Be(0);
        snapshot.TotalEdges.Should().Be(0);
        snapshot.CoverageScore.Should().Be(0);
        snapshot.Status.Should().Be(KnowledgeGraphSnapshotStatus.Generated);
    }

    [Fact]
    public void Generate_WithNullOptionalFields_ShouldSucceed()
    {
        // Act
        var snapshot = KnowledgeGraphSnapshot.Generate(
            totalNodes: 10,
            totalEdges: 8,
            connectedComponents: 2,
            isolatedNodes: 1,
            coverageScore: 72,
            nodeTypeDistribution: "{\"Document\":10}",
            edgeTypeDistribution: "{\"Service\":8}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: null,
            generatedAt: FixedNow);

        // Assert
        snapshot.TopConnectedEntities.Should().BeNull();
        snapshot.OrphanEntities.Should().BeNull();
        snapshot.Recommendations.Should().BeNull();
        snapshot.TenantId.Should().BeNull();
    }

    private static KnowledgeGraphSnapshot CreateValidSnapshot()
    {
        return KnowledgeGraphSnapshot.Generate(
            totalNodes: 20,
            totalEdges: 30,
            connectedComponents: 2,
            isolatedNodes: 3,
            coverageScore: 75,
            nodeTypeDistribution: "{\"Document\":12,\"Service\":8}",
            edgeTypeDistribution: "{\"Service\":15,\"Contract\":15}",
            topConnectedEntities: null,
            orphanEntities: null,
            recommendations: null,
            tenantId: Guid.NewGuid(),
            generatedAt: FixedNow);
    }
}
