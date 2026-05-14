using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.CreateJourneyDefinition;
using NexTraceOne.ProductAnalytics.Application.Features.DeleteJourneyDefinition;
using NexTraceOne.ProductAnalytics.Application.Features.GetCohortAnalysis;
using NexTraceOne.ProductAnalytics.Application.Features.GetJourneys;
using NexTraceOne.ProductAnalytics.Application.Features.ListJourneyDefinitions;
using NexTraceOne.ProductAnalytics.Application.Features.UpdateJourneyDefinition;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes para FEAT-03 (JourneyDefinitions configuráveis via DB) e FEAT-05 (Cohort Analysis).
/// </summary>
public sealed class JourneyConfigAndCohortTests
{
    private static readonly DateTimeOffset Now = new(2025, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");

    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IJourneyDefinitionRepository _journeyRepo = Substitute.For<IJourneyDefinitionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IConfigurationResolutionService _configService = Substitute.For<IConfigurationResolutionService>();

    public JourneyConfigAndCohortTests()
    {
        _clock.UtcNow.Returns(Now);
        _tenant.Id.Returns(TenantId);
        _configService
            .ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-03: ListJourneyDefinitions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListJourneyDefinitions_WithDefinitions_ShouldReturnDtos()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "my_journey", "My Journey", "[{}]", Now);
        _journeyRepo.ListActiveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<JourneyDefinition> { def });

        var handler = new ListJourneyDefinitions.Handler(_journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new ListJourneyDefinitions.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Definitions.Should().HaveCount(1);
        result.Value.Definitions[0].Key.Should().Be("my_journey");
        result.Value.Definitions[0].Name.Should().Be("My Journey");
    }

    [Fact]
    public async Task ListJourneyDefinitions_NoDefinitions_ShouldReturnEmpty()
    {
        // Arrange
        _journeyRepo.ListActiveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<JourneyDefinition>());

