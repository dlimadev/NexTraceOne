using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using BindContractToInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.BindContractToInterface.BindContractToInterface;
using DeactivateContractBindingFeature = NexTraceOne.Catalog.Application.Graph.Features.DeactivateContractBinding.DeactivateContractBinding;
using ListContractBindingsByInterfaceFeature = NexTraceOne.Catalog.Application.Graph.Features.ListContractBindingsByInterface.ListContractBindingsByInterface;

namespace NexTraceOne.Catalog.Tests.Graph.Features;

/// <summary>
/// Testes de unidade para os handlers de ContractBinding:
/// BindContractToInterface, DeactivateContractBinding, ListContractBindingsByInterface.
/// </summary>
public sealed class ContractBindingHandlerTests
{
    private static readonly Guid InterfaceId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid ContractVersionId = Guid.NewGuid();

    private static ServiceInterface BuildActiveInterface()
        => ServiceInterface.Create(ServiceAssetId.From(ServiceId), "REST API v1", InterfaceType.RestApi);

    private static ServiceInterface BuildRetiredInterface()
    {
        var iface = ServiceInterface.Create(ServiceAssetId.From(ServiceId), "Legacy API", InterfaceType.RestApi);
        iface.Retire();
        return iface;
    }

    // ─── BindContractToInterface ──────────────────────────────────────────

