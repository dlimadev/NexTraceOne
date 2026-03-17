using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ImportContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportContract.ImportContract;
using CreateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateContractVersion.CreateContractVersion;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

public sealed class ProtocolAutoDetectionTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string OpenApiSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private const string SwaggerSpec = """{"swagger":"2.0","info":{"title":"Test","version":"1.0.0"},"basePath":"/api","paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private const string AsyncApiSpec = """{"asyncapi":"2.0.0","info":{"title":"User Events","version":"1.0.0"},"channels":{"user/created":{"publish":{"message":{"payload":{"type":"object","properties":{"userId":{"type":"string"}}}}}}}}""";

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

    // ── Validator: tamanho máximo de conteúdo ────────────────────

    [Fact]
    public void ImportContract_Validator_RejectsSpecContentExceedingMaxSize()
    {
        var validator = new ImportContractFeature.Validator();
        var oversized = new string('x', 5_242_881);
        var command = new ImportContractFeature.Command(
            Guid.NewGuid(), "1.0.0", oversized, "json", "upload");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpecContent");
    }

    [Fact]
    public void CreateContractVersion_Validator_RejectsSpecContentExceedingMaxSize()
    {
        var validator = new CreateContractVersionFeature.Validator();
        var oversized = new string('x', 5_242_881);
        var command = new CreateContractVersionFeature.Command(
            Guid.NewGuid(), "1.1.0", oversized, "json", "upload");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpecContent");
    }

    // ── Auto-detecção de protocolo ──────────────────────────────

    [Fact]
    public async Task ImportContract_AutoDetectsSwaggerProtocol()
    {
        var (repository, unitOfWork, dateTimeProvider) = CreateMocks();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);
        ContractVersion? captured = null;
        repository.When(r => r.Add(Arg.Any<ContractVersion>()))
            .Do(ci => captured = ci.Arg<ContractVersion>());

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", SwaggerSpec, "json", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.Swagger);
        captured.Should().NotBeNull();
        captured!.Protocol.Should().Be(ContractProtocol.Swagger);
    }

    [Fact]
    public async Task ImportContract_AutoDetectsAsyncApiProtocol()
    {
        var (repository, unitOfWork, dateTimeProvider) = CreateMocks();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);
        ContractVersion? captured = null;
        repository.When(r => r.Add(Arg.Any<ContractVersion>()))
            .Do(ci => captured = ci.Arg<ContractVersion>());

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", AsyncApiSpec, "json", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.AsyncApi);
        captured.Should().NotBeNull();
        captured!.Protocol.Should().Be(ContractProtocol.AsyncApi);
    }

    [Fact]
    public async Task ImportContract_AutoDetectsWsdlProtocol()
    {
        var (repository, unitOfWork, dateTimeProvider) = CreateMocks();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);
        ContractVersion? captured = null;
        repository.When(r => r.Add(Arg.Any<ContractVersion>()))
            .Do(ci => captured = ci.Arg<ContractVersion>());

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", WsdlSpec, "xml", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.Wsdl);
        captured.Should().NotBeNull();
        captured!.Protocol.Should().Be(ContractProtocol.Wsdl);
    }

    [Fact]
    public async Task ImportContract_KeepsOpenApiWhenNoAutoDetection()
    {
        var (repository, unitOfWork, dateTimeProvider) = CreateMocks();
        var sut = new ImportContractFeature.Handler(repository, unitOfWork, dateTimeProvider);
        ContractVersion? captured = null;
        repository.When(r => r.Add(Arg.Any<ContractVersion>()))
            .Do(ci => captured = ci.Arg<ContractVersion>());

        var result = await sut.Handle(
            new ImportContractFeature.Command(Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "upload"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Protocol.Should().Be(ContractProtocol.OpenApi);
        captured.Should().NotBeNull();
        captured!.Protocol.Should().Be(ContractProtocol.OpenApi);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static (IContractVersionRepository repository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider) CreateMocks()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        repository.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        return (repository, unitOfWork, dateTimeProvider);
    }
}
