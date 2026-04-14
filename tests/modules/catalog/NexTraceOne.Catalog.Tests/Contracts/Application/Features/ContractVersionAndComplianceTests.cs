using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using CreateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateContractVersion.CreateContractVersion;
using DeprecateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.DeprecateContractVersion.DeprecateContractVersion;
using ComputeSemanticDiffFeature = NexTraceOne.Catalog.Application.Contracts.Features.ComputeSemanticDiff.ComputeSemanticDiff;
using EvaluateContractComplianceFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractCompliance.EvaluateContractCompliance;
using CreateComplianceGateFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateComplianceGate.CreateComplianceGate;
using EvaluateContractRulesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractRules.EvaluateContractRules;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes dos handlers de versionamento, deprecação, diff semântico, compliance e regras
/// na camada Application do módulo Contracts.
/// Cobre CreateContractVersion, DeprecateContractVersion, ComputeSemanticDiff,
/// EvaluateContractCompliance, CreateComplianceGate e EvaluateContractRules.
/// </summary>
public sealed class ContractVersionAndComplianceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractsUnitOfWork CreateContractsUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    // ── CreateContractVersion ─────────────────────────────────────────

    [Fact]
    public async Task CreateContractVersion_Should_ReturnResponse_When_PreviousVersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var apiAssetId = Guid.NewGuid();
        var previous = ContractVersion.Import(apiAssetId, "1.0.0", "openapi: 3.0.0", "yaml", "import", ContractProtocol.OpenApi).Value;

        repository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(previous);
        repository.GetByApiAssetAndSemVerAsync(apiAssetId, "1.1.0", Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(apiAssetId, "1.1.0", "openapi: 3.1.0", "yaml", "import"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("1.1.0");
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi);
        repository.Received(1).Add(Arg.Any<ContractVersion>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContractVersion_Should_ReturnFailure_When_NoPreviousVersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var apiAssetId = Guid.NewGuid();
        repository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(apiAssetId, "1.0.0", "content", "json", "import"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NoPreviousVersion");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContractVersion_Should_ReturnFailure_When_VersionAlreadyExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var apiAssetId = Guid.NewGuid();
        var previous = ContractVersion.Import(apiAssetId, "1.0.0", "openapi: 3.0.0", "yaml", "import").Value;
        var existing = ContractVersion.Import(apiAssetId, "1.1.0", "openapi: 3.1.0", "yaml", "import").Value;

        repository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(previous);
        repository.GetByApiAssetAndSemVerAsync(apiAssetId, "1.1.0", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(apiAssetId, "1.1.0", "content", "yaml", "import"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyExists");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContractVersion_Validator_Should_Fail_When_SemVerIsEmpty()
    {
        var validator = new CreateContractVersionFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateContractVersionFeature.Command(Guid.NewGuid(), "", "content", "json", "import"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "SemVer");
    }

    // ── DeprecateContractVersion ──────────────────────────────────────

    [Fact]
    public async Task DeprecateContractVersion_Should_ReturnResponse_When_VersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new DeprecateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", "openapi: 3.0.0", "yaml", "import").Value;

        // Transiciona para um estado que pode ser deprecated (Approved)
        version.TransitionTo(ContractLifecycleState.InReview, FixedNow);
        version.TransitionTo(ContractLifecycleState.Approved, FixedNow);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(version);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sunsetDate = FixedNow.AddMonths(6);
        var result = await sut.Handle(
            new DeprecateContractVersionFeature.Command(version.Id.Value, "Use v2 instead", sunsetDate),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(version.Id.Value);
        result.Value.DeprecationNotice.Should().Be("Use v2 instead");
        result.Value.SunsetDate.Should().Be(sunsetDate);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeprecateContractVersion_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new DeprecateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new DeprecateContractVersionFeature.Command(Guid.NewGuid(), "Use v2 instead", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeprecateContractVersion_Validator_Should_Fail_When_NoticeIsEmpty()
    {
        var validator = new DeprecateContractVersionFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new DeprecateContractVersionFeature.Command(Guid.NewGuid(), "", null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "DeprecationNotice");
    }

    // ── ComputeSemanticDiff ───────────────────────────────────────────

    [Fact]
    public async Task ComputeSemanticDiff_Should_ReturnNotFound_When_BaseVersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var eventBus = Substitute.For<IEventBus>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        var sut = new ComputeSemanticDiffFeature.Handler(
            repository, apiAssetRepository, unitOfWork, dateTimeProvider, eventBus, currentUser, currentTenant);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new ComputeSemanticDiffFeature.Query(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }

    [Fact]
    public async Task ComputeSemanticDiff_Should_ReturnProtocolMismatch_When_ProtocolsDiffer()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var eventBus = Substitute.For<IEventBus>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        var sut = new ComputeSemanticDiffFeature.Handler(
            repository, apiAssetRepository, unitOfWork, dateTimeProvider, eventBus, currentUser, currentTenant);

        var apiAssetId = Guid.NewGuid();
        var baseVersion = ContractVersion.Import(apiAssetId, "1.0.0", "openapi: 3.0.0", "yaml", "import", ContractProtocol.OpenApi).Value;
        var targetVersion = ContractVersion.Import(apiAssetId, "2.0.0", "<wsdl/>", "xml", "import", ContractProtocol.Wsdl).Value;

        repository.GetByIdAsync(baseVersion.Id, Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repository.GetByIdAsync(targetVersion.Id, Arg.Any<CancellationToken>())
            .Returns(targetVersion);

        var result = await sut.Handle(
            new ComputeSemanticDiffFeature.Query(baseVersion.Id.Value, targetVersion.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractDiff.ProtocolMismatch");
    }

    [Fact]
    public async Task ComputeSemanticDiff_Validator_Should_Fail_When_BaseVersionIdIsEmpty()
    {
        var validator = new ComputeSemanticDiffFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new ComputeSemanticDiffFeature.Query(Guid.Empty, Guid.NewGuid()));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "BaseVersionId");
    }

    // ── EvaluateContractCompliance ────────────────────────────────────

    [Fact]
    public async Task EvaluateContractCompliance_Should_ReturnResponse_When_GateExists()
    {
        var gateRepository = Substitute.For<IContractComplianceGateRepository>();
        var resultRepository = Substitute.For<IContractComplianceResultRepository>();
        var unitOfWork = CreateContractsUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new EvaluateContractComplianceFeature.Handler(gateRepository, resultRepository, unitOfWork, clock);

        var gate = ContractComplianceGate.Create(
            "Security Gate", "Check security", "rules-json", ComplianceGateScope.Organization,
            "org-1", true, "admin", FixedNow, "tenant-1");

        gateRepository.GetByIdAsync(Arg.Any<ContractComplianceGateId>(), Arg.Any<CancellationToken>())
            .Returns(gate);
        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new EvaluateContractComplianceFeature.Command(
                gate.Id.Value, "cv-123", null, ComplianceEvaluationResult.Pass, null, null, "tenant-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GateId.Should().Be(gate.Id.Value);
        result.Value.Result.Should().Be(ComplianceEvaluationResult.Pass);
        await resultRepository.Received(1).AddAsync(Arg.Any<ContractComplianceResult>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateContractCompliance_Should_ReturnNotFound_When_GateDoesNotExist()
    {
        var gateRepository = Substitute.For<IContractComplianceGateRepository>();
        var resultRepository = Substitute.For<IContractComplianceResultRepository>();
        var unitOfWork = CreateContractsUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new EvaluateContractComplianceFeature.Handler(gateRepository, resultRepository, unitOfWork, clock);

        gateRepository.GetByIdAsync(Arg.Any<ContractComplianceGateId>(), Arg.Any<CancellationToken>())
            .Returns((ContractComplianceGate?)null);

        var result = await sut.Handle(
            new EvaluateContractComplianceFeature.Command(
                Guid.NewGuid(), "cv-123", null, ComplianceEvaluationResult.Pass, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ComplianceGate.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── CreateComplianceGate ──────────────────────────────────────────

    [Fact]
    public async Task CreateComplianceGate_Should_ReturnResponse_When_ValidCommand()
    {
        var repository = Substitute.For<IContractComplianceGateRepository>();
        var unitOfWork = CreateContractsUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateComplianceGateFeature.Handler(repository, unitOfWork, clock);

        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateComplianceGateFeature.Command(
                "Security Gate", "Checks security rules", "rules-json",
                ComplianceGateScope.Organization, "org-1", true, "admin", "tenant-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Security Gate");
        result.Value.Scope.Should().Be(ComplianceGateScope.Organization);
        result.Value.BlockOnViolation.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        await repository.Received(1).AddAsync(Arg.Any<ContractComplianceGate>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateComplianceGate_Validator_Should_Fail_When_NameIsEmpty()
    {
        var validator = new CreateComplianceGateFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateComplianceGateFeature.Command(
                "", null, null, ComplianceGateScope.Organization, "org-1", true, null, null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateComplianceGate_Validator_Should_Fail_When_ScopeIdIsEmpty()
    {
        var validator = new CreateComplianceGateFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateComplianceGateFeature.Command(
                "Gate", null, null, ComplianceGateScope.Organization, "", true, null, null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ScopeId");
    }

    // ── EvaluateContractRules ─────────────────────────────────────────

    [Fact]
    public async Task EvaluateContractRules_Should_ReturnResponse_When_VersionExists()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new EvaluateContractRulesFeature.Handler(repository, dateTimeProvider);

        var version = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0",
            """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{}}""",
            "json", "import", ContractProtocol.OpenApi).Value;

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(version);

        var result = await sut.Handle(
            new EvaluateContractRulesFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(version.Id.Value);
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi.ToString());
        result.Value.TotalViolations.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task EvaluateContractRules_Should_ReturnNotFound_When_VersionDoesNotExist()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new EvaluateContractRulesFeature.Handler(repository, dateTimeProvider);

        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var result = await sut.Handle(
            new EvaluateContractRulesFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.NotFound");
    }

    [Fact]
    public async Task EvaluateContractRules_Validator_Should_Fail_When_ContractVersionIdIsEmpty()
    {
        var validator = new EvaluateContractRulesFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new EvaluateContractRulesFeature.Query(Guid.Empty));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ContractVersionId");
    }
}
