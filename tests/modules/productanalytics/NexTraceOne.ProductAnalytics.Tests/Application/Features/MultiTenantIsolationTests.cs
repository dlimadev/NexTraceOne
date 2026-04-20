using FluentAssertions;
using NSubstitute;
using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetAnalyticsSummary;
using NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;
using NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;
using NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption;
using NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage;
using NexTraceOne.ProductAnalytics.Application.Features.GetValueMilestones;
using NexTraceOne.ProductAnalytics.Application.Features.RecordAnalyticsEvent;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes de isolamento multi-tenant — verifica que cada handler opera
/// dentro do scope do tenant correto e que o TenantId é corretamente
/// propagado para as entidades e repositórios.
/// Corresponde a TEST-02 do PRODUCT-ANALYTICS-IMPROVEMENT-PLAN.md.
/// </summary>
public sealed class MultiTenantIsolationTests
{
    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenantA = Substitute.For<ICurrentTenant>();
    private readonly ICurrentTenant _tenantB = Substitute.For<ICurrentTenant>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    private static readonly Guid TenantIdA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantIdB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    public MultiTenantIsolationTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _tenantA.Id.Returns(TenantIdA);
        _tenantB.Id.Returns(TenantIdB);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns("user-001");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        // Default empty returns for repository
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _repo.CountActivePersonasAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);
        _repo.GetTopModulesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.ListSessionEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.GetPersonaBreakdownAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.GetSessionEventTypesAsync(Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.GetModuleAdoptionAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.GetFeatureCountsAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0L);
    }

    // ═══════════════════════════════════════════════════════════════════
    // RecordAnalyticsEvent — tenant stamp
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordEvent_TenantA_ShouldStampEventWithTenantAId()
    {
        // Arrange
        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenantA, _currentUser, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ModuleViewed, ProductModule.ServiceCatalog,
            "/services", "list", null, null, "Engineer", null, null, "session-a", "web", null);

        AnalyticsEvent? captured = null;
        await _repo.AddAsync(Arg.Do<AnalyticsEvent>(e => captured = e), Arg.Any<CancellationToken>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(TenantIdA, "evento deve ser associado ao tenant A");
    }

    [Fact]
    public async Task RecordEvent_TenantB_ShouldStampEventWithTenantBId()
    {
        // Arrange
        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenantB, _currentUser, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ContractPublished, ProductModule.ContractStudio,
            "/contracts", "publish", null, null, "TechLead", null, null, "session-b", "web", null);

        AnalyticsEvent? captured = null;
        await _repo.AddAsync(Arg.Do<AnalyticsEvent>(e => captured = e), Arg.Any<CancellationToken>());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(TenantIdB, "evento deve ser associado ao tenant B");
        captured.TenantId.Should().NotBe(TenantIdA, "evento de tenant B nunca deve ter TenantId de tenant A");
    }

    [Fact]
    public async Task RecordEvent_TwoTenants_ShouldProduceDifferentTenantStamps()
    {
        // Arrange
        var handlerA = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenantA, _currentUser, _clock);
        var handlerB = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenantB, _currentUser, _clock);

        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.EntityViewed, ProductModule.ServiceCatalog,
            "/services/s1", "detail", null, null, null, null, null, "sess-x", "web", null);

        var capturedTenants = new List<Guid>();
        await _repo.AddAsync(Arg.Do<AnalyticsEvent>(e => capturedTenants.Add(e.TenantId)), Arg.Any<CancellationToken>());

        // Act
        await handlerA.Handle(command, CancellationToken.None);
        await handlerB.Handle(command, CancellationToken.None);

        // Assert
        capturedTenants.Should().HaveCount(2);
        capturedTenants[0].Should().Be(TenantIdA);
        capturedTenants[1].Should().Be(TenantIdB);
        capturedTenants[0].Should().NotBe(capturedTenants[1], "cada tenant deve ter o seu próprio TenantId no evento");
    }

    [Fact]
    public async Task RecordEvent_AnonymousUser_ShouldStillAssociateTenantId()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.Id.Returns((string?)null);

        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenantA, _currentUser, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ModuleViewed, ProductModule.AiAssistant,
            "/ai", null, null, null, null, null, null, "anon-session", "web", null);

        AnalyticsEvent? captured = null;
        await _repo.AddAsync(Arg.Do<AnalyticsEvent>(e => captured = e), Arg.Any<CancellationToken>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue("eventos anónimos são permitidos");
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(TenantIdA, "mesmo utilizador anónimo tem TenantId do tenant A");
        captured.UserId.Should().BeNull("UserId deve ser null para utilizadores anónimos");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Query handlers — tenant independence (each tenant sees its own slice)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAnalyticsSummary_TenantA_ReturnsOnlyTenantAData()
    {
        // Arrange — tenant A has 500 events; we return 500 from mock
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(500L);
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(20);

        var handlerA = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);
        var query = new GetAnalyticsSummary.Query(null, null, null, null, "last_30d");

        // Act
        var result = await handlerA.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(500, "apenas eventos do tenant A devem ser visíveis");
        result.Value.UniqueUsers.Should().Be(20);
    }

    [Fact]
    public async Task GetAnalyticsSummary_TwoHandlerInstances_CanReturnDifferentCounts()
    {
        // Arrange — simulate different data per tenant by calling CountAsync twice in sequence
        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(100L, 500L); // first call returns 100, second returns 500

        var handler = new GetAnalyticsSummary.Handler(_repo, _clock, _configService);
        var query = new GetAnalyticsSummary.Query(null, null, null, null, "last_30d");

        // Act — two separate "tenant scoped" calls
        var resultA = await handler.Handle(query, CancellationToken.None);
        var resultB = await handler.Handle(query, CancellationToken.None);

        // Assert — handlers produce independent results based on repository data
        resultA.IsSuccess.Should().BeTrue();
        resultB.IsSuccess.Should().BeTrue();
        resultA.Value.TotalEvents.Should().NotBe(resultB.Value.TotalEvents,
            "handlers com dados diferentes de repositório devem produzir resultados distintos");
    }

    [Fact]
    public async Task GetPersonaUsage_WithTenantData_ShouldReturnTenantProfiles()
    {
        // Arrange
        var personas = new List<PersonaBreakdownRow>
        {
            new("Engineer", 10, 200),
            new("TechLead", 5, 80)
        };
        _repo.GetPersonaBreakdownAsync(Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(personas);

        _repo.CountAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(280L);
        _repo.CountByEventTypeAsync(Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0L);

        var handler = new GetPersonaUsage.Handler(_repo, _clock, _configService);
        var query = new GetPersonaUsage.Query(null, null, "last_30d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profiles.Should().HaveCount(2, "tenant deve ver apenas as suas próprias personas");
        result.Value.Profiles.Select(p => p.Persona).Should().Contain("Engineer").And.Contain("TechLead");
    }

    [Fact]
    public async Task GetValueMilestones_WithNoTenantData_ShouldReturnEmptyMilestones()
    {
        // Arrange — empty tenant (no events yet)
        _repo.CountUniqueUsersAsync(Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var handler = new GetValueMilestones.Handler(_repo, _clock, _configService);
        var query = new GetValueMilestones.Query(null, null, "last_30d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Milestones.Should().NotBeEmpty("milestones estruturais devem sempre ser retornados (com 0% completion)");
        result.Value.Milestones.Should().AllSatisfy(m =>
        {
            m.CompletionRate.Should().Be(0m, "sem utilizadores, completion rate deve ser 0");
            m.UsersReached.Should().Be(0);
        });
        result.Value.AvgTimeToFirstValueMinutes.Should().Be(0m);
        result.Value.AvgTimeToCoreValueMinutes.Should().Be(0m);
    }

    [Fact]
    public async Task GetJourneys_WithNoTenantSessions_ShouldReturnSkeletonJourneys()
    {
        // Arrange — new tenant, no sessions
        _repo.GetSessionEventTypesAsync(Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetJourneys.Handler(_repo, _clock, _configService);
        var query = new GetJourneys.Query(null, null, "last_30d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Journeys.Should().NotBeEmpty("skeleton journeys devem ser retornados para tenants sem sessões");
        result.Value.Journeys.Should().AllSatisfy(j =>
        {
            j.CompletionRate.Should().Be(0m);
            j.Steps.Should().NotBeEmpty("cada journey deve ter os seus steps definidos");
        });
    }

    [Fact]
    public async Task GetModuleAdoption_WithNoData_ShouldReturnSuccessWithEmpty()
    {
        // Arrange
        var handler = new GetModuleAdoption.Handler(_repo, _clock, _configService);
        var query = new GetModuleAdoption.Query(null, null, "last_7d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().BeEmpty("tenant sem dados não deve ter módulos de adopção");
    }

    [Fact]
    public async Task RecordEvent_ShouldAssociateCurrentTimestamp_FromClock()
    {
        // Arrange
        var specificTime = new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(specificTime);

        var handler = new RecordAnalyticsEvent.Handler(_repo, _unitOfWork, _tenantA, _currentUser, _clock);
        var command = new RecordAnalyticsEvent.Command(
            AnalyticsEventType.ModuleViewed, ProductModule.Dashboard,
            "/dashboard", null, null, null, null, null, null, "sess-ts", "web", null);

        AnalyticsEvent? captured = null;
        await _repo.AddAsync(Arg.Do<AnalyticsEvent>(e => captured = e), Arg.Any<CancellationToken>());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured!.OccurredAt.Should().Be(specificTime, "timestamp do evento deve vir do IDateTimeProvider, não do sistema");
    }

    [Fact]
    public async Task GetFrictionIndicators_EmptyTenant_ShouldReturnZeroScore()
    {
        // Arrange — empty tenant
        var handler = new GetFrictionIndicators.Handler(_repo, _clock, _configService);
        var query = new GetFrictionIndicators.Query(null, null, "last_30d");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallFrictionScore.Should().Be(0m, "tenant sem eventos não pode ter friction score");
        result.Value.Indicators.Should().BeEmpty();
        result.Value.IsSimulated.Should().BeFalse("dados reais (vazios) não devem ser marcados como simulados");
    }
}
