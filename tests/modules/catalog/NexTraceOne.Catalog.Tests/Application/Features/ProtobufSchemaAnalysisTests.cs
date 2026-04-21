using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using AnalyzeProtobufSchemaFeature = NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeProtobufSchema.AnalyzeProtobufSchema;
using DetectBreakingChangesFeature = NexTraceOne.Catalog.Application.Contracts.Features.DetectProtobufBreakingChanges.DetectProtobufBreakingChanges;
using GetHistoryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetProtobufSchemaHistory.GetProtobufSchemaHistory;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave H.1 — Protobuf Schema Analysis.
/// Cobre parsing de schema .proto, persistência de snapshots, diff e histórico.
/// </summary>
public sealed class ProtobufSchemaAnalysisTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ApiAssetId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private const string SimpleProto = """
        syntax = "proto3";

        message User {
          string id = 1;
          string name = 2;
          string email = 3;
        }

        message Product {
          string id = 1;
          float price = 2;
        }

        service UserService {
          rpc GetUser (User) returns (User);
          rpc CreateUser (User) returns (User);
        }
        """;

    private const string ExtendedProto = """
        syntax = "proto3";

        message User {
          string id = 1;
          string name = 2;
          string email = 3;
          string avatar = 4;
        }

        message Product {
          string id = 1;
          float price = 2;
          string description = 3;
        }

        message Order {
          string id = 1;
          string user_id = 2;
        }

        service UserService {
          rpc GetUser (User) returns (User);
          rpc CreateUser (User) returns (User);
          rpc DeleteUser (User) returns (User);
        }
        """;

    private const string ReducedProto = """
        syntax = "proto3";

        message User {
          string id = 1;
        }

        service UserService {
          rpc GetUser (User) returns (User);
        }
        """;

    // ── ParseSchema tests ──────────────────────────────────────────────────────

    [Fact]
    public void ParseSchema_SimpleProto_Counts_Messages_Correctly()
    {
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema(SimpleProto);
        analysis.MessageNames.Should().HaveCount(2);
        analysis.MessageNames.Should().Contain("User");
        analysis.MessageNames.Should().Contain("Product");
    }

    [Fact]
    public void ParseSchema_SimpleProto_Counts_Services_Correctly()
    {
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema(SimpleProto);
        analysis.RpcsByService.Should().ContainKey("UserService");
        analysis.RpcsByService["UserService"].Should().HaveCount(2);
        analysis.RpcsByService["UserService"].Should().Contain("GetUser");
        analysis.RpcsByService["UserService"].Should().Contain("CreateUser");
    }

    [Fact]
    public void ParseSchema_SimpleProto_Counts_Fields_Correctly()
    {
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema(SimpleProto);
        analysis.FieldsByMessage.Should().ContainKey("User");
        analysis.FieldsByMessage["User"].Should().HaveCount(3);
    }

    [Fact]
    public void ParseSchema_Detects_Proto3_Syntax()
    {
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema(SimpleProto);
        analysis.Syntax.Should().Be("proto3");
    }

    [Fact]
    public void ParseSchema_Detects_Proto2_Syntax()
    {
        const string proto2 = """
            syntax = "proto2";
            message Foo { optional string bar = 1; }
            """;
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema(proto2);
        analysis.Syntax.Should().Be("proto2");
    }

    [Fact]
    public void ParseSchema_EmptySchema_Returns_Zero_Counts()
    {
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema("// empty file\n");
        analysis.MessageNames.Should().BeEmpty();
        analysis.RpcsByService.Should().BeEmpty();
        analysis.TotalFieldCount.Should().Be(0);
    }

    // ── AnalyzeProtobufSchema command handler ──────────────────────────────────

    [Fact]
    public async Task AnalyzeProtobufSchema_Persists_Snapshot()
    {
        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        ProtobufSchemaSnapshot? captured = null;
        repo.When(r => r.Add(Arg.Any<ProtobufSchemaSnapshot>()))
            .Do(ci => captured = ci.Arg<ProtobufSchemaSnapshot>());

        var handler = new AnalyzeProtobufSchemaFeature.Handler(repo, uow, clock);
        var result = await handler.Handle(
            new AnalyzeProtobufSchemaFeature.Command(ApiAssetId, "1.0.0", SimpleProto, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.MessageCount.Should().Be(2);
        captured.ServiceCount.Should().Be(1);
        captured.RpcCount.Should().Be(2);
        captured.Syntax.Should().Be("proto3");
    }

    [Fact]
    public async Task AnalyzeProtobufSchema_Validator_Rejects_Empty_ApiAssetId()
    {
        var validator = new AnalyzeProtobufSchemaFeature.Validator();
        var result = await validator.ValidateAsync(
            new AnalyzeProtobufSchemaFeature.Command(Guid.Empty, "1.0.0", SimpleProto, TenantId));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeProtobufSchema_Validator_Rejects_Oversized_Schema()
    {
        var validator = new AnalyzeProtobufSchemaFeature.Validator();
        var bigSchema = new string('x', 263_000);
        var result = await validator.ValidateAsync(
            new AnalyzeProtobufSchemaFeature.Command(ApiAssetId, "1.0.0", bigSchema, TenantId));
        result.IsValid.Should().BeFalse();
    }

    // ── DetectProtobufBreakingChanges ──────────────────────────────────────────

    private static ProtobufSchemaSnapshot MakeSnapshot(string proto, string version)
    {
        var analysis = AnalyzeProtobufSchemaFeature.Handler.ParseSchema(proto);
        return ProtobufSchemaSnapshot.Create(
            apiAssetId: ApiAssetId,
            contractVersion: version,
            schemaContent: proto,
            messageCount: analysis.MessageNames.Count,
            fieldCount: analysis.TotalFieldCount,
            serviceCount: analysis.RpcsByService.Count,
            rpcCount: analysis.RpcsByService.Values.Sum(r => r.Count),
            messageNamesJson: System.Text.Json.JsonSerializer.Serialize(analysis.MessageNames),
            fieldsByMessageJson: System.Text.Json.JsonSerializer.Serialize(
                analysis.FieldsByMessage.ToDictionary(kv => kv.Key, kv => kv.Value.ToList())),
            rpcsByServiceJson: System.Text.Json.JsonSerializer.Serialize(
                analysis.RpcsByService.ToDictionary(kv => kv.Key, kv => kv.Value.ToList())),
            syntax: analysis.Syntax,
            tenantId: TenantId,
            capturedAt: FixedNow);
    }

    [Fact]
    public async Task DetectBreakingChanges_Returns_NotFound_For_Missing_Base()
    {
        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.GetByIdAsync(Arg.Any<ProtobufSchemaSnapshotId>(), Arg.Any<CancellationToken>())
            .Returns((ProtobufSchemaSnapshot?)null);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(Guid.NewGuid(), Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DetectBreakingChanges_Detects_Removed_Message_As_Breaking()
    {
        var baseSnap = MakeSnapshot(SimpleProto, "1.0.0");
        var targetSnap = MakeSnapshot(ReducedProto, "2.0.0");

        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(baseSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseSnap);
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(targetSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetSnap);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseSnap.Id.Value, targetSnap.Id.Value, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBreakingChanges.Should().BeTrue();
        result.Value.BreakingChanges.Should().Contain(c => c.ChangeType == "MessageRemoved" && c.Name == "Product");
    }

    [Fact]
    public async Task DetectBreakingChanges_Detects_Removed_Rpc_As_Breaking()
    {
        var baseSnap = MakeSnapshot(SimpleProto, "1.0.0");
        var targetSnap = MakeSnapshot(ReducedProto, "2.0.0");

        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(baseSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseSnap);
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(targetSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetSnap);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseSnap.Id.Value, targetSnap.Id.Value, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BreakingChanges.Should().Contain(c => c.ChangeType == "RpcRemoved" && c.Name == "CreateUser");
    }

    [Fact]
    public async Task DetectBreakingChanges_Marks_Added_Message_As_NonBreaking()
    {
        var baseSnap = MakeSnapshot(SimpleProto, "1.0.0");
        var targetSnap = MakeSnapshot(ExtendedProto, "1.1.0");

        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(baseSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseSnap);
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(targetSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetSnap);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseSnap.Id.Value, targetSnap.Id.Value, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBreakingChanges.Should().BeFalse();
        result.Value.NonBreakingChanges.Should().Contain(c => c.ChangeType == "MessageAdded" && c.Name == "Order");
    }

    [Fact]
    public async Task DetectBreakingChanges_Marks_Added_Rpc_As_NonBreaking()
    {
        var baseSnap = MakeSnapshot(SimpleProto, "1.0.0");
        var targetSnap = MakeSnapshot(ExtendedProto, "1.1.0");

        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(baseSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseSnap);
        repo.GetByIdAsync(ProtobufSchemaSnapshotId.From(targetSnap.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetSnap);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(baseSnap.Id.Value, targetSnap.Id.Value, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NonBreakingChanges.Should().Contain(c => c.ChangeType == "RpcAdded" && c.Name == "DeleteUser");
    }

    [Fact]
    public async Task DetectBreakingChanges_No_Changes_On_Identical_Snapshots()
    {
        var snap = MakeSnapshot(SimpleProto, "1.0.0");

        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.GetByIdAsync(Arg.Any<ProtobufSchemaSnapshotId>(), Arg.Any<CancellationToken>())
            .Returns(snap);

        var handler = new DetectBreakingChangesFeature.Handler(repo);
        var result = await handler.Handle(
            new DetectBreakingChangesFeature.Query(snap.Id.Value, snap.Id.Value, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasBreakingChanges.Should().BeFalse();
        result.Value.BreakingChangeCount.Should().Be(0);
        result.Value.NonBreakingChangeCount.Should().Be(0);
    }

    // ── GetProtobufSchemaHistory ───────────────────────────────────────────────

    [Fact]
    public async Task GetProtobufSchemaHistory_Returns_Empty_List_When_No_Snapshots()
    {
        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, TenantId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ProtobufSchemaSnapshot>)[]);

        var handler = new GetHistoryFeature.Handler(repo);
        var result = await handler.Handle(
            new GetHistoryFeature.Query(ApiAssetId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(0);
        result.Value.Snapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProtobufSchemaHistory_Returns_Snapshot_Summary()
    {
        var snap = MakeSnapshot(SimpleProto, "1.0.0");

        var repo = Substitute.For<IProtobufSchemaSnapshotRepository>();
        repo.ListByApiAssetAsync(ApiAssetId, TenantId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ProtobufSchemaSnapshot>)[snap]);

        var handler = new GetHistoryFeature.Handler(repo);
        var result = await handler.Handle(
            new GetHistoryFeature.Query(ApiAssetId, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
        result.Value.Snapshots.First().ContractVersion.Should().Be("1.0.0");
        result.Value.Snapshots.First().MessageCount.Should().Be(2);
        result.Value.Snapshots.First().Syntax.Should().Be("proto3");
    }

    [Fact]
    public async Task GetProtobufSchemaHistory_Validator_Rejects_Invalid_PageSize()
    {
        var validator = new GetHistoryFeature.Validator();
        var result = await validator.ValidateAsync(
            new GetHistoryFeature.Query(ApiAssetId, TenantId, Page: 1, PageSize: 200));
        result.IsValid.Should().BeFalse();
    }
}
