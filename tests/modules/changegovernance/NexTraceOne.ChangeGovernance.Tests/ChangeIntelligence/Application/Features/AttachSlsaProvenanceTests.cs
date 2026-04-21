using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using Feature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AttachSlsaProvenance.AttachSlsaProvenance;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para <see cref="Feature"/> (AttachSlsaProvenance).
///
/// Cobre:
/// - caminho NotFound quando a release não existe;
/// - sucesso com cada campo isolado e com todos os campos;
/// - validação de command;
/// - verificação dos campos após attach.
/// </summary>
public sealed class AttachSlsaProvenanceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease(string serviceName = "my-service", string version = "1.0.0")
        => Release.Create(
            tenantId: TenantId,
            apiAssetId: Guid.NewGuid(),
            serviceName: serviceName,
            version: version,
            environment: "staging",
            pipelineSource: "https://ci.example.com/pipeline/1",
            commitSha: "abc123def456",
            createdAt: FixedNow);

    private static (Feature.Handler handler, IReleaseRepository repo, IChangeIntelligenceUnitOfWork uow)
        CreateSut(Release? release = null)
    {
        var repo = Substitute.For<IReleaseRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();

        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        return (new Feature.Handler(repo, uow), repo, uow);
    }

    [Fact]
    public async Task AttachSlsaProvenance_Returns_NotFound_For_Unknown_Release()
    {
        var (handler, _, _) = CreateSut(release: null);
        var cmd = new Feature.Command(Guid.NewGuid(), "https://provenance.example.com", null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AttachSlsaProvenance_Succeeds_With_ArtifactDigest_Only()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var cmd = new Feature.Command(release.Id.Value, null, "sha256:abc123", null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ArtifactDigest.Should().Be("sha256:abc123");
    }

    [Fact]
    public async Task AttachSlsaProvenance_Succeeds_With_ProvenanceUri_Only()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var uri = "https://slsa.example.com/provenance/123";
        var cmd = new Feature.Command(release.Id.Value, uri, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SlsaProvenanceUri.Should().Be(uri);
    }

    [Fact]
    public async Task AttachSlsaProvenance_Succeeds_With_SbomUri_Only()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var sbom = "https://sbom.example.com/sbom/v1.json";
        var cmd = new Feature.Command(release.Id.Value, null, null, sbom);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SbomUri.Should().Be(sbom);
    }

    [Fact]
    public async Task AttachSlsaProvenance_Succeeds_With_All_Fields()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var cmd = new Feature.Command(
            release.Id.Value,
            "https://slsa.example.com/provenance/1",
            "sha256:deadbeef",
            "https://sbom.example.com/sbom/1.json");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SlsaProvenanceUri.Should().Be("https://slsa.example.com/provenance/1");
        result.Value.ArtifactDigest.Should().Be("sha256:deadbeef");
        result.Value.SbomUri.Should().Be("https://sbom.example.com/sbom/1.json");
    }

    [Fact]
    public void AttachSlsaProvenance_Fails_Validation_When_No_Evidence_Provided()
    {
        var validator = new Feature.Validator();
        var cmd = new Feature.Command(Guid.NewGuid(), null, null, null);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AttachSlsaProvenance_Release_Has_ArtifactDigest_After_Attach()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var cmd = new Feature.Command(release.Id.Value, null, "sha256:abc", null);

        await handler.Handle(cmd, CancellationToken.None);

        release.ArtifactDigest.Should().Be("sha256:abc");
    }

    [Fact]
    public async Task AttachSlsaProvenance_Release_Has_ProvenanceUri_After_Attach()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var uri = "https://slsa.example.com/p/9";
        var cmd = new Feature.Command(release.Id.Value, uri, null, null);

        await handler.Handle(cmd, CancellationToken.None);

        release.SlsaProvenanceUri.Should().Be(uri);
    }

    [Fact]
    public async Task AttachSlsaProvenance_Release_Has_SbomUri_After_Attach()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var sbom = "https://sbom.example.com/s/1";
        var cmd = new Feature.Command(release.Id.Value, null, null, sbom);

        await handler.Handle(cmd, CancellationToken.None);

        release.SbomUri.Should().Be(sbom);
    }

    [Fact]
    public async Task AttachSlsaProvenance_HasArtifactAttestation_True_When_Digest_Set()
    {
        var release = CreateRelease();
        var (handler, _, _) = CreateSut(release);
        var cmd = new Feature.Command(release.Id.Value, null, "sha512:abcdef123", null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Value!.HasArtifactAttestation.Should().BeTrue();
    }
}