        var handler = new ListJourneyDefinitions.Handler(_journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new ListJourneyDefinitions.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Definitions.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-03: CreateJourneyDefinition
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateJourneyDefinition_NewKey_ShouldPersist()
    {
        // Arrange
        _journeyRepo.ExistsAsync("search_flow", TenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _tenant, _clock);
        var command = new CreateJourneyDefinition.Command(
            Key: "search_flow",
            Name: "Search Flow",
            StepsJson: """[{"stepId":"s1","stepName":"Start","eventType":"SearchExecuted"}]""");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("search_flow");
        result.Value.Name.Should().Be("Search Flow");
        await _journeyRepo.Received(1).AddAsync(Arg.Any<JourneyDefinition>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateJourneyDefinition_DuplicateKey_ShouldReturnConflict()
    {
        // Arrange
        _journeyRepo.ExistsAsync("existing_key", TenantId, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _tenant, _clock);
        var command = new CreateJourneyDefinition.Command("existing_key", "Existing", "[{}]");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("journey_definition.key_conflict");
        await _journeyRepo.DidNotReceive().AddAsync(Arg.Any<JourneyDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateJourneyDefinition_GlobalScope_ShouldUsNullTenant()
    {
        // Arrange
        _journeyRepo.ExistsAsync("global_journey", (Guid?)null, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _tenant, _clock);
        var command = new CreateJourneyDefinition.Command("global_journey", "Global Journey", "[{}]", IsGlobal: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _journeyRepo.Received(1).AddAsync(
            Arg.Is<JourneyDefinition>(d => d.TenantId == null),
            Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-03: UpdateJourneyDefinition
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateJourneyDefinition_ExistingDef_ShouldUpdate()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "my_journey", "Old Name", "[{}]", Now.AddDays(-1));
        _journeyRepo.GetByIdAsync(Arg.Any<JourneyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(def);

        var handler = new UpdateJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _clock);
        var command = new UpdateJourneyDefinition.Command(def.Id.Value, "New Name", "[{\"stepId\":\"s1\"}]", IsActive: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        def.Name.Should().Be("New Name");
        def.IsActive.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateJourneyDefinition_Deactivate_ShouldSetInactive()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "flow", "Flow", "[{}]", Now.AddDays(-1));
        _journeyRepo.GetByIdAsync(Arg.Any<JourneyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(def);

        var handler = new UpdateJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _clock);
        var command = new UpdateJourneyDefinition.Command(def.Id.Value, "Flow", "[{}]", IsActive: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        def.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateJourneyDefinition_NotFound_ShouldReturnNotFound()
    {
        // Arrange
        _journeyRepo.GetByIdAsync(Arg.Any<JourneyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns((JourneyDefinition?)null);

        var handler = new UpdateJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _clock);
        var command = new UpdateJourneyDefinition.Command(Guid.NewGuid(), "Name", "[{}]", true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("journey_definition.not_found");
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-03: DeleteJourneyDefinition
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteJourneyDefinition_TenantOwned_ShouldDelete()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "my_flow", "My Flow", "[{}]", Now);
        _journeyRepo.GetByIdAsync(Arg.Any<JourneyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(def);

        var handler = new DeleteJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _tenant);
        var command = new DeleteJourneyDefinition.Command(def.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("my_flow");
        _journeyRepo.Received(1).Remove(def);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteJourneyDefinition_GlobalDef_ShouldForbid()
    {
        // Arrange — global definition (TenantId = null)
        var def = JourneyDefinition.Create(null, "global_flow", "Global Flow", "[{}]", Now);
        _journeyRepo.GetByIdAsync(Arg.Any<JourneyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(def);

        var handler = new DeleteJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _tenant);
        var command = new DeleteJourneyDefinition.Command(def.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("journey_definition.global_delete_forbidden");
        _journeyRepo.DidNotReceive().Remove(Arg.Any<JourneyDefinition>());
    }

    [Fact]
    public async Task DeleteJourneyDefinition_OtherTenantDef_ShouldForbid()
    {
        // Arrange — definition owned by a different tenant
        var otherTenantId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");
        var def = JourneyDefinition.Create(otherTenantId, "other_flow", "Other Flow", "[{}]", Now);
        _journeyRepo.GetByIdAsync(Arg.Any<JourneyDefinitionId>(), Arg.Any<CancellationToken>())
            .Returns(def);

        var handler = new DeleteJourneyDefinition.Handler(_journeyRepo, _unitOfWork, _tenant);
        var command = new DeleteJourneyDefinition.Command(def.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("journey_definition.access_denied");
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-03: GetJourneys with DB definitions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetJourneys_WithDbDefinition_ShouldUseDbStepsInsteadOfStatic()
    {
        // Arrange — DB has one custom journey
        var stepsJson = JsonSerializer.Serialize(new[]
        {
            new { stepId = "step1", stepName = "First Step", eventType = "SearchExecuted" },
            new { stepId = "step2", stepName = "Second Step", eventType = "ContractDraftCreated" }
        });
        var dbDef = JourneyDefinition.Create(TenantId, "custom_journey", "Custom Journey", stepsJson, Now);
        _journeyRepo.ListActiveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<JourneyDefinition> { dbDef });

        // Sessions with both step events
        var sessionData = new List<SessionEventTypeRow>
        {
            new("sess-1", AnalyticsEventType.SearchExecuted, Now.AddHours(-2)),
            new("sess-1", AnalyticsEventType.ContractDraftCreated, Now.AddHours(-1)),
            new("sess-2", AnalyticsEventType.SearchExecuted, Now.AddMinutes(-30))
        };
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(sessionData);

        var handler = new GetJourneys.Handler(_repo, _clock, _configService, _journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, "last_30d"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var journeys = result.Value.Journeys;
        journeys.Should().HaveCount(1);
        journeys[0].JourneyId.Should().Be("custom_journey");
        journeys[0].Steps.Should().HaveCount(2);
        // 2 sessions started, 1 completed both steps → 50% completion
        journeys[0].CompletionRate.Should().Be(50m);
    }

    [Fact]
    public async Task GetJourneys_WithNoDbDefinitions_ShouldFallbackToStatic()
    {
        // Arrange — DB returns empty list → should use static defaults
        _journeyRepo.ListActiveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<JourneyDefinition>());

        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());

        var handler = new GetJourneys.Handler(_repo, _clock, _configService, _journeyRepo, _tenant);

        // Act
        var result = await handler.Handle(new GetJourneys.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 5 static journey definitions
        result.Value.Journeys.Should().HaveCount(5);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-05: GetCohortAnalysis
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCohortAnalysis_WithNoUsers_ShouldReturnEmptyCohorts()
    {
        // Arrange
        _repo.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserFirstEventRow>());

        var handler = new GetCohortAnalysis.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(new GetCohortAnalysis.Query(null, null, null, null, "last_90d"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Cohorts.Should().BeEmpty();
        result.Value.Granularity.Should().Be("week");
        result.Value.Metric.Should().Be("retention");
    }

    [Fact]
    public async Task GetCohortAnalysis_MonthGranularity_ShouldGroupByMonth()
    {
        // Arrange — two users in July, one in August
        var july = new DateTimeOffset(2025, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var aug = new DateTimeOffset(2025, 8, 5, 10, 0, 0, TimeSpan.Zero);
        var userEvents = new List<UserFirstEventRow>
        {
            new("user-1", AnalyticsEventType.SearchExecuted, july),
            new("user-2", AnalyticsEventType.SearchExecuted, july.AddDays(5)),
            new("user-3", AnalyticsEventType.SearchExecuted, aug)
        };

        _repo.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(userEvents);

        var handler = new GetCohortAnalysis.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(
            new GetCohortAnalysis.Query("month", 4, "retention", null, "last_90d"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Granularity.Should().Be("month");
        result.Value.Cohorts.Should().HaveCountGreaterThan(0);
        
        // Find the cohort that contains 2 users (July cohort)
        var julyCohort = result.Value.Cohorts.FirstOrDefault(c => c.CohortSize == 2);
        julyCohort.Should().NotBeNull();
        julyCohort!.CohortSize.Should().Be(2);
    }

    [Fact]
    public async Task GetCohortAnalysis_WeekGranularity_ShouldGroupByWeek()
    {
        // Arrange
        var monday = new DateTimeOffset(2025, 7, 7, 10, 0, 0, TimeSpan.Zero); // A Monday
        var userEvents = new List<UserFirstEventRow>
        {
            new("user-1", AnalyticsEventType.SearchExecuted, monday),
            new("user-2", AnalyticsEventType.SearchExecuted, monday.AddDays(2)),
            new("user-3", AnalyticsEventType.SearchExecuted, monday.AddDays(8)) // next week
        };

        _repo.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(userEvents);

        var handler = new GetCohortAnalysis.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(
            new GetCohortAnalysis.Query("week", 4, null, null, "last_30d"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Granularity.Should().Be("week");
        // First cohort should contain users in the same week
        result.Value.Cohorts[0].CohortSize.Should().Be(2);
    }

    [Fact]
    public async Task GetCohortAnalysis_ActivationMetric_ShouldUseActivationEvents()
    {
        // Arrange
        var userEvents = new List<UserFirstEventRow>
        {
            new("user-1", AnalyticsEventType.ModuleViewed, Now.AddDays(-10)),
            new("user-2", AnalyticsEventType.ModuleViewed, Now.AddDays(-10))
        };

        _repo.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(userEvents);

        var handler = new GetCohortAnalysis.Handler(_repo, _clock, _configService);

        // Act
        var result = await handler.Handle(
            new GetCohortAnalysis.Query("week", 2, "activation", null, "last_30d"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Metric.Should().Be("activation");
    }

    [Fact]
    public async Task GetCohortAnalysis_Periods_ShouldBeClampedToMax()
    {
        // Arrange
        _repo.GetUserFirstEventTimesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserFirstEventRow>());

        var handler = new GetCohortAnalysis.Handler(_repo, _clock, _configService);

        // Act — request 100 periods, should be clamped to 24
        var result = await handler.Handle(
            new GetCohortAnalysis.Query(null, 100, null, null, null),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Periods.Should().Be(24);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FEAT-03: JourneyDefinition domain entity
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void JourneyDefinition_Create_ShouldSetAllProperties()
    {
        // Arrange + Act
        var def = JourneyDefinition.Create(TenantId, "my_flow", "My Flow", "[{}]", Now);

        // Assert
        def.TenantId.Should().Be(TenantId);
        def.Key.Should().Be("my_flow");
        def.Name.Should().Be("My Flow");
        def.IsActive.Should().BeTrue();
        def.CreatedAt.Should().Be(Now);
        def.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void JourneyDefinition_Create_ShouldNormalizeKeyToLowercase()
    {
        // Act
        var def = JourneyDefinition.Create(null, "My_FLOW", "Flow", "[{}]", Now);

        // Assert
        def.Key.Should().Be("my_flow");
    }

    [Fact]
    public void JourneyDefinition_Deactivate_ShouldMarkInactive()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "flow", "Flow", "[{}]", Now.AddDays(-1));

        // Act
        def.Deactivate(Now);

        // Assert
        def.IsActive.Should().BeFalse();
        def.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void JourneyDefinition_Activate_AfterDeactivate_ShouldBeActive()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "flow", "Flow", "[{}]", Now.AddDays(-2));
        def.Deactivate(Now.AddDays(-1));

        // Act
        def.Activate(Now);

        // Assert
        def.IsActive.Should().BeTrue();
        def.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void JourneyDefinition_Update_ShouldChangeNameAndSteps()
    {
        // Arrange
        var def = JourneyDefinition.Create(TenantId, "flow", "Old Name", "[{}]", Now.AddDays(-1));

        // Act
        def.Update("New Name", "[{\"stepId\":\"s1\"}]", Now);

        // Assert
        def.Name.Should().Be("New Name");
        def.StepsJson.Should().Be("[{\"stepId\":\"s1\"}]");
        def.UpdatedAt.Should().Be(Now);
    }
}
