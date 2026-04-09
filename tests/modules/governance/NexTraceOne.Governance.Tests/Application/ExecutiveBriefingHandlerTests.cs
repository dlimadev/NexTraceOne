using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GenerateExecutiveBriefing;
using NexTraceOne.Governance.Application.Features.GetExecutiveBriefing;
using NexTraceOne.Governance.Application.Features.ListExecutiveBriefings;
using NexTraceOne.Governance.Application.Features.PublishExecutiveBriefing;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application;

/// <summary>
/// Testes dos handlers de executive briefings.
/// Cobre GenerateExecutiveBriefing, GetExecutiveBriefing, ListExecutiveBriefings e PublishExecutiveBriefing.
/// </summary>
public sealed class ExecutiveBriefingHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);

    private readonly IExecutiveBriefingRepository _repository =
        Substitute.For<IExecutiveBriefingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ExecutiveBriefingHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    // ── GenerateExecutiveBriefing ──

    [Fact]
    public async Task Generate_ValidCommand_ShouldCreateDraftBriefing()
    {
        var handler = new GenerateExecutiveBriefing.Handler(_repository, _unitOfWork, _clock);
        var command = new GenerateExecutiveBriefing.Command(
            Title: "Weekly Platform Briefing",
            Frequency: BriefingFrequency.Weekly,
            PeriodStart: PeriodStart,
            PeriodEnd: PeriodEnd,
            ExecutiveSummary: "All systems operational.",
            PlatformStatusSection: "{\"status\":\"healthy\"}",
            TopIncidentsSection: "{\"incidents\":[]}",
            TeamPerformanceSection: "{\"teams\":[]}",
            HighRiskChangesSection: "{\"changes\":[]}",
            ComplianceStatusSection: "{\"compliance\":\"ok\"}",
            CostTrendsSection: "{\"trend\":\"stable\"}",
            ActiveRisksSection: "{\"risks\":[]}",
            GeneratedByAgent: "executive-briefing-agent",
            TenantId: "tenant1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Weekly Platform Briefing");
        result.Value.Status.Should().Be(BriefingStatus.Draft);
        result.Value.Frequency.Should().Be(BriefingFrequency.Weekly);
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.GeneratedByAgent.Should().Be("executive-briefing-agent");
        result.Value.BriefingId.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Any<ExecutiveBriefing>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_OnDemandFrequency_ShouldSucceed()
    {
        var handler = new GenerateExecutiveBriefing.Handler(_repository, _unitOfWork, _clock);
        var command = new GenerateExecutiveBriefing.Command(
            Title: "Ad-Hoc Incident Review",
            Frequency: BriefingFrequency.OnDemand,
            PeriodStart: PeriodStart,
            PeriodEnd: PeriodEnd,
            ExecutiveSummary: null,
            PlatformStatusSection: null,
            TopIncidentsSection: "{\"incidents\":[{\"id\":\"INC-42\"}]}",
            TeamPerformanceSection: null,
            HighRiskChangesSection: null,
            ComplianceStatusSection: null,
            CostTrendsSection: null,
            ActiveRisksSection: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Frequency.Should().Be(BriefingFrequency.OnDemand);
    }

    // ── GetExecutiveBriefing ──

    [Fact]
    public async Task Get_ExistingBriefing_ShouldReturnAllSections()
    {
        var briefing = CreateBriefing();
        _repository.GetByIdAsync(briefing.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ExecutiveBriefing?>(briefing));

        var handler = new GetExecutiveBriefing.Handler(_repository);
        var query = new GetExecutiveBriefing.Query(briefing.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BriefingId.Should().Be(briefing.Id.Value);
        result.Value.Title.Should().Be("Weekly Platform Briefing");
        result.Value.Status.Should().Be(BriefingStatus.Draft);
        result.Value.PlatformStatusSection.Should().Be("{\"status\":\"healthy\"}");
        result.Value.TopIncidentsSection.Should().Be("{\"incidents\":[]}");
        result.Value.TeamPerformanceSection.Should().Be("{\"teams\":[]}");
        result.Value.HighRiskChangesSection.Should().Be("{\"changes\":[]}");
        result.Value.ComplianceStatusSection.Should().Be("{\"compliance\":\"ok\"}");
        result.Value.CostTrendsSection.Should().Be("{\"trend\":\"stable\"}");
        result.Value.ActiveRisksSection.Should().Be("{\"risks\":[]}");
    }

    [Fact]
    public async Task Get_NonExistentBriefing_ShouldReturnNotFoundError()
    {
        var unknownId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<ExecutiveBriefingId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ExecutiveBriefing?>(null));

        var handler = new GetExecutiveBriefing.Handler(_repository);
        var query = new GetExecutiveBriefing.Query(unknownId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── ListExecutiveBriefings ──

    [Fact]
    public async Task List_NoFilters_ShouldReturnAllBriefings()
    {
        var briefings = new List<ExecutiveBriefing> { CreateBriefing(), CreateBriefing() };
        _repository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExecutiveBriefing>>(briefings));

        var handler = new ListExecutiveBriefings.Handler(_repository);
        var query = new ListExecutiveBriefings.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.FilteredFrequency.Should().BeNull();
        result.Value.FilteredStatus.Should().BeNull();
    }

    [Fact]
    public async Task List_FilterByFrequency_ShouldPassFilterToRepository()
    {
        _repository.ListAsync(BriefingFrequency.Weekly, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExecutiveBriefing>>(new List<ExecutiveBriefing> { CreateBriefing() }));

        var handler = new ListExecutiveBriefings.Handler(_repository);
        var query = new ListExecutiveBriefings.Query(Frequency: BriefingFrequency.Weekly);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.FilteredFrequency.Should().Be(BriefingFrequency.Weekly);
    }

    [Fact]
    public async Task List_FilterByStatus_ShouldPassFilterToRepository()
    {
        _repository.ListAsync(null, BriefingStatus.Published, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExecutiveBriefing>>(new List<ExecutiveBriefing>()));

        var handler = new ListExecutiveBriefings.Handler(_repository);
        var query = new ListExecutiveBriefings.Query(Status: BriefingStatus.Published);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.FilteredStatus.Should().Be(BriefingStatus.Published);
    }

    [Fact]
    public async Task List_ItemDto_ShouldMapCorrectFields()
    {
        var briefing = CreateBriefing();
        _repository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExecutiveBriefing>>(new List<ExecutiveBriefing> { briefing }));

        var handler = new ListExecutiveBriefings.Handler(_repository);
        var result = await handler.Handle(new ListExecutiveBriefings.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.BriefingId.Should().Be(briefing.Id.Value);
        item.Title.Should().Be("Weekly Platform Briefing");
        item.Status.Should().Be(BriefingStatus.Draft);
        item.Frequency.Should().Be(BriefingFrequency.Weekly);
        item.GeneratedByAgent.Should().Be("executive-briefing-agent");
        item.PublishedAt.Should().BeNull();
        item.ArchivedAt.Should().BeNull();
    }

    // ── PublishExecutiveBriefing ──

    [Fact]
    public async Task Publish_DraftBriefing_ShouldSucceed()
    {
        var briefing = CreateBriefing();
        _repository.GetByIdAsync(briefing.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ExecutiveBriefing?>(briefing));

        var handler = new PublishExecutiveBriefing.Handler(_repository, _unitOfWork, _clock);
        var command = new PublishExecutiveBriefing.Command(briefing.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _repository.Received(1).UpdateAsync(briefing, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_NonExistentBriefing_ShouldReturnNotFoundError()
    {
        _repository.GetByIdAsync(Arg.Any<ExecutiveBriefingId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ExecutiveBriefing?>(null));

        var handler = new PublishExecutiveBriefing.Handler(_repository, _unitOfWork, _clock);
        var command = new PublishExecutiveBriefing.Command(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Publish_AlreadyPublished_ShouldReturnTransitionError()
    {
        var briefing = CreateBriefing();
        briefing.Publish(FixedNow.AddMinutes(10));
        _repository.GetByIdAsync(briefing.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ExecutiveBriefing?>(briefing));

        var handler = new PublishExecutiveBriefing.Handler(_repository, _unitOfWork, _clock);
        var command = new PublishExecutiveBriefing.Command(briefing.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    // ── Helper ──

    private static ExecutiveBriefing CreateBriefing() => ExecutiveBriefing.Generate(
        title: "Weekly Platform Briefing",
        frequency: BriefingFrequency.Weekly,
        periodStart: PeriodStart,
        periodEnd: PeriodEnd,
        executiveSummary: "All systems operational.",
        platformStatusSection: "{\"status\":\"healthy\"}",
        topIncidentsSection: "{\"incidents\":[]}",
        teamPerformanceSection: "{\"teams\":[]}",
        highRiskChangesSection: "{\"changes\":[]}",
        complianceStatusSection: "{\"compliance\":\"ok\"}",
        costTrendsSection: "{\"trend\":\"stable\"}",
        activeRisksSection: "{\"risks\":[]}",
        generatedByAgent: "executive-briefing-agent",
        tenantId: "tenant1",
        now: FixedNow);
}
