using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.ExportAnalyticsData;
using NexTraceOne.ProductAnalytics.Application.Features.GetAdoptionFunnel;
using NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption;
using NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes para FEAT-01 (paginação em GetPersonaUsage, GetModuleAdoption, GetAdoptionFunnel)
/// e FEAT-04 (exportação de dados CSV/JSON via ExportAnalyticsData).
/// </summary>
public sealed class PaginationAndExportTests
{
    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    public PaginationAndExportTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _configService.ResolveEffectiveValueAsync(
            Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
    }

    // ──────────────────────────────────────────────────────────────
    // FEAT-01: GetModuleAdoption pagination
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModuleAdoption_DefaultPagination_ShouldReturnPage1()
    {
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.AiAssistant, 100, 10),
                new(ProductModule.ContractStudio, 80, 8)
            });
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(15);
        _repo.GetFeatureCountsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalCount.Should().Be(2);
        result.Value.TotalPages.Should().Be(1);
        result.Value.Modules.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetModuleAdoption_PageSize1_ShouldReturnOnlyFirstModule()
    {
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.AiAssistant, 100, 10),
                new(ProductModule.ContractStudio, 80, 8),
                new(ProductModule.ServiceCatalog, 60, 6)
            });
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(20);
        _repo.GetFeatureCountsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, null, Page: 1, PageSize: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(3);
        result.Value.Modules.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetModuleAdoption_Page2PageSize1_ShouldReturnSecondModule()
    {
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.AiAssistant, 200, 20),
                new(ProductModule.ContractStudio, 100, 10)
            });
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(30);
        _repo.GetFeatureCountsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, null, Page: 2, PageSize: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.Modules.Should().HaveCount(1);
        // second page after ordering by TotalActions desc (100 < 200 → ContractStudio is second)
        result.Value.Modules[0].Module.Should().Be(ProductModule.ContractStudio);
    }

    [Fact]
    public async Task GetModuleAdoption_EmptyResults_ShouldReturnZeroPaginationMetadata()
    {
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>());
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _repo.GetFeatureCountsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Modules.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // FEAT-01: GetPersonaUsage pagination
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPersonaUsage_DefaultPagination_ShouldReturnAllPersonas()
    {
        SetupPersonaRepo();

        var handler = new GetPersonaUsage.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetPersonaUsage.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetPersonaUsage_PageSize1_ShouldReturnSingleProfile()
    {
        SetupPersonaRepo();

        var handler = new GetPersonaUsage.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetPersonaUsage.Query(null, null, null, Page: 1, PageSize: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(3);
        result.Value.Profiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPersonaUsage_EmptyTenant_PaginationMetadataIsZero()
    {
        _repo.GetPersonaBreakdownAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<PersonaBreakdownRow>());

        var handler = new GetPersonaUsage.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetPersonaUsage.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.Profiles.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // FEAT-01: GetAdoptionFunnel pagination + config injection
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdoptionFunnel_DefaultPagination_ShouldReturnAllFunnels()
    {
        SetupFunnelRepo();

        var handler = new GetAdoptionFunnel.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetAdoptionFunnel.Query(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().BeGreaterThanOrEqualTo(1);
        result.Value.Funnels.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAdoptionFunnel_PageSize1_ShouldReturnOnlyOneFunnel()
    {
        SetupFunnelRepo();

        var handler = new GetAdoptionFunnel.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new GetAdoptionFunnel.Query(null, null, null, null, Page: 1, PageSize: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(1);
        result.Value.Funnels.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // FEAT-04: ExportAnalyticsData — CSV events
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportAnalyticsData_Events_CsvFormat_ShouldReturnCsvContent()
    {
        var sessionId = Guid.NewGuid().ToString();
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>
            {
                new(sessionId, AnalyticsEventType.SearchExecuted, FixedNow.AddMinutes(-5)),
                new(sessionId, AnalyticsEventType.EntityViewed, FixedNow.AddMinutes(-4))
            });

        var handler = new ExportAnalyticsData.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new ExportAnalyticsData.Query(
            ExportAnalyticsData.ExportDataType.Events,
            ExportAnalyticsData.ExportFormat.Csv,
            null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.FileName.Should().EndWith(".csv");
        result.Value.Content.Should().Contain("session_id,event_type,occurred_at");
        result.Value.Content.Should().Contain("SearchExecuted");
        result.Value.RowCount.Should().Be(2);
        result.Value.IsTruncated.Should().BeFalse();
    }

    [Fact]
    public async Task ExportAnalyticsData_Events_JsonFormat_ShouldReturnJsonContent()
    {
        var sessionId = Guid.NewGuid().ToString();
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>
            {
                new(sessionId, AnalyticsEventType.SearchExecuted, FixedNow.AddMinutes(-5))
            });

        var handler = new ExportAnalyticsData.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new ExportAnalyticsData.Query(
            ExportAnalyticsData.ExportDataType.Events,
            ExportAnalyticsData.ExportFormat.Json,
            null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/json");
        result.Value.FileName.Should().EndWith(".json");
        result.Value.Content.Should().StartWith("[");
        result.Value.Content.Should().Contain("sessionId");
        result.Value.Content.Should().Contain("SearchExecuted");
    }

    [Fact]
    public async Task ExportAnalyticsData_Events_EmptyResult_ShouldReturnEmptyContent()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());

        var handler = new ExportAnalyticsData.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new ExportAnalyticsData.Query(
            ExportAnalyticsData.ExportDataType.Events,
            ExportAnalyticsData.ExportFormat.Csv,
            null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RowCount.Should().Be(0);
        result.Value.Content.Should().Contain("session_id,event_type,occurred_at");
    }

    // ──────────────────────────────────────────────────────────────
    // FEAT-04: ExportAnalyticsData — Summary
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportAnalyticsData_Summary_CsvFormat_ShouldReturnCsvSummary()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(500L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(45);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>
            {
                new(ProductModule.AiAssistant, 200, 20)
            });
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(10L);

        var handler = new ExportAnalyticsData.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new ExportAnalyticsData.Query(
            ExportAnalyticsData.ExportDataType.Summary,
            ExportAnalyticsData.ExportFormat.Csv,
            null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.Content.Should().Contain("total_events,500");
        result.Value.Content.Should().Contain("unique_users,45");
    }

    [Fact]
    public async Task ExportAnalyticsData_Summary_JsonFormat_ShouldReturnJsonSummary()
    {
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(200L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(30);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        var handler = new ExportAnalyticsData.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new ExportAnalyticsData.Query(
            ExportAnalyticsData.ExportDataType.Summary,
            ExportAnalyticsData.ExportFormat.Json,
            null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/json");
        result.Value.Content.Should().Contain("totalEvents");
        result.Value.Content.Should().Contain("uniqueUsers");
        result.Value.Content.Should().Contain("topModules");
    }

    [Fact]
    public async Task ExportAnalyticsData_PeriodLabel_ShouldBeIncludedInFilename()
    {
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());

        var handler = new ExportAnalyticsData.Handler(_repo, _clock, _configService);
        var result = await handler.Handle(new ExportAnalyticsData.Query(
            ExportAnalyticsData.ExportDataType.Events,
            ExportAnalyticsData.ExportFormat.Csv,
            null, null, null, "last_7d"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Contain("last_7d");
        result.Value.PeriodLabel.Should().Be("last_7d");
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private void SetupPersonaRepo()
    {
        var personas = new List<PersonaBreakdownRow>
        {
            new("Engineer", 300, 30),
            new("TechLead", 200, 20),
            new("Architect", 100, 10)
        };

        _repo.GetPersonaBreakdownAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(personas);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _repo.GetTopEventTypesAsync(Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<EventTypeCountRow>());
        _repo.GetDistinctEventTypesAsync(Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<AnalyticsEventType>());
    }

    private void SetupFunnelRepo()
    {
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());
    }
}
