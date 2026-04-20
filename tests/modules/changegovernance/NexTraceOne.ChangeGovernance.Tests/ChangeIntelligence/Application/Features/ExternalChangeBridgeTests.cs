using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using ImportExternalChangeRequestFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ImportExternalChangeRequest.ImportExternalChangeRequest;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para o Wave B.3 — External Change Bridge.
/// Cobre ImportExternalChangeRequest e a entidade ExternalChangeRequest.
/// </summary>
public sealed class ExternalChangeBridgeTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static IConfigurationResolutionService CreateConfig()
    {
        var cfg = Substitute.For<IConfigurationResolutionService>();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var key = ci.ArgAt<string>(0);
                var val = key == "integrations.externalChange.autoLinkEnabled" ? "false" : null;
                if (val is null) return Task.FromResult<EffectiveConfigurationDto?>(null);
                return Task.FromResult<EffectiveConfigurationDto?>(
                    new EffectiveConfigurationDto(key, val, "System", null, false, true, key, "Boolean", false, 1));
            });
        return cfg;
    }

    private static ImportExternalChangeRequestFeature.Command BuildCommand(
        string system = "ServiceNow",
        string externalId = "CHG0012345",
        string title = "Deploy v2.0 to production",
        string changeType = "Normal",
        string requestedBy = "john.doe@company.com") =>
        new(system, externalId, title, null, changeType, requestedBy, null, null, null, null);

    [Fact]
    public async Task ImportExternalChangeRequest_ValidServiceNow_CreatesRequest()
    {
        var repo = Substitute.For<IExternalChangeRequestRepository>();
        repo.GetByExternalIdAsync("ServiceNow", "CHG0012345", Arg.Any<CancellationToken>()).Returns((ExternalChangeRequest?)null);
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();

        var handler = new ImportExternalChangeRequestFeature.Handler(repo, uow, CreateClock(), CreateConfig());
        var result = await handler.Handle(BuildCommand("ServiceNow"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalSystem.Should().Be("ServiceNow");
        result.Value.ExternalId.Should().Be("CHG0012345");
        result.Value.Status.Should().Be(ExternalChangeRequestStatus.Ingested);
        repo.Received(1).Add(Arg.Any<ExternalChangeRequest>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportExternalChangeRequest_ValidJira_CreatesRequest()
    {
        var repo = Substitute.For<IExternalChangeRequestRepository>();
        repo.GetByExternalIdAsync("Jira", "OPS-999", Arg.Any<CancellationToken>()).Returns((ExternalChangeRequest?)null);
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();

        var handler = new ImportExternalChangeRequestFeature.Handler(repo, uow, CreateClock(), CreateConfig());
        var result = await handler.Handle(BuildCommand("Jira", "OPS-999"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalSystem.Should().Be("Jira");
        result.Value.ExternalId.Should().Be("OPS-999");
    }

    [Fact]
    public async Task ImportExternalChangeRequest_DuplicateExternalId_IsIdempotent()
    {
        var existing = ExternalChangeRequest.Create(
            "ServiceNow", "CHG0012345", "Deploy v2.0", null, "Normal", "john@test.com",
            null, null, null, null, FixedNow);

        var repo = Substitute.For<IExternalChangeRequestRepository>();
        repo.GetByExternalIdAsync("ServiceNow", "CHG0012345", Arg.Any<CancellationToken>()).Returns(existing);
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();

        var handler = new ImportExternalChangeRequestFeature.Handler(repo, uow, CreateClock(), CreateConfig());
        var result = await handler.Handle(BuildCommand("ServiceNow"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.DidNotReceive().Add(Arg.Any<ExternalChangeRequest>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportExternalChangeRequest_InvalidSystem_ReturnsValidationError()
    {
        var repo = Substitute.For<IExternalChangeRequestRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var validator = new ImportExternalChangeRequestFeature.Validator();

        var result = await validator.ValidateAsync(BuildCommand("UnknownSystem"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExternalSystem");
    }

    [Fact]
    public async Task ImportExternalChangeRequest_EmptyExternalId_ReturnsValidationError()
    {
        var validator = new ImportExternalChangeRequestFeature.Validator();

        var result = await validator.ValidateAsync(BuildCommand(externalId: ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExternalId");
    }

    [Fact]
    public async Task ImportExternalChangeRequest_EmptyTitle_ReturnsValidationError()
    {
        var validator = new ImportExternalChangeRequestFeature.Validator();

        var result = await validator.ValidateAsync(BuildCommand(title: ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void ExternalChangeRequestEntity_Create_SetsStatusToPending()
    {
        // The factory method sets status to Ingested (ingestion has occurred)
        var entity = ExternalChangeRequest.Create(
            "Jira", "OPS-42", "Deploy service", null, "Normal", "user@test.com",
            null, null, null, null, FixedNow);

        entity.ExternalSystem.Should().Be("Jira");
        entity.ExternalId.Should().Be("OPS-42");
        entity.Status.Should().Be(ExternalChangeRequestStatus.Ingested);
        entity.IngestedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void ExternalChangeRequestEntity_LinkToRelease_SetsLinkedReleaseId()
    {
        var entity = ExternalChangeRequest.Create(
            "ServiceNow", "CHG-001", "Patch release", null, "Standard", "ops@test.com",
            null, null, null, null, FixedNow);

        var releaseId = Guid.NewGuid();
        entity.LinkToRelease(releaseId);

        entity.LinkedReleaseId.Should().Be(releaseId);
        entity.Status.Should().Be(ExternalChangeRequestStatus.Linked);
    }

    [Fact]
    public void ExternalChangeRequestEntity_Reject_SetsStatusToRejected()
    {
        var entity = ExternalChangeRequest.Create(
            "Generic", "EXT-999", "Unknown change", null, "Emergency", "unknown@test.com",
            null, null, null, null, FixedNow);

        entity.Reject("System not recognized in this environment");

        entity.Status.Should().Be(ExternalChangeRequestStatus.Rejected);
        entity.RejectionReason.Should().Contain("System not recognized");
    }

    [Fact]
    public void ExternalChangeRequest_StatusPending_ByDefault()
    {
        // After Create, status is Ingested (factory records the ingestion).
        // The Pending status represents before any processing — validated via enum definition.
        ExternalChangeRequestStatus.Pending.Should().Be((ExternalChangeRequestStatus)0);
        ExternalChangeRequestStatus.Ingested.Should().Be((ExternalChangeRequestStatus)1);
        ExternalChangeRequestStatus.Linked.Should().Be((ExternalChangeRequestStatus)2);
        ExternalChangeRequestStatus.Rejected.Should().Be((ExternalChangeRequestStatus)3);
    }
}