    [Fact]
    public async Task BindContractToInterface_Should_Succeed_When_InterfaceActive()
    {
        var iface = BuildActiveInterface();
        var ifaceRepo = Substitute.For<IServiceInterfaceRepository>();
        ifaceRepo.GetByIdAsync(Arg.Any<ServiceInterfaceId>(), Arg.Any<CancellationToken>())
            .Returns(iface);

        var bindingRepo = Substitute.For<IContractBindingRepository>();
        var uow = Substitute.For<ICatalogGraphUnitOfWork>();
        var eventBus = Substitute.For<IEventBus>();
        var audit = Substitute.For<IAuditModule>();

        var handler = new BindContractToInterfaceFeature.Handler(ifaceRepo, bindingRepo, uow, eventBus, audit);
        var command = new BindContractToInterfaceFeature.Command(
            iface.Id.Value,
            ContractVersionId,
            "production",
            IsDefaultVersion: true,
            BoundBy: "engineer@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(ContractVersionId);
        result.Value.Status.Should().Be("Active");
        bindingRepo.Received(1).Add(Arg.Any<ContractBinding>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await audit.Received(1).RecordEventAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindContractToInterface_Should_Fail_When_InterfaceNotFound()
    {
        var ifaceRepo = Substitute.For<IServiceInterfaceRepository>();
        ifaceRepo.GetByIdAsync(Arg.Any<ServiceInterfaceId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceInterface?)null);

        var handler = new BindContractToInterfaceFeature.Handler(
            ifaceRepo,
            Substitute.For<IContractBindingRepository>(),
            Substitute.For<ICatalogGraphUnitOfWork>(),
            Substitute.For<IEventBus>(),
            Substitute.For<IAuditModule>());

        var result = await handler.Handle(
            new BindContractToInterfaceFeature.Command(Guid.NewGuid(), ContractVersionId, "staging"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BindContractToInterface_Should_Fail_When_InterfaceRetired()
    {
        var retiredInterface = BuildRetiredInterface();
        var ifaceRepo = Substitute.For<IServiceInterfaceRepository>();
        ifaceRepo.GetByIdAsync(Arg.Any<ServiceInterfaceId>(), Arg.Any<CancellationToken>())
            .Returns(retiredInterface);

        var handler = new BindContractToInterfaceFeature.Handler(
            ifaceRepo,
            Substitute.For<IContractBindingRepository>(),
            Substitute.For<ICatalogGraphUnitOfWork>(),
            Substitute.For<IEventBus>(),
            Substitute.For<IAuditModule>());

        var result = await handler.Handle(
            new BindContractToInterfaceFeature.Command(retiredInterface.Id.Value, ContractVersionId, "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BindContractToInterface_Validator_Should_Require_ContractVersionId()
    {
        var validator = new BindContractToInterfaceFeature.Validator();
        var cmd = new BindContractToInterfaceFeature.Command(InterfaceId, Guid.Empty, "production");
        var validation = await validator.ValidateAsync(cmd);
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task BindContractToInterface_Validator_Should_Require_BindingEnvironment()
    {
        var validator = new BindContractToInterfaceFeature.Validator();
        var cmd = new BindContractToInterfaceFeature.Command(InterfaceId, ContractVersionId, "");
        var validation = await validator.ValidateAsync(cmd);
        validation.IsValid.Should().BeFalse();
    }

    // ─── DeactivateContractBinding ────────────────────────────────────────

    [Fact]
    public async Task DeactivateContractBinding_Should_Succeed_When_BindingExists()
    {
        var binding = ContractBinding.Create(ServiceInterfaceId.From(InterfaceId), ContractVersionId, "production");
        var bindingRepo = Substitute.For<IContractBindingRepository>();
        bindingRepo.GetByIdAsync(Arg.Any<ContractBindingId>(), Arg.Any<CancellationToken>())
            .Returns(binding);

        var uow = Substitute.For<ICatalogGraphUnitOfWork>();
        var dateProvider = Substitute.For<IDateTimeProvider>();
        dateProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var eventBus = Substitute.For<IEventBus>();
        var audit = Substitute.For<IAuditModule>();

        var handler = new DeactivateContractBindingFeature.Handler(bindingRepo, uow, dateProvider, eventBus, audit);
        var result = await handler.Handle(
            new DeactivateContractBindingFeature.Command(binding.Id.Value, "admin@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        binding.Status.Should().Be(ContractBindingStatus.Deprecated);
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await audit.Received(1).RecordEventAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateContractBinding_Should_Fail_When_BindingNotFound()
    {
        var bindingRepo = Substitute.For<IContractBindingRepository>();
        bindingRepo.GetByIdAsync(Arg.Any<ContractBindingId>(), Arg.Any<CancellationToken>())
            .Returns((ContractBinding?)null);

        var handler = new DeactivateContractBindingFeature.Handler(
            bindingRepo,
            Substitute.For<ICatalogGraphUnitOfWork>(),
            Substitute.For<IDateTimeProvider>(),
            Substitute.For<IEventBus>(),
            Substitute.For<IAuditModule>());

        var result = await handler.Handle(
            new DeactivateContractBindingFeature.Command(Guid.NewGuid(), "admin@test.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateContractBinding_Validator_Should_Require_DeactivatedBy()
    {
        var validator = new DeactivateContractBindingFeature.Validator();
        var cmd = new DeactivateContractBindingFeature.Command(Guid.NewGuid(), "");
        var validation = await validator.ValidateAsync(cmd);
        validation.IsValid.Should().BeFalse();
    }

    // ─── ListContractBindingsByInterface ──────────────────────────────────

    [Fact]
    public async Task ListContractBindingsByInterface_Should_ReturnAll_Bindings()
    {
        var binding1 = ContractBinding.Create(ServiceInterfaceId.From(InterfaceId), Guid.NewGuid(), "production");
        var binding2 = ContractBinding.Create(ServiceInterfaceId.From(InterfaceId), Guid.NewGuid(), "staging");

        var repo = Substitute.For<IContractBindingRepository>();
        repo.ListByInterfaceAsync(InterfaceId, Arg.Any<CancellationToken>())
            .Returns(new List<ContractBinding> { binding1, binding2 });

        var handler = new ListContractBindingsByInterfaceFeature.Handler(repo);
        var result = await handler.Handle(
            new ListContractBindingsByInterfaceFeature.Query(InterfaceId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(b => b.BindingEnvironment == "production");
        result.Value.Should().Contain(b => b.BindingEnvironment == "staging");
    }

    [Fact]
    public async Task ListContractBindingsByInterface_Should_ReturnEmpty_When_NoBindings()
    {
        var repo = Substitute.For<IContractBindingRepository>();
        repo.ListByInterfaceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractBinding>());

        var handler = new ListContractBindingsByInterfaceFeature.Handler(repo);
        var result = await handler.Handle(
            new ListContractBindingsByInterfaceFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
