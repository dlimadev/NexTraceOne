using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeDataContractSchema;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerInventory;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDataContractSchemaHistory;
using NexTraceOne.Catalog.Application.Contracts.Features.ProposeBreakingChange;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts;

/// <summary>
/// Testes unitários para CC-03 (DataContractSchema), CC-04 (ConsumerInventory) e CC-06 (BreakingChangeProposal).
/// </summary>
public sealed class DataContractFeaturesTests
{
    private readonly IDataContractSchemaRepository _schemaRepo = Substitute.For<IDataContractSchemaRepository>();
    private readonly IContractConsumerInventoryRepository _consumerRepo = Substitute.For<IContractConsumerInventoryRepository>();
    private readonly IBreakingChangeProposalRepository _proposalRepo = Substitute.For<IBreakingChangeProposalRepository>();
    private readonly IContractsUnitOfWork _uow = Substitute.For<IContractsUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly DateTimeOffset _now = new(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

    public DataContractFeaturesTests() => _clock.UtcNow.Returns(_now);

    // ── CC-03: AnalyzeDataContractSchema ────────────────────────────────────

    [Fact]
    public async Task AnalyzeDataContractSchema_ValidSchema_Persists()
    {
        var handler = new AnalyzeDataContractSchema.Handler(_schemaRepo, _uow, _clock);

        var schema = """[{"name":"id","type":"uuid","pii":"None"},{"name":"email","type":"varchar","pii":"High"}]""";
        var cmd = new AnalyzeDataContractSchema.Command(
            "tenant-1", Guid.NewGuid(), "team-platform", 24, schema, "postgres");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PiiClassification.Should().Be("High");
        result.Value.ColumnCount.Should().Be(2);
        await _schemaRepo.Received(1).AddAsync(Arg.Any<DataContractSchema>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeDataContractSchema_EmptySchema_ReturnsNoPii()
    {
        var handler = new AnalyzeDataContractSchema.Handler(_schemaRepo, _uow, _clock);

        var cmd = new AnalyzeDataContractSchema.Command(
            "tenant-1", Guid.NewGuid(), "team-x", 48, "[]", "clickhouse");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ColumnCount.Should().Be(0);
        result.Value.PiiClassification.Should().Be("None");
    }

    [Fact]
    public async Task AnalyzeDataContractSchema_InvalidJson_ReturnsSuccessWithZeroColumns()
    {
        var handler = new AnalyzeDataContractSchema.Handler(_schemaRepo, _uow, _clock);

        var cmd = new AnalyzeDataContractSchema.Command(
            "tenant-1", Guid.NewGuid(), "owner", 24, "not-json", "postgres");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ColumnCount.Should().Be(0);
    }

    // ── CC-03: GetDataContractSchemaHistory ──────────────────────────────────

    [Fact]
    public async Task GetDataContractSchemaHistory_ReturnsVersions()
    {
        var assetId = Guid.NewGuid();
        var snapshots = new List<DataContractSchema>
        {
            DataContractSchema.Create(assetId, "t1", "owner", 24,
                """[{"name":"id","type":"uuid","pii":"None"}]""", PiiClassification.None, "postgres", 1, 1, _now)
        };
        _schemaRepo.ListByApiAssetAsync(assetId, "t1", Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<DataContractSchema>)snapshots, snapshots.Count));

        var handler = new GetDataContractSchemaHistory.Handler(_schemaRepo);
        var result = await handler.Handle(new GetDataContractSchemaHistory.Query("t1", assetId, 1, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    // ── CC-04: GetContractConsumerInventory ─────────────────────────────────

    [Fact]
    public async Task GetContractConsumerInventory_ReturnsConsumers()
    {
        var contractId = Guid.NewGuid();
        var consumers = new List<ContractConsumerInventory>
        {
            ContractConsumerInventory.Create("t1", contractId, "svc-a", "production", "v1.2", 50.0, _now, _now)
        };
        _consumerRepo.ListByContractAsync(contractId, "t1", CancellationToken.None).Returns(consumers);

        var handler = new GetContractConsumerInventory.Handler(_consumerRepo);
        var result = await handler.Handle(new GetContractConsumerInventory.Query("t1", contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Consumers.Should().HaveCount(1);
        result.Value.Consumers[0].ConsumerService.Should().Be("svc-a");
    }

    [Fact]
    public async Task GetContractConsumerInventory_NoConsumers_ReturnsEmptyList()
    {
        _consumerRepo.ListByContractAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractConsumerInventory>());

        var handler = new GetContractConsumerInventory.Handler(_consumerRepo);
        var result = await handler.Handle(new GetContractConsumerInventory.Query("t1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Consumers.Should().BeEmpty();
    }

    // ── CC-06: ProposeBreakingChange ─────────────────────────────────────────

    [Fact]
    public async Task ProposeBreakingChange_ValidInput_CreatesProposal()
    {
        var handler = new ProposeBreakingChange.Handler(_proposalRepo, _consumerRepo, _uow, _clock);

        var cmd = new ProposeBreakingChange.Command(
            "tenant-1", Guid.NewGuid(),
            """[{"field":"userId","change":"renamed to user_id"}]""",
            30, "dev@example.com", true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProposalId.Should().NotBeEmpty();
        await _proposalRepo.Received(1).AddAsync(Arg.Any<BreakingChangeProposal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProposeBreakingChange_WithOpenConsultation_SetsStatusConsultationOpen()
    {
        var capturedProposal = (BreakingChangeProposal?)null;
        await _proposalRepo.AddAsync(
            Arg.Do<BreakingChangeProposal>(p => capturedProposal = p),
            Arg.Any<CancellationToken>());

        var handler = new ProposeBreakingChange.Handler(_proposalRepo, _consumerRepo, _uow, _clock);

        var cmd = new ProposeBreakingChange.Command(
            "tenant-1", Guid.NewGuid(), "[{}]", 14, "author", true);

        await handler.Handle(cmd, CancellationToken.None);

        capturedProposal.Should().NotBeNull();
        capturedProposal!.Status.Should().Be(BreakingChangeProposalStatus.ConsultationOpen);
    }

    // ── CC-06: Domain entity state machine ───────────────────────────────────

    [Fact]
    public void BreakingChangeProposal_Approve_SetsApprovedStatus()
    {
        var proposal = BreakingChangeProposal.Create("t1", Guid.NewGuid(), "[{}]", 30, "author", _now);
        proposal.OpenConsultation(_now);
        proposal.SubmitForApproval(_now);

        proposal.Approve("CAB decision: approved", _now);

        proposal.Status.Should().Be(BreakingChangeProposalStatus.Approved);
        proposal.DecidedAt.Should().Be(_now);
    }

    [Fact]
    public void BreakingChangeProposal_Reject_SetsRejectedStatus()
    {
        var proposal = BreakingChangeProposal.Create("t1", Guid.NewGuid(), "[{}]", 30, "author", _now);
        proposal.OpenConsultation(_now);
        proposal.SubmitForApproval(_now);

        proposal.Reject("Not backward compatible enough", _now);

        proposal.Status.Should().Be(BreakingChangeProposalStatus.Rejected);
    }

    // ── CC-03: DataContractSchema domain ─────────────────────────────────────

    [Fact]
    public void DataContractSchema_IsFreshnessViolated_WhenExpired()
    {
        var schema = DataContractSchema.Create(Guid.NewGuid(), "t1", "owner", 1,
            "[]", PiiClassification.None, "postgres", 0, 1, _now.AddHours(-2));

        schema.IsFreshnessViolated(schema.CapturedAt, _now).Should().BeTrue();
    }

    [Fact]
    public void DataContractSchema_IsNotFreshnessViolated_WhenFresh()
    {
        var schema = DataContractSchema.Create(Guid.NewGuid(), "t1", "owner", 24,
            "[]", PiiClassification.None, "postgres", 0, 1, _now);

        schema.IsFreshnessViolated(schema.CapturedAt, _now).Should().BeFalse();
    }
}
