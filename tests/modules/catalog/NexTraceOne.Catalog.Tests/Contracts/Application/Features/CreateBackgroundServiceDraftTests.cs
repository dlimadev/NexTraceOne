using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using CreateBackgroundServiceDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateBackgroundServiceDraft.CreateBackgroundServiceDraft;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="CreateBackgroundServiceDraftFeature"/>.
/// Valida o workflow de criação de draft de Background Service: ContractDraft com tipo BackgroundService
/// e BackgroundServiceDraftMetadata com os metadados do processo.
/// </summary>
public sealed class CreateBackgroundServiceDraftTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractDraftRepository CreateDraftRepo() => Substitute.For<IContractDraftRepository>();
    private static IBackgroundServiceDraftMetadataRepository CreateMetaRepo() => Substitute.For<IBackgroundServiceDraftMetadataRepository>();
    private static readonly Guid TestServiceId = Guid.NewGuid();
    private static IServiceAssetRepository CreateServiceRepo()
    {
        var service = ServiceAsset.Create("TestService", "test-domain", "test-team", Guid.NewGuid());
        service.UpdateDetails("TestService", "", ServiceType.BackgroundService, "", Criticality.Low, LifecycleStatus.Planning, ExposureType.Internal, "", "");
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
    public async Task Handle_Should_Create_Draft_With_BackgroundService_Type()
    {
        var draftRepo = CreateDraftRepo();
        var metaRepo = CreateMetaRepo();
        var sut = new CreateBackgroundServiceDraftFeature.Handler(draftRepo, CreateServiceRepo(), metaRepo, CreateUow(), CreateDt());

        var result = await sut.Handle(new CreateBackgroundServiceDraftFeature.Command(
            Title: "Order Expiration Job", Author: "dev@example.com",
            ServiceName: "OrderExpirationJob", Category: "Job", TriggerType: "Cron"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        draftRepo.Received(1).Add(Arg.Is<ContractDraft>(d =>
            d.ContractType == ContractType.BackgroundService
            && d.Title == "Order Expiration Job"));
    }

    [Fact]
    public async Task Handle_Should_Create_BackgroundServiceDraftMetadata()
    {
        var draftRepo = CreateDraftRepo();
        var metaRepo = CreateMetaRepo();
        var sut = new CreateBackgroundServiceDraftFeature.Handler(draftRepo, CreateServiceRepo(), metaRepo, CreateUow(), CreateDt());

        await sut.Handle(new CreateBackgroundServiceDraftFeature.Command(
            Title: "Report Worker", Author: "dev@example.com",
            ServiceName: "ReportWorker", Category: "Worker",
            TriggerType: "Continuous"), CancellationToken.None);

        metaRepo.Received(1).Add(Arg.Is<BackgroundServiceDraftMetadata>(m =>
            m.ServiceName == "ReportWorker"
            && m.Category == "Worker"
            && m.TriggerType == "Continuous"));
    }

    [Fact]
    public async Task Handle_Should_Return_DraftId_ServiceName_Category_In_Response()
    {
        var sut = new CreateBackgroundServiceDraftFeature.Handler(CreateDraftRepo(), CreateServiceRepo(), CreateMetaRepo(), CreateUow(), CreateDt());

        var result = await sut.Handle(new CreateBackgroundServiceDraftFeature.Command(
            Title: "Nightly Exporter", Author: "dev@example.com",
            ServiceName: "NightlyExporter", Category: "Exporter",
            TriggerType: "Cron", ScheduleExpression: "0 3 * * *"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DraftId.Should().NotBeEmpty();
        result.Value.ServiceName.Should().Be("NightlyExporter");
        result.Value.Category.Should().Be("Exporter");
        result.Value.ScheduleExpression.Should().Be("0 3 * * *");
    }

    [Fact]
    public async Task Handle_Should_Commit_UnitOfWork()
    {
        var uow = CreateUow();
        var sut = new CreateBackgroundServiceDraftFeature.Handler(CreateDraftRepo(), CreateServiceRepo(), CreateMetaRepo(), uow, CreateDt());

        await sut.Handle(new CreateBackgroundServiceDraftFeature.Command(
            Title: "Test", Author: "dev@example.com",
            ServiceName: "TestJob"), CancellationToken.None);

        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_Should_Fail_For_Empty_ServiceName()
    {
        var validator = new CreateBackgroundServiceDraftFeature.Validator();
        var result = validator.Validate(new CreateBackgroundServiceDraftFeature.Command(
            Title: "My Draft", Author: "dev@example.com", ServiceName: string.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_Command()
    {
        var validator = new CreateBackgroundServiceDraftFeature.Validator();
        var result = validator.Validate(new CreateBackgroundServiceDraftFeature.Command(
            Title: "Order Expiration", Author: "dev@example.com",
            ServiceName: "OrderExpirationJob", Category: "Job", TriggerType: "OnDemand",
            ServiceId: TestServiceId));

        result.IsValid.Should().BeTrue();
    }
}
