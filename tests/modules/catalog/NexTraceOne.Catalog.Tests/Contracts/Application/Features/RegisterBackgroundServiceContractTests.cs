using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using RegisterBackgroundServiceContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.RegisterBackgroundServiceContract.RegisterBackgroundServiceContract;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="RegisterBackgroundServiceContractFeature"/>.
/// Valida o workflow de registo de Background Service Contract: criação de ContractVersion
/// com ContractType=BackgroundService e população do BackgroundServiceContractDetail.
/// </summary>
public sealed class RegisterBackgroundServiceContractTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractVersionRepository CreateVersionRepo() => Substitute.For<IContractVersionRepository>();
    private static IBackgroundServiceContractDetailRepository CreateDetailRepo() => Substitute.For<IBackgroundServiceContractDetailRepository>();
    private static IContractsUnitOfWork CreateUow() => Substitute.For<IContractsUnitOfWork>();
    private static IDateTimeProvider CreateDt()
    {
        var p = Substitute.For<IDateTimeProvider>();
        p.UtcNow.Returns(FixedNow);
        return p;
    }

    [Fact]
    public async Task Handle_Should_Create_ContractVersion()
    {
        var vRepo = CreateVersionRepo();
        var dRepo = CreateDetailRepo();
        var sut = new RegisterBackgroundServiceContractFeature.Handler(vRepo, dRepo, CreateUow(), CreateDt());

        var result = await sut.Handle(new RegisterBackgroundServiceContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            ServiceName: "OrderExpirationJob", Category: "Job", TriggerType: "Cron",
            ScheduleExpression: "0 * * * *"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        vRepo.Received(1).Add(Arg.Is<ContractVersion>(v => v.SemVer == "1.0.0"));
    }

    [Fact]
    public async Task Handle_Should_Create_BackgroundServiceContractDetail()
    {
        var vRepo = CreateVersionRepo();
        var dRepo = CreateDetailRepo();
        var sut = new RegisterBackgroundServiceContractFeature.Handler(vRepo, dRepo, CreateUow(), CreateDt());

        await sut.Handle(new RegisterBackgroundServiceContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            ServiceName: "NightlyProcessor", Category: "Processor",
            TriggerType: "Cron", ScheduleExpression: "0 2 * * *",
            AllowsConcurrency: false), CancellationToken.None);

        dRepo.Received(1).Add(Arg.Is<BackgroundServiceContractDetail>(d =>
            d.ServiceName == "NightlyProcessor"
            && d.Category == "Processor"
            && d.TriggerType == "Cron"
            && d.ScheduleExpression == "0 2 * * *"));
    }

    [Fact]
    public async Task Handle_Should_Return_Correct_Response()
    {
        var vRepo = CreateVersionRepo();
        var dRepo = CreateDetailRepo();
        var sut = new RegisterBackgroundServiceContractFeature.Handler(vRepo, dRepo, CreateUow(), CreateDt());

        var result = await sut.Handle(new RegisterBackgroundServiceContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "2.0.0",
            ServiceName: "ReportWorker", Category: "Worker",
            TriggerType: "Continuous"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("ReportWorker");
        result.Value.Category.Should().Be("Worker");
        result.Value.TriggerType.Should().Be("Continuous");
        result.Value.RegisteredAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Version_Already_Exists()
    {
        var vRepo = CreateVersionRepo();
        vRepo.GetByApiAssetAndSemVerAsync(Arg.Any<Guid>(), "1.0.0", Arg.Any<CancellationToken>())
            .Returns(ContractVersion.Import(Guid.NewGuid(), "1.0.0", "{}", "json", "manual", ContractProtocol.OpenApi).Value);

        var sut = new RegisterBackgroundServiceContractFeature.Handler(vRepo, CreateDetailRepo(), CreateUow(), CreateDt());

        var result = await sut.Handle(new RegisterBackgroundServiceContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            ServiceName: "Job", Category: "Job", TriggerType: "OnDemand"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.ContractVersion.AlreadyExists");
    }

    [Fact]
    public async Task Handle_Should_Commit_UnitOfWork()
    {
        var uow = CreateUow();
        var sut = new RegisterBackgroundServiceContractFeature.Handler(CreateVersionRepo(), CreateDetailRepo(), uow, CreateDt());

        await sut.Handle(new RegisterBackgroundServiceContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            ServiceName: "Job", Category: "Job", TriggerType: "OnDemand"), CancellationToken.None);

        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_Should_Fail_For_Invalid_TriggerType()
    {
        var validator = new RegisterBackgroundServiceContractFeature.Validator();
        var result = validator.Validate(new RegisterBackgroundServiceContractFeature.Command(
            ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
            ServiceName: "Job", Category: "Job", TriggerType: "InvalidType"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_All_Valid_TriggerTypes()
    {
        var validator = new RegisterBackgroundServiceContractFeature.Validator();
        foreach (var triggerType in new[] { "Cron", "Interval", "EventTriggered", "OnDemand", "Continuous" })
        {
            var result = validator.Validate(new RegisterBackgroundServiceContractFeature.Command(
                ApiAssetId: Guid.NewGuid(), SemVer: "1.0.0",
                ServiceName: "Job", Category: "Job", TriggerType: triggerType));
            result.IsValid.Should().BeTrue($"TriggerType '{triggerType}' should be valid");
        }
    }
}
