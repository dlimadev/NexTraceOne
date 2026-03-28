using System.Linq;
using System.Text.Json;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Contracts;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateRunbook;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes unitários para as features de Runbooks do subdomínio Incidents.
/// Verificam handlers, validators e respostas das queries e commands de runbooks operacionais.
/// </summary>
public sealed class RunbookFeatureTests
{
    private static readonly Guid Rb1 = Guid.Parse("bb000001-0001-0000-0000-000000000001");
    private static readonly Guid Rb2 = Guid.Parse("bb000002-0001-0000-0000-000000000001");
    private static readonly Guid Rb3 = Guid.Parse("bb000003-0001-0000-0000-000000000001");

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    private static IReadOnlyList<RunbookRecord> BuildRunbooks() =>
    [
        RunbookRecord.Create(
            RunbookRecordId.From(Rb1),
            "Payment Gateway Rollback Procedure",
            "Step-by-step guide for rolling back the payment-service deployment to a known stable version.",
            "payment-service", "ServiceDegradation",
            Serialize(new[]
            {
                new { stepOrder = 1, title = "Confirm rollback target version", description = "Identify the last known stable version.", isOptional = false },
                new { stepOrder = 2, title = "Notify affected teams", description = "Send notification to downstream consumers.", isOptional = false },
                new { stepOrder = 3, title = "Trigger rollback pipeline", description = "Use the CI/CD one-click rollback.", isOptional = false },
                new { stepOrder = 4, title = "Validate deployment health", description = "Check health endpoints.", isOptional = false },
                new { stepOrder = 5, title = "Monitor for 30 minutes", description = "Observe error rate and payment success metrics.", isOptional = false },
                new { stepOrder = 6, title = "Update incident status", description = "Mark the incident as mitigated.", isOptional = true },
            }),
            Serialize(new[] { "CI/CD pipeline access for payment-service", "Previous stable version identified", "Downstream teams notified" }),
            "After rollback, monitor error rate for at least 30 minutes.",
            "platform-team@nextraceone.io",
            DateTimeOffset.Parse("2024-01-15T09:00:00Z"),
            DateTimeOffset.Parse("2024-05-20T14:30:00Z")),

        RunbookRecord.Create(
            RunbookRecordId.From(Rb2),
            "Catalog Sync Manual Recovery",
            "Steps for manually recovering catalog synchronization.",
            "catalog-service", "DependencyFailure",
            Serialize(new[]
            {
                new { stepOrder = 1, title = "Check vendor status page", description = (string?)null, isOptional = false },
                new { stepOrder = 2, title = "Attempt manual sync request", description = (string?)null, isOptional = false },
                new { stepOrder = 3, title = "Enable fallback mode", description = (string?)null, isOptional = false },
                new { stepOrder = 4, title = "Verify catalog data freshness", description = (string?)null, isOptional = false },
            }),
            Serialize(new[] { "Access to catalog-service configuration", "Manual sync endpoint credentials" }),
            "Monitor catalog data freshness and sync error rate.",
            "platform-team@nextraceone.io",
            DateTimeOffset.Parse("2024-02-10T11:00:00Z")),

        RunbookRecord.Create(
            RunbookRecordId.From(Rb3),
            "Generic Service Restart Procedure",
            "Standard procedure for performing a controlled restart of a service.",
            null, null,
            Serialize(new[]
            {
                new { stepOrder = 1, title = "Notify dependent teams", description = (string?)null, isOptional = true },
                new { stepOrder = 2, title = "Drain active connections", description = (string?)null, isOptional = false },
                new { stepOrder = 3, title = "Trigger controlled restart", description = (string?)null, isOptional = false },
                new { stepOrder = 4, title = "Verify service health", description = (string?)null, isOptional = false },
            }),
            Serialize(new[] { "Orchestrator or deployment tool access", "Service health endpoint available" }),
            "Monitor service health for 15 minutes post-restart.",
            "sre-team@nextraceone.io",
            DateTimeOffset.Parse("2024-03-01T08:00:00Z"),
            DateTimeOffset.Parse("2024-04-10T16:00:00Z")),
    ];

    // ── ListRunbooks ─────────────────────────────────────────────────

