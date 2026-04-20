using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using ListReleasesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases.ListReleases;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para ListReleases — lista releases de um ativo de API ou do tenant completo.
/// Valida happy path com e sem ApiAssetId, lista vazia e validação de entrada.
/// </summary>
public sealed class ListReleasesTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 14, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    private ListReleasesFeature.Handler CreateHandler() =>
        new(_releaseRepo, _currentTenant);

    private static Release MakeRelease(string service = "checkout-api", string env = "production") =>
        Release.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            service,
            "1.2.0",
            env,
            "jenkins-pipeline",
            "abc123",
            FixedNow);

    // ── Happy path — with ApiAssetId ─────────────────────────────────────────

    [Fact]
    public async Task Handle_WithApiAssetId_ListsByAsset_AndReturnsDto()
    {
        var assetId = Guid.NewGuid();
        var release = MakeRelease();

        _releaseRepo.ListByApiAssetAsync(assetId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Release> { release });

        var result = await CreateHandler().Handle(
            new ListReleasesFeature.Query(assetId, Page: 1, PageSize: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().HaveCount(1);
        result.Value.Releases[0].ServiceName.Should().Be("checkout-api");
        result.Value.Releases[0].Version.Should().Be("1.2.0");
        result.Value.Releases[0].Environment.Should().Be("production");
        result.Value.Releases[0].ChangeLevel.Should().Be(ChangeLevel.Operational);
    }

    [Fact]
    public async Task Handle_WithApiAssetId_DoesNotCallListFiltered()
    {
        var assetId = Guid.NewGuid();
        _releaseRepo.ListByApiAssetAsync(assetId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        await CreateHandler().Handle(
            new ListReleasesFeature.Query(assetId),
            CancellationToken.None);

        await _releaseRepo.DidNotReceive().ListFilteredAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<ChangeType?>(), Arg.Any<ConfidenceStatus?>(), Arg.Any<DeploymentStatus?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ── Happy path — without ApiAssetId (list all for tenant) ────────────────

    [Fact]
    public async Task Handle_WithoutApiAssetId_ListsAllForTenant()
    {
        var tenantId = Guid.NewGuid();
        _currentTenant.Id.Returns(tenantId);

        var release = MakeRelease("payments-api", "staging");
        _releaseRepo.ListFilteredAsync(
            tenantId,
            null, null, null,
            (ChangeType?)null,
            (ConfidenceStatus?)null,
            (DeploymentStatus?)null,
            null,
            (DateTimeOffset?)null,
            (DateTimeOffset?)null,
            1, 20,
            Arg.Any<CancellationToken>())
            .Returns(new List<Release> { release });

        var result = await CreateHandler().Handle(
            new ListReleasesFeature.Query(null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().HaveCount(1);
        result.Value.Releases[0].ServiceName.Should().Be("payments-api");
    }

    // ── Empty result ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        var assetId = Guid.NewGuid();
        _releaseRepo.ListByApiAssetAsync(assetId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        var result = await CreateHandler().Handle(
            new ListReleasesFeature.Query(assetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Releases.Should().BeEmpty();
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_PageZero_ReturnsError()
    {
        var validator = new ListReleasesFeature.Validator();
        var result = validator.Validate(new ListReleasesFeature.Query(null, Page: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Page");
    }

    [Fact]
    public void Validator_PageSizeOver100_ReturnsError()
    {
        var validator = new ListReleasesFeature.Validator();
        var result = validator.Validate(new ListReleasesFeature.Query(null, PageSize: 101));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validator_ValidInput_Passes()
    {
        var validator = new ListReleasesFeature.Validator();
        var result = validator.Validate(new ListReleasesFeature.Query(Guid.NewGuid(), Page: 2, PageSize: 50));

        result.IsValid.Should().BeTrue();
    }
}
