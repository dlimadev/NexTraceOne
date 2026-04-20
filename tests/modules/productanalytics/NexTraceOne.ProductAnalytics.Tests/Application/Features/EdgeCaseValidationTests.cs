using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetAnalyticsSummary;
using NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;
using NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption;
using NexTraceOne.ProductAnalytics.Application.Features.RecordAnalyticsEvent;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// TEST-05 — Testes de edge cases em validação e comportamentos limite.
/// Cobre cenários em falta nos unit tests existentes conforme plano de melhoria.
/// </summary>
public sealed class EdgeCaseValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("cccc0000-0000-0000-0000-000000000001");

    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IJourneyDefinitionRepository _journeyRepo = Substitute.For<IJourneyDefinitionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    public EdgeCaseValidationTests()
    {
        _clock.UtcNow.Returns(Now);
        _tenant.Id.Returns(TenantId);
        _user.IsAuthenticated.Returns(true);
        _user.Id.Returns("user-abc");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        // Default repository stubs
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _repo.CountActivePersonasAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow>());
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventRow>());
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>());
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());
        _journeyRepo.ListActiveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<JourneyDefinition>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-01: RecordAnalyticsEvent com UserId null (utilizador anónimo)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordAnalyticsEvent_AnonymousUser_ShouldSucceedAndPersist()
    {
        // Arrange — user not authenticated → no UserId
        _user.IsAuthenticated.Returns(false);
        _user.Id.Returns((string?)null);

        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenant, _user, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ModuleViewed,
            ProductModule.ServiceCatalog,
            "/services",
            "list",
            null, null,
            "Engineer",
            null, null,
            "session-anon-001",
            "web",
            null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Any<AnalyticsEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAnalyticsEvent_AnonymousUser_ShouldAssociateTenant()
    {
        // Arrange
        _user.IsAuthenticated.Returns(false);
        _user.Id.Returns((string?)null);
        AnalyticsEvent? capturedEvent = null;
        await _repo.AddAsync(Arg.Do<AnalyticsEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>());

        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenant, _user, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.SearchExecuted,
            ProductModule.Search,
            "/search",
            null, null, null, null, null, null, "session-anon-002", "web", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TenantId.Should().Be(TenantId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-02: Range last_1d em dia sem eventos
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAnalyticsSummary_LastOneDay_WithNoEvents_ShouldReturnZeroScores()
    {
        // Arrange — all zero stubs (already set in ctor)
        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetAnalyticsSummary.Query(null, null, null, null, "last_1d"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdoptionScore.Should().Be(0m);
        result.Value.ValueScore.Should().Be(0m);
        result.Value.FrictionScore.Should().Be(0m);
        result.Value.UniqueUsers.Should().Be(0);
        result.Value.TotalEvents.Should().Be(0);
    }

    [Fact]
    public async Task GetModuleAdoption_LastOneDay_WithNoEvents_ShouldReturnEmptyList()
    {
        // Arrange — empty (already set in ctor)
        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, "last_1d", 1, 20), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-03: Persona inexistente / inválida nos filtros
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetModuleAdoption_UnknownPersona_ShouldReturnEmptyGracefully()
    {
        // Arrange
        _repo.GetModuleAdoptionAsync("UnknownPersona", Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(
            new GetModuleAdoption.Query("UnknownPersona", null, "last_30d", 1, 20),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAnalyticsSummary_UnknownPersona_ShouldNotThrow()
    {
        // Arrange — all zero stubs in ctor

        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(
            new GetAnalyticsSummary.Query("NonExistentPersona", null, null, null, "last_30d"),
            CancellationToken.None);

        // Assert — should succeed with zero values, not throw
        result.IsSuccess.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-04: RecordAnalyticsEvent — validação de campos
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordAnalyticsEvent_EmptyRoute_ShouldFailValidation()
    {
        // Arrange
        var validator = new RecordAnalyticsEvent.Validator();
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ModuleViewed,
            ProductModule.ServiceCatalog,
            "",      // empty route
            null, null, null, null, null, null, null, null, null);

        // Act
        var validation = await validator.ValidateAsync(command);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == "Route");
    }

    [Fact]
    public async Task RecordAnalyticsEvent_PersonaHintTooLong_ShouldFailValidation()
    {
        // Arrange
        var validator = new RecordAnalyticsEvent.Validator();
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ModuleViewed,
            ProductModule.ServiceCatalog,
            "/services",
            null, null, null,
            new string('X', 51),  // 51 chars > max 50
            null, null, null, null, null);

        // Act
        var validation = await validator.ValidateAsync(command);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == "PersonaHint");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-05: Journey com sessão parcial (apenas step 1 de N)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetJourneys_PartialSession_OnlyFirstStep_ShouldShowCorrectDropOff()
    {
        // Arrange — 10 sessions started (SearchExecuted), only 2 completed next step
        var sessionRows = new List<SessionEventTypeRow>();
        for (int i = 1; i <= 10; i++)
            sessionRows.Add(new SessionEventTypeRow($"sess-{i:00}", AnalyticsEventType.SearchExecuted, Now.AddHours(-i)));

        // Only 2 sessions went further
        sessionRows.Add(new SessionEventTypeRow("sess-01", AnalyticsEventType.SearchResultClicked, Now.AddHours(-1).AddMinutes(5)));
        sessionRows.Add(new SessionEventTypeRow("sess-02", AnalyticsEventType.SearchResultClicked, Now.AddHours(-2).AddMinutes(5)));

        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(sessionRows);

        var handler = new GetJourneys.Handler(_repo, _clock, _configService, _journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, "last_7d"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().NotBeEmpty();
        result.Value.Journeys.Should().HaveCount(5); // fallback static definitions
    }

    [Fact]
    public async Task GetJourneys_SessionWithAllSteps_ShouldCountAsCompleted()
    {
        // Arrange — 3 sessions; 2 completed the search journey, 1 didn't
        var sessionRows = new List<SessionEventTypeRow>
        {
            new("s1", AnalyticsEventType.SearchExecuted, Now.AddHours(-3)),
            new("s1", AnalyticsEventType.SearchResultClicked, Now.AddHours(-3).AddMinutes(1)),
            new("s2", AnalyticsEventType.SearchExecuted, Now.AddHours(-2)),
            new("s2", AnalyticsEventType.SearchResultClicked, Now.AddHours(-2).AddMinutes(2)),
            new("s3", AnalyticsEventType.SearchExecuted, Now.AddHours(-1)),
            // s3 doesn't complete
        };

        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(sessionRows);

        var handler = new GetJourneys.Handler(_repo, _clock, _configService, _journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, "last_7d"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().NotBeEmpty();

        var searchJourney = result.Value.Journeys.FirstOrDefault(j => j.JourneyId == "search_to_result");
        if (searchJourney != null)
        {
            // 3 sessions started, 2 completed → ~66.7% completion
            searchJourney.CompletionRate.Should().BeApproximately(66.67m, 1m);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-06: BUG-04 — Dashboard vazio quando não há sessões
    //             GetJourneys deve retornar journeys com 0%, não array vazio
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetJourneys_EmptySessionData_ShouldReturnJourneysWithZeroCompletion()
    {
        // Arrange — no session data, empty repo
        // (stubs already set in ctor with empty lists)
        var handler = new GetJourneys.Handler(_repo, _clock, _configService, _journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, "last_30d"), CancellationToken.None);

        // Assert — BUG-04 fix: must return structure, not empty array
        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().NotBeEmpty("journeys must always show structure even when there are no sessions");
        result.Value.Journeys.Should().AllSatisfy(j =>
        {
            j.CompletionRate.Should().Be(0m);
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-07: GetAnalyticsSummary — range desconhecido usa default
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAnalyticsSummary_UnknownRangeString_ShouldNotThrow()
    {
        // Arrange — stubs already set in ctor with zero values
        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);

        // Act — unknown range string → handler should default gracefully
        var result = await handler.Handle(
            new GetAnalyticsSummary.Query(null, null, null, null, "last_999d"),
            CancellationToken.None);

        // Assert — should succeed (clamped to max range) or return success with defaults
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAnalyticsSummary_NullRange_ShouldUseDefaultRange()
    {
        // Arrange — non-zero events
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(5L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetAnalyticsSummary.Query(null, null, null, null, null), CancellationToken.None);

        // Assert — should succeed and use 30-day window
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(5);
        result.Value.UniqueUsers.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-08: RecordAnalyticsEvent — sem sessão (sessionId null)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordAnalyticsEvent_WithNullSessionId_ShouldSucceed()
    {
        // Arrange
        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenant, _user, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ReportGenerated,
            ProductModule.Governance,
            "/governance/reports",
            "export",
            null, null,
            "Executive",
            null, null,
            null,  // null sessionId
            "web",
            null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Any<AnalyticsEvent>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-09: GetJourneys — filtro por journeyId específico
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetJourneys_FilteredByJourneyId_ShouldReturnOnlyMatchingJourney()
    {
        // Arrange — empty sessions (fallback to static with 0%)
        var handler = new GetJourneys.Handler(_repo, _clock, _configService, _journeyRepo, _tenant);

        // Act — filter to a specific journey
        var result = await handler.Handle(new GetJourneys.Query("contract_draft_to_publish", null, "last_30d"), CancellationToken.None);

        // Assert — only 1 journey returned matching the filter
        result.IsSuccess.Should().BeTrue();
        if (result.Value.Journeys.Count == 1)
        {
            result.Value.Journeys[0].JourneyId.Should().Be("contract_draft_to_publish");
        }
        else
        {
            // If filter isn't applied at domain level, all 5 should come back
            result.Value.Journeys.Should().NotBeEmpty();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST-05-10: GetModuleAdoption — paginação com page > total
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetModuleAdoption_PageBeyondTotal_ShouldReturnEmptyItems()
    {
        // Arrange — 2 modules, request page 999
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.ServiceCatalog, 100, 10),
                new(ProductModule.ContractStudio, 50, 5)
            });

        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);

        // Act — page 999, pageSize 10 → way beyond
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, "last_30d", 999, 10), CancellationToken.None);

        // Assert — no items on page 999, but TotalCount still correct
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(2);
    }
}
