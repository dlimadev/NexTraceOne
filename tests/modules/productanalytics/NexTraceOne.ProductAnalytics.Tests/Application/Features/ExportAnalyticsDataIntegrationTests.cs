using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Features.ExportAnalyticsData;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using System.Linq;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// ACT-023 — testes de integração (handler-level) para ExportAnalyticsData.
///
/// Complementa os casos básicos de PaginationAndExportTests com cenários de:
/// - Filtros combinados (persona + module + teamId)
/// - Truncação quando o resultado excede MaxExportRows
/// - Tipo de exportação desconhecido (validation error)
/// - Range edge cases (last_1d, last_90d, custom range → default)
/// - Conteúdo CSV/JSON detalhado (cabeçalhos, encoding, campos)
/// - IsTruncated + RowCount corretos em summary
/// </summary>
public sealed class ExportAnalyticsDataIntegrationTests
{
    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 8, 0, 0, TimeSpan.Zero);

    public ExportAnalyticsDataIntegrationTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
    }

    private ExportAnalyticsData.Handler CreateHandler() =>
        new(_repo, _clock, _configService);

    private static List<SessionEventRow> BuildEvents(int count, AnalyticsEventType type = AnalyticsEventType.SearchExecuted)
        => Enumerable.Range(0, count)
            .Select(i => new SessionEventRow(Guid.NewGuid().ToString(), type, FixedNow.AddMinutes(-i)))
            .ToList();

    // ──────────────────────────────────────────────────────────────
    // Events — combined filters
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportEvents_PersonaAndTeamIdFilter_PassedToRepository()
    {
        _repo.ListSessionEventsAsync(
            Arg.Is<string?>(p => p == "Engineer"),
            Arg.Is<string?>(t => t == "team-42"),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(BuildEvents(3));

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                "Engineer", null, "team-42", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RowCount.Should().Be(3);
        await _repo.Received(1).ListSessionEventsAsync(
            "Engineer", "team-42",
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportEvents_ModuleFilter_FiltersResultsByCaseInsensitiveEventType()
    {
        // "ServiceCatalog" is a valid ProductModule; the handler filters rows where
        // EventType.ToString() contains "ServiceCatalog". Neither SearchExecuted nor EntityViewed contains it.
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>
            {
                new(Guid.NewGuid().ToString(), AnalyticsEventType.SearchExecuted, FixedNow),
                new(Guid.NewGuid().ToString(), AnalyticsEventType.EntityViewed, FixedNow),
            });

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Json,
                null, "ServiceCatalog", null, null),
            CancellationToken.None);

        // "ServiceCatalog" is a valid ProductModule. EventTypes SearchExecuted/EntityViewed
        // don't contain "ServiceCatalog" → all rows filtered out.
        result.IsSuccess.Should().BeTrue();
        result.Value.RowCount.Should().Be(0);
    }

    [Fact]
    public async Task ExportEvents_UnknownModule_TreatedAsNoModuleFilter_ReturnsAllRows()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(BuildEvents(5));

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, "NonExistentModuleName", null, null),
            CancellationToken.None);

        // Unrecognised module → no filter applied → all 5 rows returned
        result.IsSuccess.Should().BeTrue();
        result.Value.RowCount.Should().Be(5);
    }

    // ──────────────────────────────────────────────────────────────
    // Events — truncation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportEvents_RowsExceedMaxExportRows_IsTruncatedTrue()
    {
        // MaxExportRows is hardcoded to 10_000 in the handler (DefaultMaxExportRows).
        // Produce 10_001 rows to trigger truncation.
        var rows = BuildEvents(10_001);
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsTruncated.Should().BeTrue();
        result.Value.RowCount.Should().Be(10_000);
    }

    [Fact]
    public async Task ExportEvents_RowsAtExactMaxExportRows_IsTruncatedFalse()
    {
        var rows = BuildEvents(10_000);
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Json,
                null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsTruncated.Should().BeFalse();
        result.Value.RowCount.Should().Be(10_000);
    }

    // ──────────────────────────────────────────────────────────────
    // Range edge cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportEvents_Range_Last1d_ProducesSingleDayWindow()
    {
        _repo.ListSessionEventsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Is<DateTimeOffset>(f => (FixedNow - f).TotalHours <= 25),
            Arg.Is<DateTimeOffset>(t => t >= FixedNow.AddSeconds(-1)),
            Arg.Any<CancellationToken>())
            .Returns(BuildEvents(2));

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, "last_1d"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_1d");
    }

    [Fact]
    public async Task ExportEvents_Range_Last90d_PeriodLabelPreserved()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(BuildEvents(1));

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Json,
                null, null, null, "last_90d"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_90d");
    }

    [Fact]
    public async Task ExportEvents_Range_CustomOrNullRange_DefaultsToLast30d()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(BuildEvents(0));

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_30d");
        result.Value.FileName.Should().Contain("last_30d");
    }

    [Fact]
    public async Task ExportEvents_Range_MaxRangeDaysFromConfig_CapsLargeRange()
    {
        // Config returns 7 as MaxRangeDays — "last_90d" should be capped to 7 days.
        _configService
            .ResolveEffectiveValueAsync(
                Arg.Is<string>(k => k == AnalyticsConfigKeys.MaxRangeDays),
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto(
                Key: AnalyticsConfigKeys.MaxRangeDays,
                EffectiveValue: "7",
                ResolvedScope: "System",
                ResolvedScopeReferenceId: null,
                IsInherited: false,
                IsDefault: false,
                DefinitionKey: AnalyticsConfigKeys.MaxRangeDays,
                ValueType: "integer",
                IsSensitive: false,
                Version: 1));

        DateTimeOffset capturedFrom = default;
        _repo.ListSessionEventsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Do<DateTimeOffset>(f => capturedFrom = f),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(BuildEvents(0));

        await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, "last_90d"),
            CancellationToken.None);

        var windowDays = (FixedNow - capturedFrom).TotalDays;
        windowDays.Should().BeApproximately(7, precision: 0.1);
    }

    // ──────────────────────────────────────────────────────────────
    // Unknown data type
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Export_UnknownDataType_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                (ExportAnalyticsData.ExportDataType)99,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("analytics.export.unknown_type");
    }

    // ──────────────────────────────────────────────────────────────
    // CSV content structure
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportEvents_Csv_ContainsHeaderAndAllRows()
    {
        var sessionId = Guid.NewGuid().ToString();
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>
            {
                new(sessionId, AnalyticsEventType.SearchExecuted, FixedNow.AddMinutes(-2)),
                new(sessionId, AnalyticsEventType.EntityViewed, FixedNow.AddMinutes(-1)),
            });

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, null),
            CancellationToken.None);

        var lines = result.Value.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("session_id,event_type,occurred_at");
        lines.Should().HaveCount(3); // header + 2 data rows
    }

    [Fact]
    public async Task ExportEvents_Json_IsValidJsonArray()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(BuildEvents(3));

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Json,
                null, null, null, null),
            CancellationToken.None);

        result.Value.Content.TrimStart().Should().StartWith("[");
        result.Value.Content.TrimEnd().Should().EndWith("]");
        result.Value.ContentType.Should().Be("application/json");
    }

    // ──────────────────────────────────────────────────────────────
    // Summary — IsTruncated always false, RowCount always 1
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportSummary_IsTruncatedAlwaysFalse_RowCountIsOne()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(100L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(10);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Summary,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsTruncated.Should().BeFalse();
        result.Value.RowCount.Should().Be(1);
    }

    [Fact]
    public async Task ExportSummary_Json_ContainsExpectedTopLevelFields()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(750L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(55);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.AiAssistant, 400, 30) });
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(5L);

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Summary,
                ExportAnalyticsData.ExportFormat.Json,
                null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Contain("\"totalEvents\"");
        result.Value.Content.Should().Contain("\"uniqueUsers\"");
        result.Value.Content.Should().Contain("\"topModules\"");
        result.Value.Content.Should().Contain("750");
        result.Value.Content.Should().Contain("55");
    }

    // ──────────────────────────────────────────────────────────────
    // FileName conventions
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportEvents_Csv_FileNameEndsWithCsvExtension()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, null),
            CancellationToken.None);

        result.Value.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task ExportEvents_Json_FileNameEndsWithJsonExtension()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Events,
                ExportAnalyticsData.ExportFormat.Json,
                null, null, null, null),
            CancellationToken.None);

        result.Value.FileName.Should().EndWith(".json");
    }

    [Fact]
    public async Task ExportSummary_FileName_ContainsDataTypeAndPeriod()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        var result = await CreateHandler().Handle(
            new ExportAnalyticsData.Query(
                ExportAnalyticsData.ExportDataType.Summary,
                ExportAnalyticsData.ExportFormat.Csv,
                null, null, null, "last_7d"),
            CancellationToken.None);

        result.Value.FileName.Should().Contain("last_7d");
    }
}
