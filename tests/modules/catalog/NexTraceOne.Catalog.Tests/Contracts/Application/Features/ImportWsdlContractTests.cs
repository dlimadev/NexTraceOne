using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ImportWsdlContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportWsdlContract.ImportWsdlContract;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="ImportWsdlContractFeature"/>.
/// Valida o workflow real de importação de contratos WSDL: criação de ContractVersion com Protocol=Wsdl
/// e população do SoapContractDetail com metadados extraídos do WSDL.
/// </summary>
public sealed class ImportWsdlContractTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string ValidWsdl = """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
                     xmlns:tns="http://example.com/users"
                     name="UserService"
                     targetNamespace="http://example.com/users">
          <portType name="UserServicePort">
            <operation name="GetUser">
              <input message="tns:GetUserRequest"/>
              <output message="tns:GetUserResponse"/>
            </operation>
            <operation name="CreateUser">
              <input message="tns:CreateUserRequest"/>
              <output message="tns:CreateUserResponse"/>
            </operation>
          </portType>
          <binding name="UserServiceBinding" type="tns:UserServicePort">
            <soap:binding style="document" transport="http://schemas.xmlsoap.org/soap/http"/>
          </binding>
          <service name="UserService">
            <port name="UserServicePort" binding="tns:UserServiceBinding">
              <soap:address location="http://example.com/users/service"/>
            </port>
          </service>
        </definitions>
        """;

    private static IContractVersionRepository CreateVersionRepository() =>
        Substitute.For<IContractVersionRepository>();

    private static ISoapContractDetailRepository CreateDetailRepository() =>
        Substitute.For<ISoapContractDetailRepository>();

    private static IContractsUnitOfWork CreateUnitOfWork() =>
        Substitute.For<IContractsUnitOfWork>();

    private static IDateTimeProvider CreateDateTimeProvider()
    {
        var provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(FixedNow);
        return provider;
    }

    [Fact]
    public async Task Handle_Should_Create_ContractVersion_With_Wsdl_Protocol()
    {
        var versionRepo = CreateVersionRepository();
        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("1.0.0");
        versionRepo.Received(1).Add(Arg.Is<ContractVersion>(v =>
            v.Protocol == ContractProtocol.Wsdl && v.SemVer == "1.0.0"));
    }

    [Fact]
    public async Task Handle_Should_Create_SoapContractDetail_With_Extracted_Metadata()
    {
        var versionRepo = CreateVersionRepository();
        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload");

        await sut.Handle(command, CancellationToken.None);

        detailRepo.Received(1).Add(Arg.Is<SoapContractDetail>(d =>
            d.ServiceName == "UserService"
            && d.TargetNamespace == "http://example.com/users"
            && d.SoapVersion == "1.1"));
    }

    [Fact]
    public async Task Handle_Should_Extract_Operations_Into_Detail()
    {
        var versionRepo = CreateVersionRepository();
        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExtractedOperationsJson.Should().Contain("GetUser");
        result.Value.ExtractedOperationsJson.Should().Contain("CreateUser");
    }

    [Fact]
    public async Task Handle_Should_Use_Explicit_EndpointUrl_Over_Extracted()
    {
        var versionRepo = CreateVersionRepository();
        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload",
            EndpointUrl: "http://production.company.com/users");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EndpointUrl.Should().Be("http://production.company.com/users");
    }

    [Fact]
    public async Task Handle_Should_Override_SoapVersion_When_Provided()
    {
        var versionRepo = CreateVersionRepository();
        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload",
            SoapVersion: "1.2");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SoapVersion.Should().Be("1.2");
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Version_Already_Exists()
    {
        var versionRepo = CreateVersionRepository();
        versionRepo.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), "1.0.0", Arg.Any<CancellationToken>())
            .Returns(ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidWsdl, "xml", "upload", ContractProtocol.Wsdl).Value);

        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyExists");
    }

    [Fact]
    public async Task Handle_Should_Commit_UnitOfWork()
    {
        var versionRepo = CreateVersionRepository();
        var detailRepo = CreateDetailRepository();
        var unitOfWork = CreateUnitOfWork();
        var dateTimeProvider = CreateDateTimeProvider();
        var sut = new ImportWsdlContractFeature.Handler(versionRepo, detailRepo, unitOfWork, dateTimeProvider);

        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload");

        await sut.Handle(command, CancellationToken.None);

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Validator tests ──────────────────────────────────────────────

    [Fact]
    public void Validator_Should_Fail_For_Non_Xml_Content()
    {
        var validator = new ImportWsdlContractFeature.Validator();
        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: """{"openapi":"3.0.0"}""",
            ImportedFrom: "upload");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Fail_For_Invalid_SoapVersion()
    {
        var validator = new ImportWsdlContractFeature.Validator();
        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload",
            SoapVersion: "3.0");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Command()
    {
        var validator = new ImportWsdlContractFeature.Validator();
        var command = new ImportWsdlContractFeature.Command(
            ApiAssetId: Guid.NewGuid(),
            SemVer: "1.0.0",
            WsdlContent: ValidWsdl,
            ImportedFrom: "upload");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