    [Fact]
    public async Task ListRunbooks_NoFilters_ShouldReturnAllRunbooks()
    {
        var repo = Substitute.For<IRunbookRepository>();
        var allRunbooks = BuildRunbooks();
        repo.ListAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(allRunbooks);

        var handler = new ListRunbooks.Handler(repo);
        var result = await handler.Handle(new ListRunbooks.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListRunbooks_FilterBySearchTerm_ShouldPassSearchToRepository()
    {
        var repo = Substitute.For<IRunbookRepository>();
        var single = BuildRunbooks().Where(r => r.Title.Contains("Payment")).ToList();
        repo.ListAsync(null, null, "Payment", Arg.Any<CancellationToken>())
            .Returns(single);

        var handler = new ListRunbooks.Handler(repo);
        var result = await handler.Handle(new ListRunbooks.Query(null, null, "Payment"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().ContainSingle();
        result.Value.Runbooks[0].Title.Should().Contain("Payment");
    }

    [Fact]
    public async Task ListRunbooks_FilterByService_ShouldPassServiceToRepository()
    {
        var repo = Substitute.For<IRunbookRepository>();
        var single = BuildRunbooks().Where(r => r.LinkedService == "catalog-service").ToList();
        repo.ListAsync("catalog-service", null, null, Arg.Any<CancellationToken>())
            .Returns(single);

        var handler = new ListRunbooks.Handler(repo);
        var result = await handler.Handle(new ListRunbooks.Query("catalog-service", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().ContainSingle();
        result.Value.Runbooks[0].LinkedServiceId.Should().Be("catalog-service");
    }

    [Fact]
    public async Task ListRunbooks_FilterByIncidentType_ShouldPassTypeToRepository()
    {
        var repo = Substitute.For<IRunbookRepository>();
        var single = BuildRunbooks().Where(r => r.LinkedIncidentType == "DependencyFailure").ToList();
        repo.ListAsync(null, "DependencyFailure", null, Arg.Any<CancellationToken>())
            .Returns(single);

        var handler = new ListRunbooks.Handler(repo);
        var result = await handler.Handle(new ListRunbooks.Query(null, "DependencyFailure", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().ContainSingle();
        result.Value.Runbooks[0].LinkedIncidentType.Should().Be("DependencyFailure");
    }

    [Fact]
    public async Task ListRunbooks_EmptyRepository_ShouldReturnEmptyList()
    {
        var repo = Substitute.For<IRunbookRepository>();
        repo.ListAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new ListRunbooks.Handler(repo);
        var result = await handler.Handle(new ListRunbooks.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Runbooks.Should().BeEmpty();
    }

    [Fact]
    public void ListRunbooks_Validator_ShouldAcceptNullFilters()
    {
        var validator = new ListRunbooks.Validator();
        var result = validator.Validate(new ListRunbooks.Query(null, null, null));
        result.IsValid.Should().BeTrue();
    }

    // ── GetRunbookDetail ─────────────────────────────────────────────

    [Fact]
    public async Task GetRunbookDetail_KnownRunbook_ShouldReturnDetailWithSteps()
    {
        var repo = Substitute.For<IRunbookRepository>();
        var runbook = BuildRunbooks().First(r => r.Id.Value == Rb1);
        repo.GetByIdAsync(Rb1, Arg.Any<CancellationToken>()).Returns(runbook);

        var handler = new GetRunbookDetail.Handler(repo);
        var result = await handler.Handle(new GetRunbookDetail.Query(Rb1.ToString()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Payment Gateway Rollback Procedure");
        result.Value.LinkedServiceId.Should().Be("payment-service");
        result.Value.Steps.Should().HaveCount(6);
        result.Value.Preconditions.Should().HaveCount(3);
        result.Value.PostValidationGuidance.Should().NotBeNullOrEmpty();
        result.Value.CreatedBy.Should().Be("platform-team@nextraceone.io");
    }

    [Fact]
    public async Task GetRunbookDetail_UnknownRunbook_ShouldReturnError()
    {
        var repo = Substitute.For<IRunbookRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RunbookRecord?)null);

        var handler = new GetRunbookDetail.Handler(repo);
        var result = await handler.Handle(new GetRunbookDetail.Query(Guid.NewGuid().ToString()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetRunbookDetail_NonGuidRunbookId_ShouldReturnError()
    {
        var repo = Substitute.For<IRunbookRepository>();

        var handler = new GetRunbookDetail.Handler(repo);
        var result = await handler.Handle(new GetRunbookDetail.Query("nonexistent-runbook-id"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetRunbookDetail_Validator_ShouldRejectEmptyRunbookId()
    {
        var validator = new GetRunbookDetail.Validator();
        var result = validator.Validate(new GetRunbookDetail.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── CreateRunbook ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateRunbook_ValidCommand_ShouldPersistAndReturnId()
    {
        var repo = Substitute.For<IRunbookRepository>();
        var linkingService = Substitute.For<IRunbookKnowledgeLinkingService>();
        var clock = Substitute.For<IDateTimeProvider>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        var handler = new CreateRunbook.Handler(repo, linkingService, clock);
        var command = new CreateRunbook.Command(
            "Deploy Rollback Procedure",
            "Guide for rolling back a failed deployment.",
            "my-service", "ServiceDegradation",
            new[] { new CreateRunbook.CreateRunbookStepDto(1, "Check logs", "Review recent logs.", false) },
            new[] { "Access to deployment system" },
            "After rollback, monitor for 10 minutes.",
            "ops-team@example.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RunbookId.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().Be(now);
        await repo.Received(1).AddAsync(Arg.Any<RunbookRecord>(), Arg.Any<CancellationToken>());
        await linkingService.Received(1).LinkRunbookToServiceAsync(
            result.Value.RunbookId,
            command.Title,
            command.Description,
            command.LinkedService,
            command.MaintainedBy,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateRunbook_Validator_ShouldRejectMissingTitle()
    {
        var validator = new CreateRunbook.Validator();
        var command = new CreateRunbook.Command(
            "", "Some description.", null, null, null, null, null, "ops-team");
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateRunbook_Validator_ShouldRejectMissingMaintainedBy()
    {
        var validator = new CreateRunbook.Validator();
        var command = new CreateRunbook.Command(
            "My Runbook", "Description.", null, null, null, null, null, "");
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaintainedBy");
    }
}
