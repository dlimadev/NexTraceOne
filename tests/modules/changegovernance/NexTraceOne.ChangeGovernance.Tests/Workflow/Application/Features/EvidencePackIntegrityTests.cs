using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.SignEvidencePack;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using SignEvidencePackFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.SignEvidencePack.SignEvidencePack;
using VerifyIntegrityFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.VerifyEvidencePackIntegrity.VerifyEvidencePackIntegrity;

namespace NexTraceOne.ChangeGovernance.Tests.Workflow.Application.Features;

/// <summary>
/// Testes unitários para Wave C.2 — Evidence Pack Integrity Signing.
/// Cobre SignEvidencePack e VerifyEvidencePackIntegrity features.
/// </summary>
public sealed class EvidencePackIntegrityTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private const string TestSigningKey = "test-signing-key-for-unit-tests";

    private static WorkflowTemplate CreateTemplate() =>
        WorkflowTemplate.Create("Release Approval", "desc", "Breaking", "High", "Production", 2, FixedNow);

    private static WorkflowInstance CreateInstance() =>
        WorkflowInstance.Create(WorkflowTemplateId.New(), Guid.NewGuid(), "submitter@test.com", FixedNow);

    private static EvidencePack CreateEvidencePack(WorkflowInstanceId instanceId) =>
        EvidencePack.Create(instanceId, Guid.NewGuid(), FixedNow);

    private static IConfigurationResolutionService CreateConfigService(string key = TestSigningKey)
    {
        var cfg = Substitute.For<IConfigurationResolutionService>();
        cfg.ResolveEffectiveValueAsync(
                EvidencePackConfigKeys.SigningKeyHex,
                Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto(
                EvidencePackConfigKeys.SigningKeyHex, key, "System", null, false, true, "label", "String", false, 1));
        return cfg;
    }

    private static IDateTimeProvider CreateClock() =>
        Substitute.For<IDateTimeProvider>() is { } c
            ? (c.UtcNow.Returns(FixedNow), c).Item2
            : null!;

    // ── EvidencePack domain tests ─────────────────────────────────────────

    [Fact]
    public void ApplyIntegritySignature_SetsPropertiesCorrectly()
    {
        var instanceId = WorkflowInstanceId.New();
        var pack = CreateEvidencePack(instanceId);

        pack.ApplyIntegritySignature("{}", "abc123hash", "auditor@test.com", FixedNow);

        pack.IntegrityHash.Should().Be("abc123hash");
        pack.IntegrityManifest.Should().Be("{}");
        pack.IntegritySignedBy.Should().Be("auditor@test.com");
        pack.IntegritySignedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void IsIntegritySigned_ReturnsFalse_WhenNotSigned()
    {
        var pack = CreateEvidencePack(WorkflowInstanceId.New());
        pack.IsIntegritySigned.Should().BeFalse();
    }

    [Fact]
    public void IsIntegritySigned_ReturnsTrue_AfterSigning()
    {
        var pack = CreateEvidencePack(WorkflowInstanceId.New());
        pack.ApplyIntegritySignature("{}", "somehash", "user@test.com", FixedNow);
        pack.IsIntegritySigned.Should().BeTrue();
    }

    // ── SignEvidencePack handler tests ────────────────────────────────────

    [Fact]
    public async Task SignEvidencePack_ProducesDeterministicHmacHash()
    {
        var instance = CreateInstance();
        var pack = CreateEvidencePack(instance.Id);

        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.GetByWorkflowInstanceIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(pack);

        var uow = Substitute.For<IWorkflowUnitOfWork>();
        var clock = CreateClock();
        var cfg = CreateConfigService();

        var handler = new SignEvidencePackFeature.Handler(instanceRepo, packRepo, uow, clock, cfg);
        var cmd = new SignEvidencePackFeature.Command(instance.Id.Value, "auditor@company.com");

        var result1 = await handler.Handle(cmd, CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();

        // Verify the hash is correct HMAC-SHA256
        var keyBytes = Encoding.UTF8.GetBytes(TestSigningKey);
        var dataBytes = Encoding.UTF8.GetBytes(pack.IntegrityManifest!);
        var expectedHash = Convert.ToHexString(HMACSHA256.HashData(keyBytes, dataBytes)).ToLowerInvariant();

        result1.Value.IntegrityHash.Should().Be(expectedHash);
        result1.Value.SignedBy.Should().Be("auditor@company.com");
        result1.Value.SignedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task SignEvidencePack_ReturnsError_WhenInstanceNotFound()
    {
        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var handler = new SignEvidencePackFeature.Handler(
            instanceRepo,
            Substitute.For<IEvidencePackRepository>(),
            Substitute.For<IWorkflowUnitOfWork>(),
            CreateClock(),
            CreateConfigService());

        var result = await handler.Handle(
            new SignEvidencePackFeature.Command(Guid.NewGuid(), "user"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SignEvidencePack_ReturnsError_WhenEvidencePackNotFound()
    {
        var instance = CreateInstance();
        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.GetByWorkflowInstanceIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns((EvidencePack?)null);

        var handler = new SignEvidencePackFeature.Handler(
            instanceRepo, packRepo,
            Substitute.For<IWorkflowUnitOfWork>(),
            CreateClock(), CreateConfigService());

        var result = await handler.Handle(
            new SignEvidencePackFeature.Command(instance.Id.Value, "user"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SignEvidencePack_ReturnsError_WhenAlreadySigned()
    {
        var instance = CreateInstance();
        var pack = CreateEvidencePack(instance.Id);
        pack.ApplyIntegritySignature("{}", "existinghash", "previous@user.com", FixedNow);

        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.GetByWorkflowInstanceIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(pack);

        var handler = new SignEvidencePackFeature.Handler(
            instanceRepo, packRepo,
            Substitute.For<IWorkflowUnitOfWork>(),
            CreateClock(), CreateConfigService());

        var result = await handler.Handle(
            new SignEvidencePackFeature.Command(instance.Id.Value, "newuser"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("evidence_pack.already_signed");
    }

    // ── VerifyEvidencePackIntegrity handler tests ─────────────────────────

    [Fact]
    public async Task VerifyIntegrity_ReturnsIsSigned_False_WhenNotSigned()
    {
        var instance = CreateInstance();
        var pack = CreateEvidencePack(instance.Id);

        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.GetByWorkflowInstanceIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(pack);

        var handler = new VerifyIntegrityFeature.Handler(instanceRepo, packRepo, CreateConfigService());
        var result = await handler.Handle(new VerifyIntegrityFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSigned.Should().BeFalse();
        result.Value.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyIntegrity_ReturnsIsValid_True_ForCorrectSignature()
    {
        var instance = CreateInstance();
        var pack = CreateEvidencePack(instance.Id);

        // Manually sign with known key
        var manifest = JsonSerializer.Serialize(new { test = "data" });
        var keyBytes = Encoding.UTF8.GetBytes(TestSigningKey);
        var hashBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(manifest));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        pack.ApplyIntegritySignature(manifest, hash, "auditor@test.com", FixedNow);

        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.GetByWorkflowInstanceIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(pack);

        var handler = new VerifyIntegrityFeature.Handler(instanceRepo, packRepo, CreateConfigService());
        var result = await handler.Handle(new VerifyIntegrityFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSigned.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.SignedBy.Should().Be("auditor@test.com");
    }

    [Fact]
    public async Task VerifyIntegrity_ReturnsIsValid_False_WhenManifestTampered()
    {
        var instance = CreateInstance();
        var pack = CreateEvidencePack(instance.Id);

        // Sign with original manifest
        var manifest = JsonSerializer.Serialize(new { test = "original" });
        var keyBytes = Encoding.UTF8.GetBytes(TestSigningKey);
        var hashBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(manifest));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Tamper: apply wrong manifest but keep original hash
        pack.ApplyIntegritySignature("{\"test\":\"tampered\"}", hash, "auditor@test.com", FixedNow);

        var instanceRepo = Substitute.For<IWorkflowInstanceRepository>();
        instanceRepo.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.GetByWorkflowInstanceIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(pack);

        var handler = new VerifyIntegrityFeature.Handler(instanceRepo, packRepo, CreateConfigService());
        var result = await handler.Handle(new VerifyIntegrityFeature.Query(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSigned.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.VerificationNote.Should().Contain("FAILED");
    }
}
