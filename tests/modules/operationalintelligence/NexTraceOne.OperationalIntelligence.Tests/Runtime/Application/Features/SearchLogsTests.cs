using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

using SearchLogsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.SearchLogs.SearchLogs;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application.Features;

/// <summary>
/// Testes unitários para SearchLogs — pesquisa de logs estruturados via Elasticsearch.
/// SaaS-07: Log Search UI.
/// </summary>
public sealed class SearchLogsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ILogSearchService _logSearchService = Substitute.For<ILogSearchService>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public SearchLogsTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _currentTenant.Id.Returns(TenantId);
        _currentTenant.IsActive.Returns(true);
    }

    private SearchLogsFeature.Handler CreateHandler() =>
        new(_logSearchService, _currentTenant, _clock);

    private static SearchLogsFeature.LogEntry MakeEntry(string id = "log-1") => new(
        id,
        DateTimeOffset.UtcNow,
        "info",
        "Test log message",
        "order-api",
        "production",
        new Dictionary<string, object?>());

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DefaultWindow_PassesLast1HourToService()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var query = new SearchLogsFeature.Query();
        await CreateHandler().Handle(query, CancellationToken.None);

        await _logSearchService.Received(1).SearchAsync(
            Arg.Is<LogSearchRequest>(r =>
                r.From == FixedNow.AddHours(-1) &&
                r.To == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Last6HoursWindow_PassesCorrectTimeRange()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var query = new SearchLogsFeature.Query(Window: SearchLogsFeature.TimeWindow.Last6Hours);
        await CreateHandler().Handle(query, CancellationToken.None);

        await _logSearchService.Received(1).SearchAsync(
            Arg.Is<LogSearchRequest>(r =>
                r.From == FixedNow.AddHours(-6) &&
                r.To == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Last7DaysWindow_PassesCorrectTimeRange()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var query = new SearchLogsFeature.Query(Window: SearchLogsFeature.TimeWindow.Last7Days);
        await CreateHandler().Handle(query, CancellationToken.None);

        await _logSearchService.Received(1).SearchAsync(
            Arg.Is<LogSearchRequest>(r =>
                r.From == FixedNow.AddDays(-7) &&
                r.To == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomWindow_PassesExplicitDatesToService()
    {
        var from = FixedNow.AddDays(-2);
        var to = FixedNow.AddDays(-1);

        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var query = new SearchLogsFeature.Query(
            Window: SearchLogsFeature.TimeWindow.Custom,
            From: from,
            To: to);

        await CreateHandler().Handle(query, CancellationToken.None);

        await _logSearchService.Received(1).SearchAsync(
            Arg.Is<LogSearchRequest>(r => r.From == from && r.To == to),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFilters_PassesAllFiltersToService()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var query = new SearchLogsFeature.Query(
            ServiceName: "payment-api",
            Severity: "error",
            Environment: "production",
            SearchText: "NullReferenceException");

        await CreateHandler().Handle(query, CancellationToken.None);

        await _logSearchService.Received(1).SearchAsync(
            Arg.Is<LogSearchRequest>(r =>
                r.ServiceName == "payment-api" &&
                r.Severity == "error" &&
                r.Environment == "production" &&
                r.SearchText == "NullReferenceException" &&
                r.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEntries_MapsToResponse()
    {
        var entries = new List<SearchLogsFeature.LogEntry> { MakeEntry("log-42") };

        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((entries, 1L));

        var result = await CreateHandler().Handle(new SearchLogsFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(1);
        result.Value.Entries[0].Id.Should().Be("log-42");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyResponse()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var result = await CreateHandler().Handle(new SearchLogsFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsPageAndPageSizeInResponse()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 100L));

        var query = new SearchLogsFeature.Query(Page: 3, PageSize: 25);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(3);
        result.Value.PageSize.Should().Be(25);
        result.Value.TotalCount.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ReturnsSearchWindowInResponse()
    {
        _logSearchService.SearchAsync(Arg.Any<LogSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((new List<SearchLogsFeature.LogEntry>(), 0L));

        var result = await CreateHandler().Handle(
            new SearchLogsFeature.Query(Window: SearchLogsFeature.TimeWindow.Last24Hours),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SearchFrom.Should().Be(FixedNow.AddHours(-24));
        result.Value.SearchTo.Should().Be(FixedNow);
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    public void Validator_InvalidPageSize_ReturnsError(int pageSize)
    {
        var validator = new SearchLogsFeature.Validator();
        var result = validator.Validate(new SearchLogsFeature.Query(PageSize: pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_PageLessThan1_ReturnsError()
    {
        var validator = new SearchLogsFeature.Validator();
        var result = validator.Validate(new SearchLogsFeature.Query(Page: 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ServiceNameTooLong_ReturnsError()
    {
        var validator = new SearchLogsFeature.Validator();
        var longName = new string('x', 201);
        var result = validator.Validate(new SearchLogsFeature.Query(ServiceName: longName));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_SearchTextTooLong_ReturnsError()
    {
        var validator = new SearchLogsFeature.Validator();
        var longText = new string('x', 1001);
        var result = validator.Validate(new SearchLogsFeature.Query(SearchText: longText));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_CustomWindowFromAfterTo_ReturnsError()
    {
        var validator = new SearchLogsFeature.Validator();
        var now = DateTimeOffset.UtcNow;

        var result = validator.Validate(new SearchLogsFeature.Query(
            Window: SearchLogsFeature.TimeWindow.Custom,
            From: now,
            To: now.AddHours(-1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new SearchLogsFeature.Validator();
        var result = validator.Validate(new SearchLogsFeature.Query(
            ServiceName: "checkout-api",
            Severity: "error",
            Environment: "production",
            SearchText: "timeout",
            Page: 1,
            PageSize: 50));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_CustomWindowValidDates_Passes()
    {
        var validator = new SearchLogsFeature.Validator();
        var now = DateTimeOffset.UtcNow;

        var result = validator.Validate(new SearchLogsFeature.Query(
            Window: SearchLogsFeature.TimeWindow.Custom,
            From: now.AddHours(-2),
            To: now));

        result.IsValid.Should().BeTrue();
    }
}
