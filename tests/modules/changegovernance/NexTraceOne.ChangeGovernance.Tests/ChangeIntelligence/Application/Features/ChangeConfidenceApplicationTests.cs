using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using ListChangesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChanges.ListChanges;
using GetChangesSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary.GetChangesSummary;
using ListChangesByServiceFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChangesByService.ListChangesByService;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes dos handlers de Change Confidence da Fase 4.4.</summary>
public sealed class ChangeConfidenceApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease(string serviceName = "TestService") =>
        Release.Create(Guid.NewGuid(), Guid.Empty, serviceName, "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    // ── ListChanges ───────────────────────────────────────────────────

    [Fact]
    public async Task ListChanges_Should_ReturnFilteredResults()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(Guid.NewGuid());
        var release = CreateRelease();
        var sut = new ListChangesFeature.Handler(repository, currentTenant);

        repository.ListFilteredAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ChangeType?>(), Arg.Any<ConfidenceStatus?>(), Arg.Any<DeploymentStatus?>(),
            Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release> { release });

        repository.CountFilteredAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ChangeType?>(), Arg.Any<ConfidenceStatus?>(), Arg.Any<DeploymentStatus?>(),
            Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await sut.Handle(
            new ListChangesFeature.Query(null, null, "prod", null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changes.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Changes[0].ServiceName.Should().Be("TestService");
    }

    [Fact]
    public async Task ListChanges_Should_ReturnEmpty_WhenNoResults()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(Guid.NewGuid());
        var sut = new ListChangesFeature.Handler(repository, currentTenant);

        repository.ListFilteredAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ChangeType?>(), Arg.Any<ConfidenceStatus?>(), Arg.Any<DeploymentStatus?>(),
            Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        repository.CountFilteredAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ChangeType?>(), Arg.Any<ConfidenceStatus?>(), Arg.Any<DeploymentStatus?>(),
            Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await sut.Handle(
            new ListChangesFeature.Query("NonExistent", null, null, null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changes.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetChangesSummary ─────────────────────────────────────────────

    [Fact]
    public async Task GetChangesSummary_Should_ReturnAggregatedCounts()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(Guid.NewGuid());
        var sut = new GetChangesSummaryFeature.Handler(repository, currentTenant);

        repository.GetSummaryCountsAsync(
            Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>())
            .Returns((100, 80, 15, 3, 2));

        var result = await sut.Handle(
            new GetChangesSummaryFeature.Query(null, "prod", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalChanges.Should().Be(100);
        result.Value.ValidatedChanges.Should().Be(80);
        result.Value.ChangesNeedingAttention.Should().Be(15);
        result.Value.SuspectedRegressions.Should().Be(3);
        result.Value.ChangesCorrelatedWithIncidents.Should().Be(2);
    }

    // ── ListChangesByService ──────────────────────────────────────────

    [Fact]
    public async Task ListChangesByService_Should_ReturnChangesForService()
    {
        var repository = Substitute.For<IReleaseRepository>();
        var release1 = CreateRelease("payments-api");
        var release2 = CreateRelease("payments-api");
        var sut = new ListChangesByServiceFeature.Handler(repository);

        repository.ListByServiceNameAsync("payments-api", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Release> { release1, release2 });

        repository.CountByServiceNameAsync("payments-api", Arg.Any<CancellationToken>())
            .Returns(2);

        var result = await sut.Handle(
            new ListChangesByServiceFeature.Query("payments-api", 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Changes.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Changes.Should().AllSatisfy(c => c.ServiceName.Should().Be("payments-api"));
    }
}
