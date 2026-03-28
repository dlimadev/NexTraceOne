using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateIncidentWithChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetCorrelatedChanges;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para o motor de correlação dinâmica incidente↔mudança.
/// Verifica os critérios de scoring, janela temporal, deduplicação e handlers.
/// </summary>
public sealed class DynamicCorrelationEngineTests
{
    // ── CorrelateIncidentWithChanges.Validator ────────────────────────────

    [Fact]
    public void Validator_WhenValidCommand_ShouldPass()
    {
        var validator = new CorrelateIncidentWithChanges.Validator();
        var command = new CorrelateIncidentWithChanges.Command(Guid.NewGuid(), 24);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenEmptyIncidentId_ShouldFail()
    {
        var validator = new CorrelateIncidentWithChanges.Validator();
        var command = new CorrelateIncidentWithChanges.Command(Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IncidentId");
    }

    [Fact]
    public void Validator_WhenTimeWindowOutOfRange_ShouldFail()
    {
        var validator = new CorrelateIncidentWithChanges.Validator();
        var command = new CorrelateIncidentWithChanges.Command(Guid.NewGuid(), 0);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TimeWindowHours");
    }

    [Fact]
    public void Validator_WhenTimeWindowExceedsMax_ShouldFail()
    {
        var validator = new CorrelateIncidentWithChanges.Validator();
        var command = new CorrelateIncidentWithChanges.Command(Guid.NewGuid(), 200);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    // ── CorrelateIncidentWithChanges.Handler ──────────────────────────────

    [Fact]
    public async Task Handler_WhenIncidentNotFound_ShouldReturnFailure()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        store.GetIncidentCorrelationContext(Arg.Any<string>()).Returns((IncidentCorrelationContext?)null);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(Guid.NewGuid(), null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handler_WhenNoReleasesInWindow_ShouldReturnEmptyCorrelations()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(incidentId, "svc-api", "API Service", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Correlations.Should().BeEmpty();
        result.Value.NewCorrelations.Should().Be(0);
        await repo.DidNotReceive().AddAsync(Arg.Any<IncidentChangeCorrelation>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddRangeAsync(Arg.Any<IReadOnlyList<IncidentChangeCorrelation>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_WhenReleaseMatchesExactServiceName_ShouldProduceHighConfidence()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId, "payment-gateway", "Payment Gateway", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([new ChangeReleaseDto(
                changeId, Guid.NewGuid(),
                "payment-gateway",   // exact service match
                "Production", "Deploy v2.0", now.AddHours(-2), null)]);

        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, 24);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewCorrelations.Should().Be(1);
        result.Value.Correlations.Should().HaveCount(1);
        result.Value.Correlations[0].ConfidenceLevel.Should().Be(CorrelationConfidenceLevel.High);
        result.Value.Correlations[0].MatchType.Should().Be(CorrelationMatchType.ExactServiceMatch);
        await repo.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyList<IncidentChangeCorrelation>>(l => l.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_WhenReleasePartiallyMatchesServiceName_ShouldProduceMediumConfidence()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId, "payment", "Payment", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([new ChangeReleaseDto(
                changeId, Guid.NewGuid(),
                "payment-gateway",   // partial match — contains "payment"
                "Production", "Deploy v2.0", now.AddHours(-2), null)]);

        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, 24);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Correlations[0].ConfidenceLevel.Should().Be(CorrelationConfidenceLevel.Medium);
        result.Value.Correlations[0].MatchType.Should().Be(CorrelationMatchType.DependencyMatch);
    }

    [Fact]
    public async Task Handler_WhenReleaseHasNoServiceMatch_ShouldProduceLowConfidence()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId, "svc-catalog", "Catalog Service", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([new ChangeReleaseDto(
                changeId, Guid.NewGuid(),
                "unrelated-service",   // no service match — time proximity only
                "Production", "Deploy v5.0", now.AddHours(-1), null)]);

        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, 24);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Correlations[0].ConfidenceLevel.Should().Be(CorrelationConfidenceLevel.Low);
        result.Value.Correlations[0].MatchType.Should().Be(CorrelationMatchType.TimeProximity);
    }

    [Fact]
    public async Task Handler_WhenDuplicateCorrelationExists_ShouldSkipPersistenceForDuplicate()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId, "svc-payment", "Payment Service", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([new ChangeReleaseDto(
                changeId, Guid.NewGuid(), "svc-payment", "Production", "Deploy", now.AddHours(-1), null)]);

        // Mark the correlation as already existing (duplicate)
        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, 24);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCandidates.Should().Be(1);
        result.Value.NewCorrelations.Should().Be(0);     // No new correlations
        result.Value.Correlations[0].IsDuplicate.Should().BeTrue();
        await repo.DidNotReceive().AddRangeAsync(Arg.Any<IReadOnlyList<IncidentChangeCorrelation>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_WhenTimeWindowIsCustom_ShouldPassItToReader()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var customWindowHours = 48;

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(incidentId, "svc-x", "Service X", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, customWindowHours);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TimeWindowHours.Should().Be(customWindowHours);

        // Verify the reader was called with correct time window bounds
        await reader.Received(1).GetReleasesInWindowAsync(
            "Production",
            Arg.Is<DateTimeOffset>(f => Math.Abs((f - now.AddHours(-customWindowHours)).TotalSeconds) < 1),
            Arg.Is<DateTimeOffset>(t => Math.Abs((t - now).TotalSeconds) < 1),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_WhenMultipleReleasesPresent_ShouldPersistAllNonDuplicates()
    {
        var store = Substitute.For<IIncidentStore>();
        var reader = Substitute.For<IChangeIntelligenceReader>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var changeId1 = Guid.NewGuid();
        var changeId2 = Guid.NewGuid();
        var changeId3 = Guid.NewGuid();

        clock.UtcNow.Returns(now);
        tenant.IsActive.Returns(false);

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(incidentId, "svc-payments", "Payments", "Production", now));

        reader.GetReleasesInWindowAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([
                new ChangeReleaseDto(changeId1, Guid.NewGuid(), "svc-payments", "Production", "Deploy v1", now.AddHours(-1), null),
                new ChangeReleaseDto(changeId2, Guid.NewGuid(), "unrelated-svc", "Production", "Deploy v2", now.AddHours(-2), null),
                new ChangeReleaseDto(changeId3, Guid.NewGuid(), "another-svc", "Production", "Deploy v3", now.AddHours(-3), null)
            ]);

        // changeId1 already exists (duplicate), others are new
        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId1, Arg.Any<CancellationToken>()).Returns(true);
        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId2, Arg.Any<CancellationToken>()).Returns(false);
        repo.ExistsByIncidentAndChangeAsync(incidentId, changeId3, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CorrelateIncidentWithChanges.Handler(store, reader, repo, tenant, clock);
        var command = new CorrelateIncidentWithChanges.Command(incidentId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCandidates.Should().Be(3);
        result.Value.NewCorrelations.Should().Be(2);
        await repo.Received(1).AddRangeAsync(
            Arg.Is<IReadOnlyList<IncidentChangeCorrelation>>(l => l.Count == 2),
            Arg.Any<CancellationToken>());
    }

    // ── GetCorrelatedChanges.Handler ──────────────────────────────────────

    [Fact]
    public async Task GetCorrelatedChanges_WhenIncidentNotFound_ShouldReturnFailure()
    {
        var store = Substitute.For<IIncidentStore>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();

        store.IncidentExists(Arg.Any<string>()).Returns(false);

        var handler = new GetCorrelatedChanges.Handler(store, repo);
        var query = new GetCorrelatedChanges.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetCorrelatedChanges_WhenCorrelationsExist_ShouldReturnAll()
    {
        var store = Substitute.For<IIncidentStore>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.IncidentExists(incidentId.ToString()).Returns(true);

        var correlation1 = IncidentChangeCorrelation.Create(
            incidentId, Guid.NewGuid(), Guid.NewGuid(),
            CorrelationConfidenceLevel.High, CorrelationMatchType.ExactServiceMatch,
            24, now, null, "SvcA", "Deploy v1", "Production", now.AddHours(-2));

        var correlation2 = IncidentChangeCorrelation.Create(
            incidentId, Guid.NewGuid(), Guid.NewGuid(),
            CorrelationConfidenceLevel.Low, CorrelationMatchType.TimeProximity,
            24, now, null, "SvcB", "Deploy v2", "Production", now.AddHours(-5));

        repo.GetByIncidentIdAsync(incidentId, Arg.Any<CancellationToken>())
            .Returns([correlation1, correlation2]);

        var handler = new GetCorrelatedChanges.Handler(store, repo);
        var query = new GetCorrelatedChanges.Query(incidentId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCorrelations.Should().Be(2);
        result.Value.Correlations.Should().HaveCount(2);
        result.Value.Correlations.Should().Contain(c => c.ConfidenceLevel == CorrelationConfidenceLevel.High);
        result.Value.Correlations.Should().Contain(c => c.MatchType == CorrelationMatchType.TimeProximity);
    }

    [Fact]
    public async Task GetCorrelatedChanges_WhenNoCorrelations_ShouldReturnEmpty()
    {
        var store = Substitute.For<IIncidentStore>();
        var repo = Substitute.For<IIncidentCorrelationRepository>();

        var incidentId = Guid.NewGuid();

        store.IncidentExists(incidentId.ToString()).Returns(true);
        repo.GetByIncidentIdAsync(incidentId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetCorrelatedChanges.Handler(store, repo);
        var query = new GetCorrelatedChanges.Query(incidentId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCorrelations.Should().Be(0);
        result.Value.Correlations.Should().BeEmpty();
    }

    // ── IncidentChangeCorrelation domain entity ───────────────────────────

    [Fact]
    public void IncidentChangeCorrelation_Create_ShouldInitializeAllRequiredProperties()
    {
        var incidentId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var correlation = IncidentChangeCorrelation.Create(
            incidentId, changeId, serviceId,
            CorrelationConfidenceLevel.High, CorrelationMatchType.ExactServiceMatch,
            24, now, null,
            "payment-service", "Deploy v3.0", "Production", now.AddHours(-1));

        correlation.IncidentId.Should().Be(incidentId);
        correlation.ChangeId.Should().Be(changeId);
        correlation.ServiceId.Should().Be(serviceId);
        correlation.ConfidenceLevel.Should().Be(CorrelationConfidenceLevel.High);
        correlation.MatchType.Should().Be(CorrelationMatchType.ExactServiceMatch);
        correlation.TimeWindowHours.Should().Be(24);
        correlation.CorrelatedAt.Should().Be(now);
        correlation.ServiceName.Should().Be("payment-service");
        correlation.ChangeEnvironment.Should().Be("Production");
    }

    [Fact]
    public void IncidentChangeCorrelation_Create_WhenEmptyServiceName_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => IncidentChangeCorrelation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CorrelationConfidenceLevel.Low, CorrelationMatchType.TimeProximity,
            24, now, null,
            string.Empty,    // invalid: empty service name
            "Description", "Production", now.AddHours(-1));

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void IncidentChangeCorrelation_Create_WhenZeroTimeWindow_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => IncidentChangeCorrelation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CorrelationConfidenceLevel.Low, CorrelationMatchType.TimeProximity,
            0,    // invalid: zero time window
            now, null,
            "svc-test", "Description", "Production", now.AddHours(-1));

        act.Should().Throw<Exception>();
    }
}
