using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using SyncContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.SyncContracts.SyncContracts;
using ValidateContractIntegrityFeature = NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractIntegrity.ValidateContractIntegrity;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes para os handlers SyncContracts (integração externa em lote) e ValidateContractIntegrity.
/// Cobrem cenários de criação, idempotência (Skipped), falha isolada por item
/// e validação multi-protocolo de integridade estrutural.
/// </summary>
public sealed class SyncContractsTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string OpenApiSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";
    private const string SwaggerSpec = """{"swagger":"2.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";
    private const string AsyncApiSpec = """{"asyncapi":"2.6.0","info":{"title":"Test","version":"1.0.0"},"channels":{"user/created":{"subscribe":{"operationId":"onUserCreated","message":{"payload":{}}}}}}""";

    // ── SyncContracts ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncContracts_Should_CreateAllItems_When_NoVersionsExist()
    {
        // Arrange
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new SyncContractsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var assetId = Guid.NewGuid();
        var command = new SyncContractsFeature.Command(
            Items: [new(assetId, "1.0.0", OpenApiSpec, "json", "github-actions", ContractProtocol.OpenApi)],
            SourceSystem: "github-actions",
            CorrelationId: "test-run-001");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProcessed.Should().Be(1);
        result.Value.Created.Should().Be(1);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().Be(0);
        result.Value.CorrelationId.Should().Be("test-run-001");
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(SyncContractsFeature.SyncStatus.Created);
        repository.Received(1).Add(Arg.Any<ContractVersion>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncContracts_Should_SkipItem_When_VersionAlreadyExists()
    {
        // Arrange: versão já existe — deve ser marcada como Skipped por idempotência
        var assetId = Guid.NewGuid();
        var existing = ContractVersion.Import(assetId, "1.0.0", OpenApiSpec, "json", "upload").Value;

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new SyncContractsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(assetId, "1.0.0", Arg.Any<CancellationToken>())
            .Returns(existing);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var command = new SyncContractsFeature.Command(
            Items: [new(assetId, "1.0.0", OpenApiSpec, "json", "ci-pipeline", ContractProtocol.OpenApi)],
            SourceSystem: "jenkins",
            CorrelationId: null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Be(0);
        result.Value.Skipped.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        result.Value.Items[0].Status.Should().Be(SyncContractsFeature.SyncStatus.Skipped);
        result.Value.Items[0].ContractVersionId.Should().Be(existing.Id.Value);
        repository.DidNotReceive().Add(Arg.Any<ContractVersion>());
    }

    [Fact]
    public async Task SyncContracts_Should_IsolateFailure_When_OneItemFails()
    {
        // Arrange: segundo item tem conteúdo inválido — deve falhar sem bloquear o primeiro
        var assetId1 = Guid.NewGuid();
        var assetId2 = Guid.NewGuid();

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new SyncContractsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        // SpecContent inválido (vazio) deve causar falha no handler de domínio
        var command = new SyncContractsFeature.Command(
            Items: [
                new(assetId1, "1.0.0", OpenApiSpec, "json", "pipeline", ContractProtocol.OpenApi),
                new(assetId2, "1.0.0", string.Empty, "json", "pipeline", ContractProtocol.OpenApi), // inválido
            ],
            SourceSystem: "gitlab-ci",
            CorrelationId: "isolated-test");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert: primeiro item criado, segundo falhou — fault isolation correto
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProcessed.Should().Be(2);
        result.Value.Created.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        result.Value.Items[0].Status.Should().Be(SyncContractsFeature.SyncStatus.Created);
        result.Value.Items[1].Status.Should().Be(SyncContractsFeature.SyncStatus.Failed);
        result.Value.Items[1].ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SyncContracts_Should_DetectSwaggerProtocol_When_ProtocolNotExplicitlySet()
    {
        // Arrange: conteúdo Swagger com Protocol=OpenApi (padrão) deve ter protocolo auto-detectado
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new SyncContractsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        ContractVersion? addedVersion = null;
        repository.When(r => r.Add(Arg.Any<ContractVersion>()))
            .Do(call => addedVersion = call.Arg<ContractVersion>());

        var command = new SyncContractsFeature.Command(
            Items: [new(Guid.NewGuid(), "2.0.0", SwaggerSpec, "json", "api-gateway", ContractProtocol.OpenApi)],
            SourceSystem: "api-gateway",
            CorrelationId: null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert: protocolo deve ter sido detectado como Swagger, não OpenApi
        result.IsSuccess.Should().BeTrue();
        addedVersion.Should().NotBeNull();
        addedVersion!.Protocol.Should().Be(ContractProtocol.Swagger);
    }

    [Fact]
    public async Task SyncContracts_Should_DetectAsyncApiProtocol_When_ContentContainsAsyncApiKey()
    {
        // Arrange: conteúdo AsyncAPI deve ser auto-detectado
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new SyncContractsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        ContractVersion? addedVersion = null;
        repository.When(r => r.Add(Arg.Any<ContractVersion>()))
            .Do(call => addedVersion = call.Arg<ContractVersion>());

        var command = new SyncContractsFeature.Command(
            Items: [new(Guid.NewGuid(), "1.0.0", AsyncApiSpec, "json", "event-bus", ContractProtocol.OpenApi)],
            SourceSystem: "kafka-gateway",
            CorrelationId: null);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        addedVersion!.Protocol.Should().Be(ContractProtocol.AsyncApi);
    }

    [Fact]
    public async Task SyncContracts_Should_ProcessBatch_And_ReturnCorrectSummary()
    {
        // Arrange: lote misto — 2 novos, 1 já existente
        var assetId1 = Guid.NewGuid();
        var assetId2 = Guid.NewGuid();
        var assetId3 = Guid.NewGuid();
        var existingVersion = ContractVersion.Import(assetId3, "1.0.0", OpenApiSpec, "json", "upload").Value;

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IContractsUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new SyncContractsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(assetId1, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        repository.GetByApiAssetAndSemVerAsync(assetId2, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        repository.GetByApiAssetAndSemVerAsync(assetId3, "1.0.0", Arg.Any<CancellationToken>())
            .Returns(existingVersion);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var command = new SyncContractsFeature.Command(
            Items: [
                new(assetId1, "1.0.0", OpenApiSpec, "json", "ci", ContractProtocol.OpenApi),
                new(assetId2, "2.0.0", OpenApiSpec, "json", "ci", ContractProtocol.OpenApi),
                new(assetId3, "1.0.0", OpenApiSpec, "json", "ci", ContractProtocol.OpenApi),
            ],
            SourceSystem: "github-actions",
            CorrelationId: "batch-123");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProcessed.Should().Be(3);
        result.Value.Created.Should().Be(2);
        result.Value.Skipped.Should().Be(1);
        result.Value.Failed.Should().Be(0);
    }

    // ── ValidateContractIntegrity ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_Should_ReturnValid_ForOpenApiSpec()
    {
        // Arrange
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "upload").Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        // Act
        var result = await sut.Handle(new ValidateContractIntegrityFeature.Query(version.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ValidateContractIntegrity_Should_ReturnError_When_VersionNotFound()
    {
        // Arrange
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        // Act
        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task ValidateContractIntegrity_Should_ValidateSwaggerSpec()
    {
        // Arrange: spec Swagger 2.0
        var version = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0", SwaggerSpec, "json", "upload", ContractProtocol.Swagger).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        // Act
        var result = await sut.Handle(new ValidateContractIntegrityFeature.Query(version.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.SchemaVersion.Should().Be("2.0");
    }

    [Fact]
    public async Task ValidateContractIntegrity_Should_ValidateAsyncApiSpec()
    {
        // Arrange: spec AsyncAPI
        var version = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0", AsyncApiSpec, "json", "upload", ContractProtocol.AsyncApi).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);

        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        // Act
        var result = await sut.Handle(new ValidateContractIntegrityFeature.Query(version.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.SchemaVersion.Should().Be("2.6.0");
    }

    // ── SyncContracts Validator ────────────────────────────────────────────────

    [Fact]
    public void SyncContracts_Validator_Should_Reject_EmptyItems()
    {
        // Arrange
        var validator = new SyncContractsFeature.Validator();
        var command = new SyncContractsFeature.Command(
            Items: [],
            SourceSystem: "github-actions",
            CorrelationId: null);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Items"));
    }

    [Fact]
    public void SyncContracts_Validator_Should_Reject_MoreThan50Items()
    {
        // Arrange: 51 itens — excede o máximo de 50 por lote
        var items = Enumerable.Range(0, 51)
            .Select(_ => new SyncContractsFeature.ContractSyncItem(
                Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "ci", ContractProtocol.OpenApi))
            .ToList();

        var validator = new SyncContractsFeature.Validator();
        var command = new SyncContractsFeature.Command(
            Items: items,
            SourceSystem: "github-actions",
            CorrelationId: null);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SyncContracts_Validator_Should_Accept_ValidBatch()
    {
        // Arrange: lote válido com 3 itens
        var validator = new SyncContractsFeature.Validator();
        var command = new SyncContractsFeature.Command(
            Items: [
                new(Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "ci", ContractProtocol.OpenApi),
                new(Guid.NewGuid(), "2.0.0", SwaggerSpec, "json", "ci", ContractProtocol.Swagger),
            ],
            SourceSystem: "jenkins",
            CorrelationId: "run-001");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
