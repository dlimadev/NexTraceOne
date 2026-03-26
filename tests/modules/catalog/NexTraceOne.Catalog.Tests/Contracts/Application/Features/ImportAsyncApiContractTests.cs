using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ImportAsyncApiContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportAsyncApiContract.ImportAsyncApiContract;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="ImportAsyncApiContractFeature"/>.
/// Valida o workflow real de importação de contratos AsyncAPI: criação de ContractVersion com Protocol=AsyncApi
/// e população do EventContractDetail com metadados extraídos da spec.
/// </summary>
public sealed class ImportAsyncApiContractTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private const string ValidAsyncApi = """
        {
          "asyncapi": "2.6.0",
          "info": { "title": "UserEventService", "version": "1.0.0" },
          "defaultContentType": "application/json",
          "servers": { "production": { "url": "kafka.example.com:9092", "protocol": "kafka" } },
          "channels": {
            "user/signedup": { "publish": {} },
            "user/deleted": { "subscribe": {} }
          },
          "components": {
            "messages": {
              "UserSignedUp": {
                "payload": {
                  "properties": { "userId": { "type": "string" }, "email": { "type": "string" } }
                }
              }
            }
          }
        }
        """;

    private static IContractVersionRepository CreateVersionRepository() =>
        Substitute.For<IContractVersionRepository>();
    private static IEventContractDetailRepository CreateDetailRepository() =>
        Substitute.For<IEventContractDetailRepository>();
    private static IContractsUnitOfWork CreateUnitOfWork() =>
        Substitute.For<IContractsUnitOfWork>();
    private static IDateTimeProvider CreateDateTimeProvider()
    {
        var p = Substitute.For<IDateTimeProvider>();
        p.UtcNow.Returns(FixedNow);
        return p;
    }

    [Fact]
    public async Task Handle_Should_Create_ContractVersion_With_AsyncApi_Protocol()
    {
        var vRepo = CreateVersionRepository();
        var dRepo = CreateDetailRepository();
        var uow = CreateUnitOfWork();
        var dt = CreateDateTimeProvider();
        var sut = new ImportAsyncApiContractFeature.Handler(vRepo, dRepo, uow, dt);

        var result = await sut.Handle(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        vRepo.Received(1).Add(Arg.Is<ContractVersion>(v =>
            v.Protocol == ContractProtocol.AsyncApi && v.SemVer == "1.0.0"));
    }

    [Fact]
    public async Task Handle_Should_Create_EventContractDetail_With_Extracted_Title()
    {
        var vRepo = CreateVersionRepository();
        var dRepo = CreateDetailRepository();
        var uow = CreateUnitOfWork();
        var dt = CreateDateTimeProvider();
        var sut = new ImportAsyncApiContractFeature.Handler(vRepo, dRepo, uow, dt);

        await sut.Handle(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload"), CancellationToken.None);

        dRepo.Received(1).Add(Arg.Is<EventContractDetail>(d =>
            d.Title == "UserEventService"
            && d.AsyncApiVersion == "2.6.0"
            && d.DefaultContentType == "application/json"));
    }

    [Fact]
    public async Task Handle_Should_Extract_Channels_Into_Detail()
    {
        var vRepo = CreateVersionRepository();
        var dRepo = CreateDetailRepository();
        var uow = CreateUnitOfWork();
        var dt = CreateDateTimeProvider();
        var sut = new ImportAsyncApiContractFeature.Handler(vRepo, dRepo, uow, dt);

        var result = await sut.Handle(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ChannelsJson.Should().Contain("user/signedup");
        result.Value.ChannelsJson.Should().Contain("user/deleted");
    }

    [Fact]
    public async Task Handle_Should_Override_DefaultContentType_When_Provided()
    {
        var vRepo = CreateVersionRepository();
        var dRepo = CreateDetailRepository();
        var uow = CreateUnitOfWork();
        var dt = CreateDateTimeProvider();
        var sut = new ImportAsyncApiContractFeature.Handler(vRepo, dRepo, uow, dt);

        var result = await sut.Handle(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload",
            DefaultContentType: "application/avro"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DefaultContentType.Should().Be("application/avro");
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Version_Already_Exists()
    {
        var vRepo = CreateVersionRepository();
        vRepo.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), "1.0.0", Arg.Any<CancellationToken>())
            .Returns(ContractVersion.Import(Guid.NewGuid(), "1.0.0", ValidAsyncApi, "json", "upload", ContractProtocol.AsyncApi).Value);

        var dRepo = CreateDetailRepository();
        var uow = CreateUnitOfWork();
        var dt = CreateDateTimeProvider();
        var sut = new ImportAsyncApiContractFeature.Handler(vRepo, dRepo, uow, dt);

        var result = await sut.Handle(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyExists");
    }

    [Fact]
    public async Task Handle_Should_Commit_UnitOfWork()
    {
        var vRepo = CreateVersionRepository();
        var dRepo = CreateDetailRepository();
        var uow = CreateUnitOfWork();
        var dt = CreateDateTimeProvider();
        var sut = new ImportAsyncApiContractFeature.Handler(vRepo, dRepo, uow, dt);

        await sut.Handle(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload"), CancellationToken.None);

        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_Should_Fail_For_Non_AsyncApi_Content()
    {
        var validator = new ImportAsyncApiContractFeature.Validator();
        var result = validator.Validate(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: """{"openapi":"3.0.0"}""", ImportedFrom: "upload"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Command()
    {
        var validator = new ImportAsyncApiContractFeature.Validator();
        var result = validator.Validate(new ImportAsyncApiContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            AsyncApiContent: ValidAsyncApi, ImportedFrom: "upload"));

        result.IsValid.Should().BeTrue();
    }
}
