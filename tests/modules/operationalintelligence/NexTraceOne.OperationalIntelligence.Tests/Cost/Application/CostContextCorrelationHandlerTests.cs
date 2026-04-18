using FluentAssertions;
using NSubstitute;
using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.EnrichCostRecordWithRelease;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByDomain;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByRelease;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByTeam;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Application;

/// <summary>
/// Testes unitários dos handlers introduzidos no P6.4:
/// GetCostRecordsByTeam, GetCostRecordsByDomain,
/// GetCostRecordsByRelease e EnrichCostRecordWithRelease.
/// Também testa a lógica de domínio AssignRelease/ClearRelease de CostRecord.
/// </summary>
public sealed class CostContextCorrelationHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid BatchId = Guid.NewGuid();
    private static readonly Guid ReleaseId = Guid.NewGuid();

    private static CostRecord MakeRecord(
        string serviceId,
        string? team = "team-a",
        string? domain = "commerce",
        string? env = "production",
        decimal cost = 100m) =>
        CostRecord.Create(BatchId, serviceId, $"{serviceId}-name", team, domain, env, "2026-03", cost, "USD", "AWS CUR", FixedNow).Value;

    // ── CostRecord domain: AssignRelease / ClearRelease ──────────────────────

    [Fact]
    public void AssignRelease_ValidReleaseId_SetsReleaseId()
    {
        var record = MakeRecord("svc-api");
        record.ReleaseId.Should().BeNull();

        record.AssignRelease(ReleaseId);

        record.ReleaseId.Should().Be(ReleaseId);
    }

    [Fact]
    public void AssignRelease_EmptyGuid_ThrowsArgumentException()
    {
        var record = MakeRecord("svc-api");
        var act = () => record.AssignRelease(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClearRelease_AfterAssign_SetsReleaseIdToNull()
    {
        var record = MakeRecord("svc-api");
        record.AssignRelease(ReleaseId);
        record.ClearRelease();
        record.ReleaseId.Should().BeNull();
    }

    // ── GetCostRecordsByTeam ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCostRecordsByTeam_WithRecords_ReturnsAggregatedTotal()
    {
        var r1 = MakeRecord("svc-order", team: "team-a", cost: 200m);
        var r2 = MakeRecord("svc-catalog", team: "team-a", cost: 150m);

        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByTeamAsync("team-a", "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { r1, r2 });

        var handler = new GetCostRecordsByTeam.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByTeam.Query("team-a", "2026-03"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Team.Should().Be("team-a");
        result.Value.TotalCost.Should().Be(350m);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.All(i => i.ReleaseId == null).Should().BeTrue();
    }

    [Fact]
    public async Task GetCostRecordsByTeam_RecordWithRelease_IncludesReleaseId()
    {
        var record = MakeRecord("svc-api", team: "team-b", cost: 500m);
        record.AssignRelease(ReleaseId);

        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByTeamAsync("team-b", null, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { record });

        var handler = new GetCostRecordsByTeam.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByTeam.Query("team-b"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].ReleaseId.Should().Be(ReleaseId);
    }

    [Fact]
    public async Task GetCostRecordsByTeam_WithNoRecords_ReturnsEmptyWithZeroTotal()
    {
        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByTeamAsync("team-unknown", null, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var handler = new GetCostRecordsByTeam.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByTeam.Query("team-unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(0m);
        result.Value.Items.Should().BeEmpty();
    }

    // ── GetCostRecordsByDomain ────────────────────────────────────────────────

    [Fact]
    public async Task GetCostRecordsByDomain_WithRecords_ReturnsAggregatedTotal()
    {
        var r1 = MakeRecord("svc-order", domain: "commerce", cost: 300m);
        var r2 = MakeRecord("svc-checkout", domain: "commerce", cost: 250m);

        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByDomainAsync("commerce", "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { r1, r2 });

        var handler = new GetCostRecordsByDomain.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByDomain.Query("commerce", "2026-03"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Domain.Should().Be("commerce");
        result.Value.TotalCost.Should().Be(550m);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCostRecordsByDomain_WithNoRecords_ReturnsEmpty()
    {
        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByDomainAsync("unknown-domain", null, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var handler = new GetCostRecordsByDomain.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByDomain.Query("unknown-domain"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(0m);
        result.Value.Items.Should().BeEmpty();
    }

    // ── GetCostRecordsByRelease ───────────────────────────────────────────────

    [Fact]
    public async Task GetCostRecordsByRelease_WithRecords_ReturnsTotal()
    {
        var r1 = MakeRecord("svc-api", cost: 400m);
        r1.AssignRelease(ReleaseId);
        var r2 = MakeRecord("svc-worker", cost: 100m);
        r2.AssignRelease(ReleaseId);

        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByReleaseAsync(ReleaseId, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { r1, r2 });

        var handler = new GetCostRecordsByRelease.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByRelease.Query(ReleaseId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(ReleaseId);
        result.Value.TotalCost.Should().Be(500m);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCostRecordsByRelease_WithNoRecords_ReturnsNotFound()
    {
        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByReleaseAsync(ReleaseId, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var handler = new GetCostRecordsByRelease.Handler(repo);
        var result = await handler.Handle(new GetCostRecordsByRelease.Query(ReleaseId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoRecordsForRelease");
    }

    // ── EnrichCostRecordWithRelease ───────────────────────────────────────────

    [Fact]
    public async Task EnrichCostRecordWithRelease_WithMatchingRecords_AssignsReleaseAndPersists()
    {
        var r1 = MakeRecord("svc-api", env: "production", cost: 100m);
        var r2 = MakeRecord("svc-api", env: "production", cost: 200m);

        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByServiceAsync("svc-api", "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { r1, r2 });

        var uow = Substitute.For<ICostIntelligenceUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new EnrichCostRecordWithRelease.Handler(repo, uow);
        var command = new EnrichCostRecordWithRelease.Command(ReleaseId, "svc-api", "production", "2026-03");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(ReleaseId);
        result.Value.EnrichedRecordCount.Should().Be(2);

        r1.ReleaseId.Should().Be(ReleaseId);
        r2.ReleaseId.Should().Be(ReleaseId);

        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichCostRecordWithRelease_WithNoMatchingEnvironment_ReturnsNotFound()
    {
        var r1 = MakeRecord("svc-api", env: "staging", cost: 100m);

        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByServiceAsync("svc-api", "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { r1 });

        var uow = Substitute.For<ICostIntelligenceUnitOfWork>();

        var handler = new EnrichCostRecordWithRelease.Handler(repo, uow);
        var command = new EnrichCostRecordWithRelease.Command(ReleaseId, "svc-api", "production", "2026-03");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichCostRecordWithRelease_WithNoRecordsAtAll_ReturnsNotFound()
    {
        var repo = Substitute.For<ICostRecordRepository>();
        repo.ListByServiceAsync("svc-unknown", "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var uow = Substitute.For<ICostIntelligenceUnitOfWork>();

        var handler = new EnrichCostRecordWithRelease.Handler(repo, uow);
        var command = new EnrichCostRecordWithRelease.Command(ReleaseId, "svc-unknown", "production", "2026-03");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
