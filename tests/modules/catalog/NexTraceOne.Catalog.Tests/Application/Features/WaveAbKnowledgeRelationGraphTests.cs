using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Features.GetKnowledgeRelationGraph;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AB.1 — GetKnowledgeRelationGraph.
/// Cobre construção de nós, arestas, RelationStrength, filtro por âncora, truncagem e validador.
/// </summary>
public sealed class WaveAbKnowledgeRelationGraphTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ab1";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetKnowledgeRelationGraph.Handler CreateHandler(
        IKnowledgeRelationReader reader)
        => new(reader, CreateClock());

    // ── 1. Tenant sem relações devolve grafo vazio ────────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyGraph_ForEmptyTenant()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>([]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().BeEmpty();
        result.Value.Edges.Should().BeEmpty();
        result.Value.Summary.TotalNodes.Should().Be(0);
    }

    // ── 2. Nós de serviço são criados correctamente ───────────────────────

    [Fact]
    public async Task Handler_BuildsServiceNodes_FromRelations()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, [], [], [], [], [], null, null),
                new ServiceRelationEntry("svc-b", null, [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes
            .Where(n => n.NodeType == GetKnowledgeRelationGraph.KnowledgeNodeType.Service)
            .Should().HaveCount(2);
    }

    // ── 3. Arestas OwnedBy quando equipa está definida ───────────────────

    [Fact]
    public async Task Handler_BuildsOwnedByEdges_WhenTeamIsSet()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", "team-alpha", [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.Value.Edges
            .Should().Contain(e =>
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.OwnedBy
                && e.SourceId == "svc-a"
                && e.TargetId == "team-alpha");
    }

    // ── 4. Arestas DependsOn são construídas ─────────────────────────────

    [Fact]
    public async Task Handler_BuildsDependsOnEdges()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, ["svc-b"], [], [], [], [], null, null),
                new ServiceRelationEntry("svc-b", null, [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.Value.Edges
            .Should().Contain(e =>
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.DependsOn
                && e.SourceId == "svc-a"
                && e.TargetId == "svc-b");
    }

    // ── 5. Arestas PublishesContract são construídas ──────────────────────

    [Fact]
    public async Task Handler_BuildsPublishesContractEdges()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, [], ["api-v1"], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.Value.Edges
            .Should().Contain(e =>
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.PublishesContract
                && e.SourceId == "svc-a"
                && e.TargetId == "api-v1");
    }

    // ── 6. Arestas ConsumesContract são construídas ──────────────────────

    [Fact]
    public async Task Handler_BuildsConsumesContractEdges()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, [], [], ["ext-api-v1"], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.Value.Edges
            .Should().Contain(e =>
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.ConsumesContract
                && e.SourceId == "svc-a"
                && e.TargetId == "ext-api-v1");
    }

    // ── 7. Arestas MitigatedBy para runbooks ─────────────────────────────

    [Fact]
    public async Task Handler_BuildsMitigatedByEdges_ForRunbooks()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, [], [], [], ["runbook-1"], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.Value.Edges
            .Should().Contain(e =>
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.MitigatedBy
                && e.SourceId == "svc-a"
                && e.TargetId == "runbook-1");
    }

    // ── 8. RelationStrength é 1.0 para arestas estruturais ───────────────

    [Fact]
    public async Task Handler_RelationStrength_IsOne_ForStructuralEdges()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", "team-x", ["svc-b"], ["api-v1"], [], [], [], null, null),
                new ServiceRelationEntry("svc-b", null, [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        result.Value.Edges
            .Where(e =>
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.OwnedBy ||
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.DependsOn ||
                e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.PublishesContract)
            .Should().AllSatisfy(e => e.RelationStrength.Should().Be(1.0));
    }

    // ── 9. RelationStrength decresce com o tempo para arestas temporais ───

    [Fact]
    public async Task Handler_RelationStrength_Decays_ForTemporalEdges()
    {
        // Último incidente há 90 dias com decayDays=90 → força ~0.5
        var lastIncident = FixedNow.AddDays(-90);

        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, [], [], [], [], ["IncidentType1"],
                    null, lastIncident),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId, RelationStrengthDecayDays: 90),
            CancellationToken.None);

        var edge = result.Value.Edges
            .Single(e => e.EdgeType == GetKnowledgeRelationGraph.KnowledgeEdgeType.CorrelatedWith);

        // 1.0 / (1 + 90/90) = 0.5
        edge.RelationStrength.Should().BeApproximately(0.5, 0.01);
    }

    // ── 10. AnchorEntityId filtra até MaxDepth saltos ────────────────────

    [Fact]
    public async Task Handler_AnchorEntityId_FiltersToMaxDepthHops()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        // svc-a depende de svc-b que depende de svc-c (profundidade 2 a partir de svc-a)
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, ["svc-b"], [], [], [], [], null, null),
                new ServiceRelationEntry("svc-b", null, ["svc-c"], [], [], [], [], null, null),
                new ServiceRelationEntry("svc-c", null, ["svc-d"], [], [], [], [], null, null),
                new ServiceRelationEntry("svc-d", null, [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId, AnchorEntityId: "svc-a", MaxDepth: 2),
            CancellationToken.None);

        // Com profundidade 2 a partir de svc-a: svc-a, svc-b, svc-c (svc-d fica fora)
        result.Value.Nodes.Should().HaveCount(3);
        result.Value.Nodes.Select(n => n.Id)
            .Should().Contain(["svc-a", "svc-b", "svc-c"])
            .And.NotContain("svc-d");
    }

    // ── 11. MaxNodes trunca o grafo quando excedido ───────────────────────

    [Fact]
    public async Task Handler_MaxNodes_TruncatesGraph_WhenExceeded()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        // 10 serviços cada com uma dependência única
        var entries = Enumerable.Range(1, 10)
            .Select(i => new ServiceRelationEntry(
                $"svc-{i}", null, [$"dep-{i}"], [], [], [], [], null, null))
            .ToList();

        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(entries));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId, MaxNodes: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Count.Should().BeLessThanOrEqualTo(5);
    }

    // ── 12. GraphDensity é calculada correctamente ───────────────────────

    [Fact]
    public async Task Handler_GraphDensity_IsCorrectlyCalculated()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        // 2 nós com 1 aresta: density = 1 / (2*1) = 0.5
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", null, ["svc-b"], [], [], [], [], null, null),
                new ServiceRelationEntry("svc-b", null, [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        // 2 nós, 1 aresta: density = 1 / (2 * 1) = 0.5
        result.Value.Summary.GraphDensity.Should().Be(0.5);
    }

    // ── 13. Contagens do Summary são coerentes com nós e arestas ─────────

    [Fact]
    public async Task Handler_SummaryCounts_MatchNodesAndEdges()
    {
        var reader = Substitute.For<IKnowledgeRelationReader>();
        reader.ListServiceRelationsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceRelationEntry>>(
            [
                new ServiceRelationEntry("svc-a", "team-x", ["svc-b"], ["api-v1"],
                    [], ["runbook-1"], ["IncidentA"], null, null),
                new ServiceRelationEntry("svc-b", null, [], [], [], [], [], null, null),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetKnowledgeRelationGraph.Query(TenantId), CancellationToken.None);

        var summary = result.Value.Summary;
        summary.TotalNodes.Should().Be(result.Value.Nodes.Count);
        summary.TotalEdges.Should().Be(result.Value.Edges.Count);
        summary.ServiceCount.Should().Be(
            result.Value.Nodes.Count(n => n.NodeType == GetKnowledgeRelationGraph.KnowledgeNodeType.Service));
        summary.ContractCount.Should().Be(
            result.Value.Nodes.Count(n => n.NodeType == GetKnowledgeRelationGraph.KnowledgeNodeType.Contract));
    }

    // ── 14. Validador rejeita MaxDepth > 3 ───────────────────────────────

    [Fact]
    public void Validator_RejectsMaxDepth_GreaterThan3()
    {
        var validator = new GetKnowledgeRelationGraph.Validator();
        var query = new GetKnowledgeRelationGraph.Query(TenantId, MaxDepth: 4);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(query.MaxDepth));
    }
}
