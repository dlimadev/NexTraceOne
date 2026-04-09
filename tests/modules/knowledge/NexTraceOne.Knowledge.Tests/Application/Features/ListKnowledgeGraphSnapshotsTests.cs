using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.ListKnowledgeGraphSnapshots;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a feature ListKnowledgeGraphSnapshots.
/// Valida listagem com e sem filtros e validação de status.
/// </summary>
public sealed class ListKnowledgeGraphSnapshotsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IKnowledgeGraphSnapshotRepository _snapshotRepo = Substitute.For<IKnowledgeGraphSnapshotRepository>();

    [Fact]
    public async Task Handle_NoFilter_ShouldReturnAll()
    {
        // Arrange
        var snapshot1 = KnowledgeGraphSnapshot.Generate(
            totalNodes: 10, totalEdges: 5, connectedComponents: 2, isolatedNodes: 1,
            coverageScore: 60, nodeTypeDistribution: "{\"Document\":10}",
            edgeTypeDistribution: "{\"Service\":5}", topConnectedEntities: null,
            orphanEntities: null, recommendations: null,
            tenantId: null, generatedAt: FixedNow);

        var snapshot2 = KnowledgeGraphSnapshot.Generate(
            totalNodes: 20, totalEdges: 15, connectedComponents: 1, isolatedNodes: 0,
            coverageScore: 90, nodeTypeDistribution: "{\"Document\":12,\"Service\":8}",
            edgeTypeDistribution: "{\"Service\":10,\"Contract\":5}", topConnectedEntities: null,
            orphanEntities: null, recommendations: null,
            tenantId: null, generatedAt: FixedNow.AddHours(1));

        _snapshotRepo
            .ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeGraphSnapshot> { snapshot2, snapshot1 });

        var handler = new ListKnowledgeGraphSnapshots.Handler(_snapshotRepo);
        var query = new ListKnowledgeGraphSnapshots.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items[0].TotalNodes.Should().Be(20);
        result.Value.Items[1].TotalNodes.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldReturnFiltered()
    {
        // Arrange
        var snapshot = KnowledgeGraphSnapshot.Generate(
            totalNodes: 15, totalEdges: 10, connectedComponents: 1, isolatedNodes: 0,
            coverageScore: 80, nodeTypeDistribution: "{\"Document\":15}",
            edgeTypeDistribution: "{\"Service\":10}", topConnectedEntities: null,
            orphanEntities: null, recommendations: null,
            tenantId: null, generatedAt: FixedNow);

        _snapshotRepo
            .ListAsync(KnowledgeGraphSnapshotStatus.Generated, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeGraphSnapshot> { snapshot });

        var handler = new ListKnowledgeGraphSnapshots.Handler(_snapshotRepo);
        var query = new ListKnowledgeGraphSnapshots.Query(Status: "Generated");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(nameof(KnowledgeGraphSnapshotStatus.Generated));
    }

    [Fact]
    public async Task Handle_EmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        _snapshotRepo
            .ListAsync(KnowledgeGraphSnapshotStatus.Stale, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeGraphSnapshot>());

        var handler = new ListKnowledgeGraphSnapshots.Handler(_snapshotRepo);
        var query = new ListKnowledgeGraphSnapshots.Query(Status: "Stale");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Validator_InvalidStatus_ShouldFail()
    {
        // Arrange
        var validator = new ListKnowledgeGraphSnapshots.Validator();
        var query = new ListKnowledgeGraphSnapshots.Query(Status: "InvalidStatus");

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }
}
