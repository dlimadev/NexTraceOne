using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.Configuration.Application.Abstractions;

using IngestExternalReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestExternalRelease.IngestExternalRelease;
using ResolveReleaseByExternalKeyFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ResolveReleaseByExternalKey.ResolveReleaseByExternalKey;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes do padrão "Natural Key Routing" para a Ingestion API.
/// Cobre ResolveReleaseByExternalKey (Query) e a idempotência de IngestExternalRelease (Command).
/// </summary>
public sealed class NaturalKeyRoutingTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 21, 0, 0, TimeSpan.Zero);

    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();

    private static Release CreateReleaseWithExternalKey(
        string externalReleaseId = "jenkins-build-42",
        string externalSystem = "jenkins",
        string service = "payment-service")
        => Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.Empty,
            serviceName: service,
            version: "1.2.0",
            environment: "staging",
            pipelineSource: $"External:{externalSystem}",
            commitSha: "abc1234",
            createdAt: FixedNow,
            externalReleaseId: externalReleaseId,
            externalSystem: externalSystem);

    // ── ResolveReleaseByExternalKey ────────────────────────────────────────

    [Fact]
    public async Task ResolveReleaseByExternalKey_WhenFound_ShouldReturnReleaseId()
    {
        var release = CreateReleaseWithExternalKey();
        _releaseRepo.GetByExternalKeyAsync("jenkins-build-42", "jenkins", Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var result = await sut.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-42", "jenkins"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.ExternalReleaseId.Should().Be("jenkins-build-42");
        result.Value.ExternalSystem.Should().Be("jenkins");
        result.Value.ServiceName.Should().Be("payment-service");
        result.Value.Version.Should().Be("1.2.0");
        result.Value.Environment.Should().Be("staging");
    }

    [Fact]
    public async Task ResolveReleaseByExternalKey_WhenNotFound_ShouldReturnFailure()
    {
        _releaseRepo.GetByExternalKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new ResolveReleaseByExternalKeyFeature.Handler(_releaseRepo);
        var result = await sut.Handle(
            new ResolveReleaseByExternalKeyFeature.Query("unknown-build-99", "jenkins"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveReleaseByExternalKey_Validator_WhenExternalReleaseIdEmpty_ShouldFail()
    {
        var validator = new ResolveReleaseByExternalKeyFeature.Validator();
        var result = await validator.ValidateAsync(
            new ResolveReleaseByExternalKeyFeature.Query("", "jenkins"),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ExternalReleaseId");
    }

    [Fact]
    public async Task ResolveReleaseByExternalKey_Validator_WhenExternalSystemEmpty_ShouldFail()
    {
        var validator = new ResolveReleaseByExternalKeyFeature.Validator();
        var result = await validator.ValidateAsync(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-42", ""),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ExternalSystem");
    }

    [Fact]
    public async Task ResolveReleaseByExternalKey_Validator_WhenValid_ShouldPass()
    {
        var validator = new ResolveReleaseByExternalKeyFeature.Validator();
        var result = await validator.ValidateAsync(
            new ResolveReleaseByExternalKeyFeature.Query("jenkins-build-42", "jenkins"),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    // ── IngestExternalRelease — idempotência por chave natural ─────────────

    [Fact]
    public async Task IngestExternalRelease_WhenAlreadyExists_ShouldReturnExisting_ById()
    {
        var existing = CreateReleaseWithExternalKey("gh-run-123", "github");
        _releaseRepo.GetByExternalKeyAsync("gh-run-123", "github", Arg.Any<CancellationToken>())
            .Returns(existing);

        var unitOfWork = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();
        var envBehavior = Substitute.For<IEnvironmentBehaviorService>();
        currentTenant.Id.Returns(Guid.NewGuid());
        clock.UtcNow.Returns(FixedNow);
        envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new IngestExternalReleaseFeature.Handler(
            _releaseRepo, currentTenant, unitOfWork, clock, envBehavior);

        var result = await sut.Handle(new IngestExternalReleaseFeature.Command(
            ExternalReleaseId: "gh-run-123",
            ExternalSystem: "github",
            ServiceName: "payment-service",
            Version: "1.2.0",
            TargetEnvironment: "staging"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeFalse();
        result.Value.ReleaseId.Should().Be(existing.Id.Value);
        result.Value.ExternalReleaseId.Should().Be("gh-run-123");

        // Não deveria ter adicionado uma nova release
        _releaseRepo.DidNotReceive().Add(Arg.Any<Release>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestExternalRelease_WhenNew_ShouldStoreExternalKey()
    {
        _releaseRepo.GetByExternalKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var unitOfWork = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IDateTimeProvider>();
        var envBehavior = Substitute.For<IEnvironmentBehaviorService>();
        currentTenant.Id.Returns(Guid.NewGuid());
        clock.UtcNow.Returns(FixedNow);
        envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new IngestExternalReleaseFeature.Handler(
            _releaseRepo, currentTenant, unitOfWork, clock, envBehavior);

        var result = await sut.Handle(new IngestExternalReleaseFeature.Command(
            ExternalReleaseId: "azdo-build-77",
            ExternalSystem: "azuredevops",
            ServiceName: "order-service",
            Version: "2.0.1",
            TargetEnvironment: "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        result.Value.ExternalReleaseId.Should().Be("azdo-build-77");

        _releaseRepo.Received(1).Add(Arg.Is<Release>(r =>
            r.ExternalReleaseId == "azdo-build-77" &&
            r.ExternalSystem == "azuredevops" &&
            r.ServiceName == "order-service"));

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Release entity — ExternalKey stored correctly ──────────────────────

    [Fact]
    public void Release_Create_WithExternalKey_ShouldStoreFields()
    {
        var release = Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.Empty,
            serviceName: "catalog-service",
            version: "3.1.0",
            environment: "prod",
            pipelineSource: "External:jenkins",
            commitSha: "deadbeef",
            createdAt: FixedNow,
            externalReleaseId: "jenkins-build-99",
            externalSystem: "jenkins");

        release.ExternalReleaseId.Should().Be("jenkins-build-99");
        release.ExternalSystem.Should().Be("jenkins");
    }

    [Fact]
    public void Release_Create_WithoutExternalKey_ShouldHaveNullFields()
    {
        var release = Release.Create(
            tenantId: Guid.NewGuid(),
            apiAssetId: Guid.Empty,
            serviceName: "catalog-service",
            version: "3.1.0",
            environment: "prod",
            pipelineSource: "CI/CD Pipeline",
            commitSha: "deadbeef",
            createdAt: FixedNow);

        release.ExternalReleaseId.Should().BeNull();
        release.ExternalSystem.Should().BeNull();
    }
}
