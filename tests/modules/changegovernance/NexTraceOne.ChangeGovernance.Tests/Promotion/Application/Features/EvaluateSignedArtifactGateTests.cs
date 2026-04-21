using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using Feature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluateSignedArtifactGate.EvaluateSignedArtifactGate;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application.Features;

/// <summary>
/// Testes unitários para <see cref="Feature"/> (EvaluateSignedArtifactGate).
///
/// Cobre:
/// - NotFound quando PromotionRequest não existe;
/// - Skipped quando gate desativado por config;
/// - Failed quando sem release associada;
/// - Failed quando release sem ArtifactDigest;
/// - Passed quando release tem ArtifactDigest.
/// </summary>
public sealed class EvaluateSignedArtifactGateTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static EffectiveConfigurationDto BuildConfig(string value)
        => new("slsa.artifact_gate.enabled", value, "System", null, false, false, "slsa.artifact_gate.enabled", "Boolean", false, 1);

    private static DeploymentEnvironment CreateEnvironment()
        => DeploymentEnvironment.Create("Production", "Prod env", 1, true, true, FixedNow);

    private static PromotionRequest CreatePromotionRequest(DeploymentEnvironment env)
        => PromotionRequest.Create(Guid.NewGuid(), env.Id, env.Id, "dev@example.com", FixedNow);

    private static Release CreateRelease(string? digest = null, string? provenanceUri = null)
    {
        var r = Release.Create(
            tenantId: TenantId,
            apiAssetId: Guid.NewGuid(),
            serviceName: "payment-service",
            version: "2.3.1",
            environment: "production",
            pipelineSource: "https://ci.example.com/p/1",
            commitSha: "cafebabe",
            createdAt: FixedNow);

        if (digest is not null || provenanceUri is not null)
            r.AttachSlsaProvenance(provenanceUri, digest, null);

        return r;
    }

    private static (Feature.Handler handler, IPromotionRequestRepository requestRepo, IReleaseRepository releaseRepo)
        CreateSut(
            PromotionRequest? promotionRequest,
            Release? release,
            bool gateEnabled)
    {
        var requestRepo = Substitute.For<IPromotionRequestRepository>();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var dt = Substitute.For<IDateTimeProvider>();

        requestRepo.GetByIdAsync(Arg.Any<PromotionRequestId>(), Arg.Any<CancellationToken>())
            .Returns(promotionRequest);

        releaseRepo.GetByServiceNameVersionEnvironmentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(release);

        configService.ResolveEffectiveValueAsync(
            "slsa.artifact_gate.enabled",
            ConfigurationScope.System,
            null,
            Arg.Any<CancellationToken>())
            .Returns(gateEnabled ? BuildConfig("true") : BuildConfig("false"));

        dt.UtcNow.Returns(FixedNow);

        return (new Feature.Handler(requestRepo, releaseRepo, configService, dt), requestRepo, releaseRepo);
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Returns_NotFound_For_Unknown_PromotionRequest()
    {
        var (handler, _, _) = CreateSut(promotionRequest: null, release: null, gateEnabled: true);
        var query = new Feature.Query(Guid.NewGuid(), "my-service", "1.0.0", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Skipped_When_Gate_Disabled()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var (handler, _, _) = CreateSut(pr, release: null, gateEnabled: false);
        var query = new Feature.Query(pr.Id.Value, "my-service", "1.0.0", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GateSkipped.Should().BeTrue();
        result.Value.GatePassed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Failed_When_No_Release_Found()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var (handler, _, _) = CreateSut(pr, release: null, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "my-service", "1.0.0", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GatePassed.Should().BeFalse();
        result.Value.GateSkipped.Should().BeFalse();
        result.Value.HasArtifactAttestation.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Failed_When_Release_Has_No_ArtifactDigest()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var release = CreateRelease(digest: null);
        var (handler, _, _) = CreateSut(pr, release, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "payment-service", "2.3.1", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GatePassed.Should().BeFalse();
        result.Value.HasArtifactAttestation.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Passed_When_Release_Has_ArtifactDigest()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var release = CreateRelease(digest: "sha256:cafebabe");
        var (handler, _, _) = CreateSut(pr, release, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "payment-service", "2.3.1", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GatePassed.Should().BeTrue();
        result.Value.HasArtifactAttestation.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Includes_ArtifactDigest_In_Response()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var release = CreateRelease(digest: "sha256:deadbeef");
        var (handler, _, _) = CreateSut(pr, release, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "payment-service", "2.3.1", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.ArtifactDigest.Should().Be("sha256:deadbeef");
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Includes_ProvenanceUri_In_Response()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var uri = "https://slsa.example.com/p/42";
        var release = CreateRelease(digest: "sha256:abc", provenanceUri: uri);
        var (handler, _, _) = CreateSut(pr, release, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "payment-service", "2.3.1", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.SlsaProvenanceUri.Should().Be(uri);
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_GatePassed_False_When_Skipped_Is_False_And_No_Attestation()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var release = CreateRelease(digest: null);
        var (handler, _, _) = CreateSut(pr, release, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "payment-service", "2.3.1", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.GateSkipped.Should().BeFalse();
        result.Value.GatePassed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Response_Has_Correct_ServiceName()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var (handler, _, _) = CreateSut(pr, release: null, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "inventory-api", "3.0.0", "production");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.ServiceName.Should().Be("inventory-api");
    }

    [Fact]
    public async Task EvaluateSignedArtifactGate_Response_Has_Correct_TargetEnvironment()
    {
        var env = CreateEnvironment();
        var pr = CreatePromotionRequest(env);
        var (handler, _, _) = CreateSut(pr, release: null, gateEnabled: true);
        var query = new Feature.Query(pr.Id.Value, "inventory-api", "3.0.0", "staging");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value!.TargetEnvironment.Should().Be("staging");
    }
}
