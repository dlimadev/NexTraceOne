using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using CreateServiceInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.CreateServiceInterface.CreateServiceInterface;
using DeprecateServiceInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.DeprecateServiceInterface.DeprecateServiceInterface;
using ListServiceInterfacesFeature = NexTraceOne.Catalog.Application.Graph.Features.ListServiceInterfaces.ListServiceInterfaces;

namespace NexTraceOne.Catalog.Tests.Graph.Features;

/// <summary>
/// Testes de unidade para os handlers de ServiceInterface:
/// CreateServiceInterface, DeprecateServiceInterface, ListServiceInterfaces.
/// </summary>
public sealed class ServiceInterfaceHandlerTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static ServiceAsset BuildService()
        => ServiceAsset.Create("payment-service", "payments", "core-team");

    // ─── CreateServiceInterface ───────────────────────────────────────────

    [Fact]
    public async Task CreateServiceInterface_Should_Succeed_When_ServiceExists()
    {
        var service = BuildService();
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var interfaceRepo = Substitute.For<IServiceInterfaceRepository>();
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();
        var audit = Substitute.For<IAuditModule>();

        var handler = new CreateServiceInterfaceFeature.Handler(serviceRepo, interfaceRepo, uow, eventBus, audit);
        var command = new CreateServiceInterfaceFeature.Command(
            ServiceId, "REST API v1", "RestApi",
            Description: "Main REST endpoint",
            CreatedBy: "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("REST API v1");
        result.Value.InterfaceType.Should().Be("RestApi");
        result.Value.Status.Should().Be("Active");
        interfaceRepo.Received(1).Add(Arg.Any<ServiceInterface>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await audit.Received(1).RecordEventAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceInterface_Should_Fail_When_ServiceNotFound()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var handler = new CreateServiceInterfaceFeature.Handler(
            serviceRepo,
            Substitute.For<IServiceInterfaceRepository>(),
            Substitute.For<ICatalogGraphUnitOfWork>(),
            Substitute.For<IEventBus>(),
            Substitute.For<IAuditModule>());

        var result = await handler.Handle(
            new CreateServiceInterfaceFeature.Command(Guid.NewGuid(), "API", "RestApi"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateServiceInterface_Should_Fail_When_InterfaceTypeInvalid()
    {
        var service = BuildService();
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var handler = new CreateServiceInterfaceFeature.Handler(
            serviceRepo,
            Substitute.For<IServiceInterfaceRepository>(),
            Substitute.For<ICatalogGraphUnitOfWork>(),
            Substitute.For<IEventBus>(),
            Substitute.For<IAuditModule>());

        var result = await handler.Handle(
            new CreateServiceInterfaceFeature.Command(ServiceId, "API", "InvalidType"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("InvalidType");
    }

    [Fact]
    public async Task CreateServiceInterface_Validator_Should_Require_Name()
    {
        var validator = new CreateServiceInterfaceFeature.Validator();
        var cmd = new CreateServiceInterfaceFeature.Command(ServiceId, "", "RestApi");
        var validation = await validator.ValidateAsync(cmd);
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateServiceInterface_Validator_Should_Require_ServiceId()
    {
        var validator = new CreateServiceInterfaceFeature.Validator();
        var cmd = new CreateServiceInterfaceFeature.Command(Guid.Empty, "API v1", "RestApi");
        var validation = await validator.ValidateAsync(cmd);
        validation.IsValid.Should().BeFalse();
    }

    // ─── DeprecateServiceInterface ────────────────────────────────────────

    [Fact]
    public async Task DeprecateServiceInterface_Should_Succeed_When_InterfaceActive()
    {
        var iface = ServiceInterface.Create(ServiceId, "REST API", InterfaceType.RestApi);
        var service = BuildService();

        var ifaceRepo = Substitute.For<IServiceInterfaceRepository>();
        ifaceRepo.GetByIdAsync(Arg.Any<ServiceInterfaceId>(), Arg.Any<CancellationToken>())
            .Returns(iface);

        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var uow = Substitute.For<ICatalogGraphUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();
        var audit = Substitute.For<IAuditModule>();

        var handler = new DeprecateServiceInterfaceFeature.Handler(ifaceRepo, serviceRepo, uow, eventBus, audit);
        var deprecationDate = DateTimeOffset.UtcNow.AddDays(30);
        var command = new DeprecateServiceInterfaceFeature.Command(
            iface.Id.Value,
            DeprecationDate: deprecationDate,
            Notice: "Use v2 instead.",
            DeprecatedBy: "admin@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        iface.IsDeprecated.Should().BeTrue();
        iface.DeprecationNotice.Should().Be("Use v2 instead.");
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await audit.Received(1).RecordEventAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeprecateServiceInterface_Should_Fail_When_InterfaceNotFound()
    {
        var ifaceRepo = Substitute.For<IServiceInterfaceRepository>();
        ifaceRepo.GetByIdAsync(Arg.Any<ServiceInterfaceId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceInterface?)null);

        var handler = new DeprecateServiceInterfaceFeature.Handler(
            ifaceRepo,
            Substitute.For<IServiceAssetRepository>(),
            Substitute.For<ICatalogGraphUnitOfWork>(),
            Substitute.For<IEventBus>(),
            Substitute.For<IAuditModule>());

        var result = await handler.Handle(
            new DeprecateServiceInterfaceFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeprecateServiceInterface_Validator_Should_Require_InterfaceId()
    {
        var validator = new DeprecateServiceInterfaceFeature.Validator();
        var cmd = new DeprecateServiceInterfaceFeature.Command(Guid.Empty);
        var validation = await validator.ValidateAsync(cmd);
        validation.IsValid.Should().BeFalse();
    }

    // ─── ListServiceInterfaces ────────────────────────────────────────────

    [Fact]
    public async Task ListServiceInterfaces_Should_ReturnAll_ForService()
    {
        var iface1 = ServiceInterface.Create(ServiceId, "REST v1", InterfaceType.RestApi);
        var iface2 = ServiceInterface.Create(ServiceId, "gRPC v1", InterfaceType.GrpcService);

        var repo = Substitute.For<IServiceInterfaceRepository>();
        repo.ListByServiceAsync(ServiceId, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceInterface> { iface1, iface2 });

        var handler = new ListServiceInterfacesFeature.Handler(repo);
        var result = await handler.Handle(new ListServiceInterfacesFeature.Query(ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(r => r.Name == "REST v1");
        result.Value.Should().Contain(r => r.Name == "gRPC v1");
    }

    [Fact]
    public async Task ListServiceInterfaces_Should_ReturnEmpty_When_NoInterfaces()
    {
        var repo = Substitute.For<IServiceInterfaceRepository>();
        repo.ListByServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceInterface>());

        var handler = new ListServiceInterfacesFeature.Handler(repo);
        var result = await handler.Handle(new ListServiceInterfacesFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
