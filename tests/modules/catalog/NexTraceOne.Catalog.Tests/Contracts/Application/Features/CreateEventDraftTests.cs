using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using CreateEventDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateEventDraft.CreateEventDraft;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="CreateEventDraftFeature"/>.
/// Valida o workflow de criação de draft AsyncAPI: ContractDraft com tipo Event/AsyncApi
/// e EventDraftMetadata com os metadados de evento.
/// </summary>
public sealed class CreateEventDraftTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractDraftRepository CreateDraftRepo() => Substitute.For<IContractDraftRepository>();
    private static IEventDraftMetadataRepository CreateMetaRepo() => Substitute.For<IEventDraftMetadataRepository>();
    private static readonly Guid TestServiceId = Guid.NewGuid();
    private static IServiceAssetRepository CreateServiceRepo()
    {
        var service = ServiceAsset.Create("TestService", "test-domain", "test-team");
        service.UpdateDetails("TestService", "", ServiceType.KafkaProducer, "", Criticality.Low, LifecycleStatus.Planning, ExposureType.Internal, "", "");
        var repo = Substitute.For<IServiceAssetRepository>();
        repo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);
        return repo;
    }
    private static IContractsUnitOfWork CreateUow() => Substitute.For<IContractsUnitOfWork>();
    private static IDateTimeProvider CreateDt()
    {
        var p = Substitute.For<IDateTimeProvider>();
        p.UtcNow.Returns(FixedNow);
        return p;
    }

    [Fact]
    public async Task Handle_Should_Create_Draft_With_Event_Type_And_AsyncApi_Protocol()
    {
        var draftRepo = CreateDraftRepo();
        var metaRepo = CreateMetaRepo();
        var sut = new CreateEventDraftFeature.Handler(draftRepo, CreateServiceRepo(), metaRepo, CreateUow(), CreateDt());

        var result = await sut.Handle(new CreateEventDraftFeature.Command(
            Title: "Payment Events", Author: "dev@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draftRepo.Received(1).Add(Arg.Is<ContractDraft>(d =>
            d.ContractType == ContractType.Event
            && d.Protocol == ContractProtocol.AsyncApi
            && d.Title == "Payment Events"));
    }

    [Fact]
    public async Task Handle_Should_Create_EventDraftMetadata()
    {
        var draftRepo = CreateDraftRepo();
        var metaRepo = CreateMetaRepo();
        var sut = new CreateEventDraftFeature.Handler(draftRepo, CreateServiceRepo(), metaRepo, CreateUow(), CreateDt());

        await sut.Handle(new CreateEventDraftFeature.Command(
            Title: "Order Events", Author: "dev@example.com",
            AsyncApiVersion: "3.0.0",
            DefaultContentType: "application/avro"), CancellationToken.None);

        metaRepo.Received(1).Add(Arg.Is<EventDraftMetadata>(m =>
            m.Title == "Order Events"
            && m.AsyncApiVersion == "3.0.0"
            && m.DefaultContentType == "application/avro"));
    }

    [Fact]
    public async Task Handle_Should_Return_DraftId_And_AsyncApiVersion_In_Response()
    {
        var draftRepo = CreateDraftRepo();
        var metaRepo = CreateMetaRepo();
        var sut = new CreateEventDraftFeature.Handler(draftRepo, CreateServiceRepo(), metaRepo, CreateUow(), CreateDt());

        var result = await sut.Handle(new CreateEventDraftFeature.Command(
            Title: "Test Events", Author: "dev@example.com",
            AsyncApiVersion: "2.6.0"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.Title.Should().Be("Test Events");
        result.Value.AsyncApiVersion.Should().Be("2.6.0");
    }

    [Fact]
    public async Task Handle_Should_Commit_UnitOfWork()
    {
        var uow = CreateUow();
        var sut = new CreateEventDraftFeature.Handler(CreateDraftRepo(), CreateServiceRepo(), CreateMetaRepo(), uow, CreateDt());

        await sut.Handle(new CreateEventDraftFeature.Command(
            Title: "Test", Author: "dev@example.com"), CancellationToken.None);

        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Command()
    {
        var validator = new CreateEventDraftFeature.Validator();
        var result = validator.Validate(new CreateEventDraftFeature.Command(
            Title: "Payment Events", Author: "dev@example.com",
            AsyncApiVersion: "2.6.0", DefaultContentType: "application/json",
            ServiceId: TestServiceId));

        result.IsValid.Should().BeTrue();
    }
}
