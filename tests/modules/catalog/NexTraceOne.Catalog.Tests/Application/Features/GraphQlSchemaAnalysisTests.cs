using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using AnalyzeGraphQlSchemaFeature = NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeGraphQlSchema.AnalyzeGraphQlSchema;
using DetectBreakingChangesFeature = NexTraceOne.Catalog.Application.Contracts.Features.DetectGraphQlBreakingChanges.DetectGraphQlBreakingChanges;
using GetHistoryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetGraphQlSchemaHistory.GetGraphQlSchemaHistory;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave G.3 — GraphQL Schema Analysis.
/// Cobre parsing de schema SDL, persistência de snapshots, diff e histórico.
/// </summary>
public sealed class GraphQlSchemaAnalysisTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private const string SimpleSchema = """
        type Query {
          user(id: ID!): User
          users: [User!]!
        }

        type Mutation {
          createUser(name: String!): User
        }

        type User {
          id: ID!
          name: String!
          email: String
        }

        type Product {
          id: ID!
          price: Float!
        }
        """;

    private const string ExtendedSchema = """
        type Query {
          user(id: ID!): User
          users: [User!]!
          product(id: ID!): Product
        }

        type Mutation {
          createUser(name: String!): User
          deleteUser(id: ID!): Boolean
        }

        type User {
          id: ID!
          name: String!
          email: String
          avatar: String
        }

        type Product {
          id: ID!
          price: Float!
        }
        """;

    private const string ReducedSchema = """
        type Query {
          user(id: ID!): User
        }

        type User {
          id: ID!
          name: String!
        }
        """;

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GraphQlSchemaSnapshot MakeSnapshot(string schemaContent, string version = "1.0.0")
    {
        // Trigger the analyze handler to produce a snapshot using a substitute repo
        var capturedSnapshots = new List<GraphQlSchemaSnapshot>();
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.When(r => r.Add(Arg.Any<GraphQlSchemaSnapshot>()))
            .Do(call => capturedSnapshots.Add(call.Arg<GraphQlSchemaSnapshot>()));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AnalyzeGraphQlSchemaFeature.Handler(repo, unitOfWork, CreateClock());
        handler.Handle(
            new AnalyzeGraphQlSchemaFeature.Command(ApiAssetId, version, schemaContent, TenantId),
            CancellationToken.None).GetAwaiter().GetResult();

        return capturedSnapshots.Single();
    }

    // ── AnalyzeGraphQlSchema ───────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeGraphQlSchema_Returns_Correct_Type_Count()
    {
        var capturedSnapshots = new List<GraphQlSchemaSnapshot>();
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.When(r => r.Add(Arg.Any<GraphQlSchemaSnapshot>()))
            .Do(call => capturedSnapshots.Add(call.Arg<GraphQlSchemaSnapshot>()));
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new AnalyzeGraphQlSchemaFeature.Handler(repo, unitOfWork, CreateClock());
        var result = await handler.Handle(
            new AnalyzeGraphQlSchemaFeature.Command(ApiAssetId, "1.0.0", SimpleSchema, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // User + Product (Query and Mutation are root operation types, excluded from data types)
        result.Value.TypeCount.Should().Be(2);
    }

    [Fact]
    public async Task AnalyzeGraphQlSchema_Detects_HasQueryType()
    {
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new AnalyzeGraphQlSchemaFeature.Handler(repo, unitOfWork, CreateClock());
        var result = await handler.Handle(
            new AnalyzeGraphQlSchemaFeature.Command(ApiAssetId, "1.0.0", SimpleSchema, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasQueryType.Should().BeTrue();
        result.Value.HasMutationType.Should().BeTrue();
        result.Value.HasSubscriptionType.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeGraphQlSchema_Counts_Operations()
    {
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new AnalyzeGraphQlSchemaFeature.Handler(repo, unitOfWork, CreateClock());
        var result = await handler.Handle(
            new AnalyzeGraphQlSchemaFeature.Command(ApiAssetId, "1.0.0", SimpleSchema, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // 2 queries (user, users) + 1 mutation (createUser)
        result.Value.OperationCount.Should().Be(3);
    }

    [Fact]
    public async Task AnalyzeGraphQlSchema_Persists_Snapshot()
    {
        var capturedSnapshots = new List<GraphQlSchemaSnapshot>();
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.When(r => r.Add(Arg.Any<GraphQlSchemaSnapshot>()))
            .Do(call => capturedSnapshots.Add(call.Arg<GraphQlSchemaSnapshot>()));
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new AnalyzeGraphQlSchemaFeature.Handler(repo, unitOfWork, CreateClock());
        await handler.Handle(
            new AnalyzeGraphQlSchemaFeature.Command(ApiAssetId, "1.0.0", SimpleSchema, TenantId),
            CancellationToken.None);

        capturedSnapshots.Should().HaveCount(1);
        capturedSnapshots[0].ContractVersion.Should().Be("1.0.0");
        capturedSnapshots[0].ApiAssetId.Should().Be(ApiAssetId);
        capturedSnapshots[0].TenantId.Should().Be(TenantId);
        capturedSnapshots[0].CapturedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task AnalyzeGraphQlSchema_Validator_Rejects_Empty_Schema()
    {
        var validator = new AnalyzeGraphQlSchemaFeature.Validator();
        var result = await validator.ValidateAsync(
            new AnalyzeGraphQlSchemaFeature.Command(ApiAssetId, "1.0.0", string.Empty, TenantId));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeGraphQlSchema_Validator_Rejects_Empty_ApiAssetId()
    {
        var validator = new AnalyzeGraphQlSchemaFeature.Validator();
        var result = await validator.ValidateAsync(
            new AnalyzeGraphQlSchemaFeature.Command(Guid.Empty, "1.0.0", SimpleSchema, TenantId));
        result.IsValid.Should().BeFalse();
    }

    // ── DetectGraphQlBreakingChanges ───────────────────────────────────────

    [Fact]
    public async Task DetectGraphQlBreakingChanges_Detects_Removed_Type()
    {
        var baseSnapshot = MakeSnapshot(SimpleSchema, "1.0.0");
        var targetSnapshot = MakeSnapshot(ReducedSchema, "2.0.0");
        var baseId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.GetByIdAsync(GraphQlSchemaSnapshotId.From(baseId), Arg.Any<CancellationToken>())
            .Returns(baseSnapshot);
        repo.GetByIdAsync(GraphQlSchemaSnapshotId.From(targetId), Arg.Any<CancellationToken>())
            .Returns(targetSnapshot);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseId, targetId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBreakingChanges.Should().BeTrue();
        result.Value.BreakingChanges.Should().Contain(c => c.ChangeType == "TypeRemoved" && c.Name == "Product");
    }

    [Fact]
    public async Task DetectGraphQlBreakingChanges_Detects_Removed_Operation()
    {
        var baseSnapshot = MakeSnapshot(SimpleSchema, "1.0.0");
        var targetSnapshot = MakeSnapshot(ReducedSchema, "2.0.0");
        var baseId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.GetByIdAsync(GraphQlSchemaSnapshotId.From(baseId), Arg.Any<CancellationToken>())
            .Returns(baseSnapshot);
        repo.GetByIdAsync(GraphQlSchemaSnapshotId.From(targetId), Arg.Any<CancellationToken>())
            .Returns(targetSnapshot);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseId, targetId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BreakingChanges.Should().Contain(c =>
            c.ChangeType == "OperationRemoved" && c.Name == "createUser" && c.ParentType == "Mutation");
    }

    [Fact]
    public async Task DetectGraphQlBreakingChanges_Detects_Added_Operations_As_NonBreaking()
    {
        var baseSnapshot = MakeSnapshot(SimpleSchema, "1.0.0");
        var targetSnapshot = MakeSnapshot(ExtendedSchema, "1.1.0");
        var baseId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.GetByIdAsync(GraphQlSchemaSnapshotId.From(baseId), Arg.Any<CancellationToken>())
            .Returns(baseSnapshot);
        repo.GetByIdAsync(GraphQlSchemaSnapshotId.From(targetId), Arg.Any<CancellationToken>())
            .Returns(targetSnapshot);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseId, targetId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBreakingChanges.Should().BeFalse();
        result.Value.NonBreakingChanges.Should().NotBeEmpty();
        result.Value.NonBreakingChanges.Should().Contain(c => c.ChangeType == "OperationAdded");
    }

    [Fact]
    public async Task DetectGraphQlBreakingChanges_Returns_NotFound_For_Missing_BaseSnapshot()
    {
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.GetByIdAsync(Arg.Any<GraphQlSchemaSnapshotId>(), Arg.Any<CancellationToken>())
            .Returns((GraphQlSchemaSnapshot?)null);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(Guid.NewGuid(), Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── GetGraphQlSchemaHistory ────────────────────────────────────────────

    [Fact]
    public async Task GetGraphQlSchemaHistory_Returns_Empty_When_No_Snapshots()
    {
        var repo = Substitute.For<IGraphQlSchemaSnapshotRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, TenantId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<GraphQlSchemaSnapshot>)[]);

        var handler = new GetHistoryFeature.Handler(repo);
        var result = await handler.Handle(
            new GetHistoryFeature.Query(ApiAssetId, TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(0);
        result.Value.Snapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGraphQlSchemaHistory_Validator_Rejects_Empty_ApiAssetId()
    {
        var validator = new GetHistoryFeature.Validator();
        var validationResult = await validator.ValidateAsync(new GetHistoryFeature.Query(Guid.Empty, TenantId));
        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetGraphQlSchemaHistory_Validator_Rejects_PageSize_Above_100()
    {
        var validator = new GetHistoryFeature.Validator();
        var validationResult = await validator.ValidateAsync(
            new GetHistoryFeature.Query(ApiAssetId, TenantId, PageSize: 101));
        validationResult.IsValid.Should().BeFalse();
    }
}
