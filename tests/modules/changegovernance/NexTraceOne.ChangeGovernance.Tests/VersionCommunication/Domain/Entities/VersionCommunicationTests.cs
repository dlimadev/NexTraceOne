using FluentAssertions;
using NexTraceOne.VersionCommunication.Domain.Entities;
using NexTraceOne.VersionCommunication.Domain.Enums;
using Xunit;

namespace NexTraceOne.ChangeGovernance.Tests.VersionCommunication.Domain.Entities;

/// <summary>
/// Testes unitários para as entidades do subdomínio VersionCommunication.
/// Cobre VersionRolloutPlan, ConsumerMigrationPlan e VersionDeprecationSchedule,
/// incluindo ciclo de vida, transições de estado e validações de domínio.
/// </summary>
public sealed class VersionCommunicationTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 01, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ApiAssetId = Guid.NewGuid();

    private static VersionRolloutPlan CreateRolloutPlan() =>
        VersionRolloutPlan.Create(
            TenantId,
            ApiAssetId,
            fromVersion: "1.0.0",
            toVersion: "2.0.0",
            announcementDate: Now,
            availabilityDate: Now.AddDays(7),
            migrationDeadline: Now.AddDays(30),
            createdAt: Now,
            createdBy: "admin@corp.com",
            deprecationDate: Now.AddDays(60),
            notes: "Major version rollout");

    // ─── VersionRolloutPlan ──────────────────────────────────────────

    [Fact]
    public void VersionRolloutPlan_Create_ShouldSetDraftStatus()
    {
        var plan = CreateRolloutPlan();

        plan.Id.Value.Should().NotBeEmpty();
        plan.TenantId.Should().Be(TenantId);
        plan.ApiAssetId.Should().Be(ApiAssetId);
        plan.FromVersion.Should().Be("1.0.0");
        plan.ToVersion.Should().Be("2.0.0");
        plan.Status.Should().Be(VersionRolloutStatus.Draft);
        plan.Notes.Should().Be("Major version rollout");
        plan.CreatedBy.Should().Be("admin@corp.com");
    }

    [Fact]
    public void VersionRolloutPlan_Announce_ShouldSetAnnouncedStatus()
    {
        var plan = CreateRolloutPlan();

        var result = plan.Announce();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(VersionRolloutStatus.Announced);
    }

    [Fact]
    public void VersionRolloutPlan_StartMigration_FromAnnounced_ShouldSetInProgress()
    {
        var plan = CreateRolloutPlan();
        plan.Announce();

        var result = plan.StartMigration();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(VersionRolloutStatus.InProgress);
    }

    [Fact]
    public void VersionRolloutPlan_Complete_FromInProgress_ShouldSetCompleted()
    {
        var plan = CreateRolloutPlan();
        plan.Announce();
        plan.StartMigration();

        var result = plan.Complete();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(VersionRolloutStatus.Completed);
    }

    [Fact]
    public void VersionRolloutPlan_Cancel_ShouldSetCancelled()
    {
        var plan = CreateRolloutPlan();

        var result = plan.Cancel();

        result.IsSuccess.Should().BeTrue();
        plan.Status.Should().Be(VersionRolloutStatus.Cancelled);
    }

    [Fact]
    public void VersionRolloutPlan_Announce_WhenNotDraft_ShouldFail()
    {
        var plan = CreateRolloutPlan();
        plan.Announce();

        var result = plan.Announce();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    // ─── ConsumerMigrationPlan ───────────────────────────────────────

    [Fact]
    public void ConsumerMigrationPlan_Create_ShouldSetPendingStatus()
    {
        var rolloutPlanId = VersionRolloutPlanId.New();
        var consumerAssetId = Guid.NewGuid();

        var migration = ConsumerMigrationPlan.Create(
            rolloutPlanId, consumerAssetId, "Payment Service", "Notes");

        migration.Id.Value.Should().NotBeEmpty();
        migration.VersionRolloutPlanId.Should().Be(rolloutPlanId);
        migration.ConsumerAssetId.Should().Be(consumerAssetId);
        migration.ConsumerName.Should().Be("Payment Service");
        migration.Status.Should().Be(ConsumerMigrationStatus.Pending);
        migration.Notes.Should().Be("Notes");
    }

    [Fact]
    public void ConsumerMigrationPlan_MarkNotified_ShouldUpdateStatus()
    {
        var migration = ConsumerMigrationPlan.Create(
            VersionRolloutPlanId.New(), Guid.NewGuid(), "Service A");

        var result = migration.MarkNotified(Now);

        result.IsSuccess.Should().BeTrue();
        migration.Status.Should().Be(ConsumerMigrationStatus.Notified);
        migration.NotifiedAt.Should().Be(Now);
    }

    [Fact]
    public void ConsumerMigrationPlan_MarkAcknowledged_ShouldUpdateStatus()
    {
        var migration = ConsumerMigrationPlan.Create(
            VersionRolloutPlanId.New(), Guid.NewGuid(), "Service A");
        migration.MarkNotified(Now);

        var result = migration.MarkAcknowledged(Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        migration.Status.Should().Be(ConsumerMigrationStatus.Acknowledged);
        migration.AcknowledgedAt.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void ConsumerMigrationPlan_MarkMigrated_ShouldUpdateStatus()
    {
        var migration = ConsumerMigrationPlan.Create(
            VersionRolloutPlanId.New(), Guid.NewGuid(), "Service A");
        migration.MarkNotified(Now);
        migration.MarkAcknowledged(Now.AddHours(1));

        var result = migration.MarkMigrated(Now.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        migration.Status.Should().Be(ConsumerMigrationStatus.Completed);
        migration.MigratedAt.Should().Be(Now.AddDays(1));
    }

    // ─── VersionDeprecationSchedule ──────────────────────────────────

    [Fact]
    public void VersionDeprecationSchedule_Create_ShouldSetProperties()
    {
        var schedule = VersionDeprecationSchedule.Create(
            ApiAssetId, "1.0.0", Now, Now.AddDays(90), 5, "Deprecating v1");

        schedule.Id.Value.Should().NotBeEmpty();
        schedule.ApiAssetId.Should().Be(ApiAssetId);
        schedule.Version.Should().Be("1.0.0");
        schedule.DeprecationAnnouncedAt.Should().Be(Now);
        schedule.SunsetDate.Should().Be(Now.AddDays(90));
        schedule.IsEnforced.Should().BeFalse();
        schedule.AffectedConsumerCount.Should().Be(5);
        schedule.Notes.Should().Be("Deprecating v1");
    }

    [Fact]
    public void VersionDeprecationSchedule_Enforce_ShouldSetIsEnforced()
    {
        var schedule = VersionDeprecationSchedule.Create(
            ApiAssetId, "1.0.0", Now, Now.AddDays(90), 3);

        var result = schedule.Enforce();

        result.IsSuccess.Should().BeTrue();
        schedule.IsEnforced.Should().BeTrue();
    }

    [Fact]
    public void VersionDeprecationSchedule_Extend_ShouldUpdateSunsetDate()
    {
        var schedule = VersionDeprecationSchedule.Create(
            ApiAssetId, "1.0.0", Now, Now.AddDays(90), 3);
        var newDate = Now.AddDays(120);

        var result = schedule.Extend(newDate);

        result.IsSuccess.Should().BeTrue();
        schedule.SunsetDate.Should().Be(newDate);
    }
}
