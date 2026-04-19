using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using ListReleasesByServiceFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleasesByService.ListReleasesByService;
namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para o handler ListReleasesByService.
/// Verifica paginação, contagem total, mapeamento de campos e validação de entrada.
/// </summary>
public sealed class ListReleasesByServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease(string service = "payment-service", string version = "1.0.0", string env = "production")
        => Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.Empty,
            serviceName: service,
            version: version,
            environment: env,
            pipelineSource: "https://ci/pipeline/1",
            commitSha: "abc1234",
            createdAt: FixedNow);

    // ── Handler ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListReleasesByService_WhenReleasesExist_ShouldReturnPaginatedList()
    {
        var repo = Substitute.For<IReleaseRepository>();
        var releases = new[] { CreateRelease("svc-a"), CreateRelease("svc-a", "2.0.0") };
        repo.ListByServiceNameAsync("svc-a", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Release>(releases));
        repo.CountByServiceNameAsync("svc-a", Arg.Any<CancellationToken>())
            .Returns(2);

        var sut = new ListReleasesByServiceFeature.Handler(repo);
        var result = await sut.Handle(new ListReleasesByServiceFeature.Query("svc-a"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListReleasesByService_WhenNoReleases_ShouldReturnEmptyList()
    {
        var repo = Substitute.For<IReleaseRepository>();
        repo.ListByServiceNameAsync("unknown-svc", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Release>());
        repo.CountByServiceNameAsync("unknown-svc", Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = new ListReleasesByServiceFeature.Handler(repo);
        var result = await sut.Handle(new ListReleasesByServiceFeature.Query("unknown-svc"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListReleasesByService_ShouldMapAllFields()
    {
        var repo = Substitute.For<IReleaseRepository>();
        var release = CreateRelease("order-service", "3.0.0", "staging");
        repo.ListByServiceNameAsync("order-service", 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<Release> { release });
        repo.CountByServiceNameAsync("order-service", Arg.Any<CancellationToken>())
            .Returns(1);

        var sut = new ListReleasesByServiceFeature.Handler(repo);
        var result = await sut.Handle(
            new ListReleasesByServiceFeature.Query("order-service", Page: 1, PageSize: 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Releases[0];
        item.ReleaseId.Should().Be(release.Id.Value);
        item.ServiceName.Should().Be("order-service");
        item.Version.Should().Be("3.0.0");
        item.Environment.Should().Be("staging");
        item.ChangeLevel.Should().Be(ChangeLevel.Operational);
    }

    [Fact]
    public async Task ListReleasesByService_ShouldForwardPaginationParameters()
    {
        var repo = Substitute.For<IReleaseRepository>();
        repo.ListByServiceNameAsync("my-svc", 2, 5, Arg.Any<CancellationToken>())
            .Returns(new List<Release>());
        repo.CountByServiceNameAsync("my-svc", Arg.Any<CancellationToken>())
            .Returns(10);

        var sut = new ListReleasesByServiceFeature.Handler(repo);
        var result = await sut.Handle(
            new ListReleasesByServiceFeature.Query("my-svc", Page: 2, PageSize: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalCount.Should().Be(10);
    }

    // ── Validator ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_WhenServiceNameEmpty_ShouldFail()
    {
        var validator = new ListReleasesByServiceFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListReleasesByServiceFeature.Query(""),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public async Task Validator_WhenPageSizeExceedsMax_ShouldFail()
    {
        var validator = new ListReleasesByServiceFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListReleasesByServiceFeature.Query("svc", PageSize: 200),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public async Task Validator_WhenPageBelowMin_ShouldFail()
    {
        var validator = new ListReleasesByServiceFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListReleasesByServiceFeature.Query("svc", Page: 0),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Page");
    }

    [Fact]
    public async Task Validator_WhenValid_ShouldPass()
    {
        var validator = new ListReleasesByServiceFeature.Validator();
        var result = await validator.ValidateAsync(
            new ListReleasesByServiceFeature.Query("my-service", Page: 1, PageSize: 50),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}
