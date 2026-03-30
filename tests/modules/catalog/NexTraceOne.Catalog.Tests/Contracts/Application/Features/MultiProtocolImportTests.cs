using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;

using ImportContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportContract.ImportContract;
using CreateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateContractVersion.CreateContractVersion;
using ComputeSemanticDiffFeature = NexTraceOne.Catalog.Application.Contracts.Features.ComputeSemanticDiff.ComputeSemanticDiff;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes de importação multi-protocolo: valida que contratos de todos os protocolos suportados
/// (OpenAPI, Swagger, WSDL, AsyncAPI) podem ser importados corretamente com o formato adequado.
/// Testa também a herança de protocolo ao criar novas versões e o diff semântico entre protocolos.
/// </summary>
public sealed class MultiProtocolImportTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string OpenApiSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private const string SwaggerSpec = """{"swagger":"2.0","info":{"title":"Test","version":"1.0.0"},"basePath":"/api","paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private const string AsyncApiSpec = """{"asyncapi":"2.0.0","info":{"title":"User Events","version":"1.0.0"},"channels":{"user/created":{"publish":{"message":{"payload":{"type":"object","properties":{"userId":{"type":"string"}}}}}}}}""";

    private static IContractsUnitOfWork CreateUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    private const string WsdlSpec =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
                     name="TestService">
          <portType name="TestPortType">
            <operation name="GetUser">
              <input message="tns:GetUserRequest"/>
              <output message="tns:GetUserResponse"/>
            </operation>
          </portType>
        </definitions>
        """;

    // ── ImportContract com protocolo explícito ───────────────────

    [Theory]
    [InlineData(ContractProtocol.OpenApi, "json")]
    [InlineData(ContractProtocol.Swagger, "json")]
    [InlineData(ContractProtocol.AsyncApi, "json")]
    public async Task ImportContract_Should_StoreProtocol_When_JsonProtocolSpecified(
        ContractProtocol protocol, string format)
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var spec = protocol switch
        {
            ContractProtocol.AsyncApi => AsyncApiSpec,
            ContractProtocol.Swagger => SwaggerSpec,
            _ => OpenApiSpec
        };

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", spec, format, "upload", protocol),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(protocol);
        result.Value.Format.Should().Be(format);
    }

    [Fact]
    public async Task ImportContract_Should_StoreWsdlProtocol_When_XmlFormatSpecified()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", WsdlSpec, "xml", "upload", ContractProtocol.Wsdl),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.Wsdl);
        result.Value.Format.Should().Be("xml");
    }

    [Fact]
    public async Task ImportContract_Should_DefaultToOpenApi_When_ProtocolNotSpecified()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi);
    }

    // ── CreateContractVersion herda protocolo ───────────────────

    [Fact]
    public async Task CreateContractVersion_Should_InheritProtocol_When_ProtocolNotSpecified()
    {
        var apiAssetId = Guid.NewGuid();
        var previous = ContractVersion.Import(apiAssetId, "1.0.0", WsdlSpec, "xml", "upload", ContractProtocol.Wsdl).Value;

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetLatestByApiAssetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(previous);
        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), "1.1.0", Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(apiAssetId, "1.1.0", WsdlSpec, "xml", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.Wsdl);
    }

    [Fact]
    public async Task CreateContractVersion_Should_UseExplicitProtocol_When_Specified()
    {
        var apiAssetId = Guid.NewGuid();
        var previous = ContractVersion.Import(apiAssetId, "1.0.0", OpenApiSpec, "json", "upload", ContractProtocol.OpenApi).Value;

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractVersionFeature.Handler(repository, unitOfWork, dateTimeProvider);

        repository.GetLatestByApiAssetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(previous);
        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), "2.0.0", Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateContractVersionFeature.Command(apiAssetId, "2.0.0", SwaggerSpec, "json", "upload", ContractProtocol.Swagger),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.Swagger);
    }

    // ── Diff multi-protocolo ────────────────────────────────────

    // ── Specs de teste para diff multi-protocolo ──────────────

    private const string BaseWsdlSpec =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/" name="TestService">
          <portType name="TestPortType">
            <operation name="GetUser">
              <input message="tns:GetUserRequest"/>
            </operation>
          </portType>
        </definitions>
        """;

    private const string AdditiveWsdlSpec =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/" name="TestService">
          <portType name="TestPortType">
            <operation name="GetUser">
              <input message="tns:GetUserRequest"/>
            </operation>
            <operation name="CreateUser">
              <input message="tns:CreateUserRequest"/>
            </operation>
          </portType>
        </definitions>
        """;

    private const string BaseAsyncApiSpec = """{"asyncapi":"2.0.0","info":{"title":"Events","version":"1.0.0"},"channels":{"user/created":{"publish":{"message":{"payload":{"type":"object","properties":{"userId":{"type":"string"}}}}}}}}""";
    private const string AdditiveAsyncApiSpec = """{"asyncapi":"2.0.0","info":{"title":"Events","version":"1.1.0"},"channels":{"user/created":{"publish":{"message":{"payload":{"type":"object","properties":{"userId":{"type":"string"}}}}}},"order/placed":{"subscribe":{"message":{"payload":{"type":"object","properties":{"orderId":{"type":"string"}}}}}}}}""";

    [Fact]
    public async Task ComputeSemanticDiff_Should_UseWsdlCalculator_WhenProtocolIsWsdl()
    {
        var apiAssetId = Guid.NewGuid();
        var baseVersion = ContractVersion.Import(apiAssetId, "1.0.0", BaseWsdlSpec, "xml", "upload", ContractProtocol.Wsdl).Value;
        var targetVersion = ContractVersion.Import(apiAssetId, "1.1.0", AdditiveWsdlSpec, "xml", "upload", ContractProtocol.Wsdl).Value;

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var eventBus = Substitute.For<IEventBus>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        var sut = new ComputeSemanticDiffFeature.Handler(repository, apiAssetRepository, unitOfWork, dateTimeProvider, eventBus, currentUser, currentTenant);

        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ComputeSemanticDiffFeature.Query(baseVersion.Id.Value, targetVersion.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.Value.AdditiveChanges.Should().Contain(c => c.Description.Contains("CreateUser"));
    }

    [Fact]
    public async Task ComputeSemanticDiff_Should_UseAsyncApiCalculator_WhenProtocolIsAsyncApi()
    {
        var apiAssetId = Guid.NewGuid();
        var baseVersion = ContractVersion.Import(apiAssetId, "1.0.0", BaseAsyncApiSpec, "json", "upload", ContractProtocol.AsyncApi).Value;
        var targetVersion = ContractVersion.Import(apiAssetId, "1.1.0", AdditiveAsyncApiSpec, "json", "upload", ContractProtocol.AsyncApi).Value;

        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var eventBus = Substitute.For<IEventBus>();
        var currentUser = Substitute.For<ICurrentUser>();
        var currentTenant = Substitute.For<ICurrentTenant>();
        var sut = new ComputeSemanticDiffFeature.Handler(repository, apiAssetRepository, unitOfWork, dateTimeProvider, eventBus, currentUser, currentTenant);

        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == baseVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(baseVersion);
        repository.GetByIdAsync(Arg.Is<ContractVersionId>(id => id.Value == targetVersion.Id.Value), Arg.Any<CancellationToken>())
            .Returns(targetVersion);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ComputeSemanticDiffFeature.Query(baseVersion.Id.Value, targetVersion.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChangeLevel.Should().Be(ChangeLevel.Additive);
        result.Value.AdditiveChanges.Should().Contain(c => c.Description.Contains("order/placed"));
    }

    // ── Validação de formato ────────────────────────────────────

    [Fact]
    public void ImportValidator_Should_AcceptXmlFormat()
    {
        var validator = new ImportContractFeature.Validator();
        var command = new ImportContractFeature.Command(
            Guid.NewGuid(), "1.0.0", WsdlSpec, "xml", "upload", ContractProtocol.Wsdl);

        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ImportValidator_Should_AcceptJsonFormat()
    {
        var validator = new ImportContractFeature.Validator();
        var command = new ImportContractFeature.Command(
            Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "upload");

        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ImportValidator_Should_AcceptYamlFormat()
    {
        var validator = new ImportContractFeature.Validator();
        var command = new ImportContractFeature.Command(
            Guid.NewGuid(), "1.0.0", "openapi: 3.0.0", "yaml", "upload");

        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ImportValidator_Should_RejectInvalidFormat()
    {
        var validator = new ImportContractFeature.Validator();
        var command = new ImportContractFeature.Command(
            Guid.NewGuid(), "1.0.0", "content", "txt", "upload");

        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public void CreateVersionValidator_Should_AcceptXmlFormat()
    {
        var validator = new CreateContractVersionFeature.Validator();
        var command = new CreateContractVersionFeature.Command(
            Guid.NewGuid(), "1.1.0", WsdlSpec, "xml", "upload", ContractProtocol.Wsdl);

        var validationResult = validator.Validate(command);
        validationResult.IsValid.Should().BeTrue();
    }

    // ── Canonicalization XML ────────────────────────────────────

    [Fact]
    public void ContractCanonicalizer_Should_NormalizeXml()
    {
        var xml = "<root>\r\n  <child />   \r\n</root>";
        var canonical = ContractCanonicalizer.Canonicalize(xml, "xml");

        canonical.Should().NotContain("\r");
        canonical.Should().NotEndWith("   ");
    }

    [Fact]
    public void ContractCanonicalizer_Should_NormalizeJson()
    {
        var json1 = """{ "b": 2, "a": 1 }""";
        var json2 = """{ "a": 1, "b": 2 }""";

        var canonical1 = ContractCanonicalizer.Canonicalize(json1, "json");
        var canonical2 = ContractCanonicalizer.Canonicalize(json2, "json");

        canonical1.Should().Be(canonical2);
    }
}
