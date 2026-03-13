using FluentAssertions;
using NSubstitute;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using GetTemporalDiffFeature = NexTraceOne.EngineeringGraph.Application.Features.GetTemporalDiff.GetTemporalDiff;

namespace NexTraceOne.EngineeringGraph.Tests.Application.Features;

/// <summary>
/// Testes do handler GetTemporalDiff que compara dois snapshots do grafo
/// e retorna as diferenças de nós e arestas entre dois pontos no tempo.
/// Cenários cobertos: diff válido, snapshot de origem ausente e snapshot de destino ausente.
/// </summary>
public sealed class GetTemporalDiffTests
{
    private readonly IGraphSnapshotRepository _snapshotRepository = Substitute.For<IGraphSnapshotRepository>();
    private readonly GetTemporalDiffFeature.Handler _sut;

    public GetTemporalDiffTests()
    {
        _sut = new GetTemporalDiffFeature.Handler(_snapshotRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnDiff_When_BothSnapshotsExist()
    {
        // Arrange — dois snapshots com contagens diferentes para calcular delta
        var fromSnapshot = GraphSnapshot.Create(
            "Pre-release v1.0",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "[{\"id\":\"node1\"}]",
            "[{\"source\":\"n1\",\"target\":\"n2\"}]",
            nodeCount: 5,
            edgeCount: 3,
            "admin");

        var toSnapshot = GraphSnapshot.Create(
            "Post-release v1.1",
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            "[{\"id\":\"node1\"},{\"id\":\"node2\"}]",
            "[{\"source\":\"n1\",\"target\":\"n2\"},{\"source\":\"n2\",\"target\":\"n3\"}]",
            nodeCount: 8,
            edgeCount: 5,
            "admin");

        _snapshotRepository.GetByIdAsync(
                Arg.Is<GraphSnapshotId>(id => id.Value == fromSnapshot.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(fromSnapshot);

        _snapshotRepository.GetByIdAsync(
                Arg.Is<GraphSnapshotId>(id => id.Value == toSnapshot.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(toSnapshot);

        // Act
        var result = await _sut.Handle(
            new GetTemporalDiffFeature.Query(fromSnapshot.Id.Value, toSnapshot.Id.Value),
            CancellationToken.None);

        // Assert — delta = to - from
        result.IsSuccess.Should().BeTrue();
        result.Value.NodesAdded.Should().Be(3);
        result.Value.NodesRemoved.Should().Be(0);
        result.Value.EdgesAdded.Should().Be(2);
        result.Value.EdgesRemoved.Should().Be(0);
        result.Value.FromCapturedAt.Should().Be(fromSnapshot.CapturedAt);
        result.Value.ToCapturedAt.Should().Be(toSnapshot.CapturedAt);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_FromSnapshotIsMissing()
    {
        // Arrange — snapshot de origem não existe
        var toSnapshot = GraphSnapshot.Create(
            "Post-release",
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            "[{\"id\":\"n1\"}]",
            "[]",
            nodeCount: 1,
            edgeCount: 0,
            "system");

        var missingFromId = Guid.NewGuid();

        _snapshotRepository.GetByIdAsync(
                Arg.Is<GraphSnapshotId>(id => id.Value == missingFromId),
                Arg.Any<CancellationToken>())
            .Returns((GraphSnapshot?)null);

        _snapshotRepository.GetByIdAsync(
                Arg.Is<GraphSnapshotId>(id => id.Value == toSnapshot.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(toSnapshot);

        // Act
        var result = await _sut.Handle(
            new GetTemporalDiffFeature.Query(missingFromId, toSnapshot.Id.Value),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.GraphSnapshot.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_ToSnapshotIsMissing()
    {
        // Arrange — snapshot de destino não existe
        var fromSnapshot = GraphSnapshot.Create(
            "Pre-release",
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "[{\"id\":\"n1\"}]",
            "[]",
            nodeCount: 1,
            edgeCount: 0,
            "system");

        var missingToId = Guid.NewGuid();

        _snapshotRepository.GetByIdAsync(
                Arg.Is<GraphSnapshotId>(id => id.Value == fromSnapshot.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(fromSnapshot);

        _snapshotRepository.GetByIdAsync(
                Arg.Is<GraphSnapshotId>(id => id.Value == missingToId),
                Arg.Any<CancellationToken>())
            .Returns((GraphSnapshot?)null);

        // Act
        var result = await _sut.Handle(
            new GetTemporalDiffFeature.Query(fromSnapshot.Id.Value, missingToId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.GraphSnapshot.NotFound");
    }
}
