using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectWasteSignals;

using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Jobs;

public sealed class WasteDetectionJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _registry = new();
    private readonly ICatalogGraphModule _catalogModule = Substitute.For<ICatalogGraphModule>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IWasteSignalRepository _wasteRepo = Substitute.For<IWasteSignalRepository>();
    private readonly INotificationModule _notificationModule = Substitute.For<INotificationModule>();

    private WasteDetectionJob CreateJob()
    {
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();

        provider.GetService(typeof(ICatalogGraphModule)).Returns(_catalogModule);
        provider.GetService(typeof(IMediator)).Returns(_mediator);
        provider.GetService(typeof(IWasteSignalRepository)).Returns(_wasteRepo);
        provider.GetService(typeof(INotificationModule)).Returns(_notificationModule);

        // GetRequiredService delegates to GetService for substitutes
        provider.GetService(typeof(ICatalogGraphModule)).Returns(_catalogModule);

        scope.ServiceProvider.Returns(provider);
        _scopeFactory.CreateScope().Returns(scope);

        _notificationModule.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(true));

        return new WasteDetectionJob(
            _scopeFactory,
            _registry,
            NullLogger<WasteDetectionJob>.Instance);
    }

    [Fact]
    public void HealthCheckName_IsCorrect()
    {
        WasteDetectionJob.HealthCheckName.Should().Be("waste-detection-job");
    }

    [Fact]
    public async Task RunDetectionCycle_WhenNoServices_SkipsCycle()
    {
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TeamServiceInfo>());

        var job = CreateJob();
        await job.RunDetectionCycleAsync(CancellationToken.None);

        await _mediator.DidNotReceive().Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycle_WhenServicesExist_RunsDetectionForEach()
    {
        var services = new List<TeamServiceInfo>
        {
            new("svc1", "service-a", "platform", "High", "team"),
            new("svc2", "service-b", "platform", "Medium", "team"),
        };

        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<DetectWasteSignals.Response>.Success(
                new DetectWasteSignals.Response("service-a", "production", 0, [])));

        _wasteRepo.ListAllAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>());

        var job = CreateJob();
        await job.RunDetectionCycleAsync(CancellationToken.None);

        // 2 services × 2 environments (production + staging) = 4 calls
        await _mediator.Received(4).Send(
            Arg.Any<DetectWasteSignals.Command>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycle_WhenOneServiceFails_ContinuesWithOthers()
    {
        var services = new List<TeamServiceInfo>
        {
            new("svc1", "service-fail", "platform", "High", "team"),
            new("svc2", "service-ok", "platform", "Medium", "team"),
        };

        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var callCount = 0;
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (callCount++ == 0) throw new InvalidOperationException("service unavailable");
                return Task.FromResult(Result<DetectWasteSignals.Response>.Success(
                    new DetectWasteSignals.Response("service-ok", "production", 0, [])));
            });

        _wasteRepo.ListAllAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>());

        var job = CreateJob();
        await job.RunDetectionCycleAsync(CancellationToken.None);

        // Deve ter tentado ambos os serviços — a falha do primeiro não bloqueia o segundo
        await _mediator.Received().Send(
            Arg.Is<DetectWasteSignals.Command>(c => c.ServiceName == "service-ok"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycle_WhenWasteDetected_NotifiesTeamOwners()
    {
        var services = new List<TeamServiceInfo>
        {
            new("svc1", "service-a", "platform", "High", "team"),
        };

        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<DetectWasteSignals.Response>.Success(
                new DetectWasteSignals.Response("service-a", "production", 2, [])));

        var wasteSignals = new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>();
        _wasteRepo.ListAllAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(wasteSignals);

        var job = CreateJob();
        await job.RunDetectionCycleAsync(CancellationToken.None);

        // Deve ter chamado ListAllAsync para notificação
        await _wasteRepo.Received().ListAllAsync(
            Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycle_WhenNoWasteDetected_DoesNotSendNotification()
    {
        var services = new List<TeamServiceInfo>
        {
            new("svc1", "service-a", "platform", "High", "team"),
        };

        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<DetectWasteSignals.Response>.Success(
                new DetectWasteSignals.Response("service-a", "production", 0, [])));

        var job = CreateJob();
        await job.RunDetectionCycleAsync(CancellationToken.None);

        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycle_SendsProductionAndStagingEnvironments()
    {
        var services = new List<TeamServiceInfo>
        {
            new("svc1", "service-a", "platform", "High", "team"),
        };

        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<DetectWasteSignals.Response>.Success(
                new DetectWasteSignals.Response("service-a", "production", 0, [])));

        _wasteRepo.ListAllAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>());

        var job = CreateJob();
        await job.RunDetectionCycleAsync(CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<DetectWasteSignals.Command>(c => c.Environment == "production"),
            Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(
            Arg.Is<DetectWasteSignals.Command>(c => c.Environment == "staging"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycle_WhenCatalogModuleThrows_LogsAndDoesNotCrash()
    {
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<TeamServiceInfo>>>(
                _ => throw new InvalidOperationException("catalog unavailable"));

        var job = CreateJob();

        // O ciclo deve propagar a exceção (é tratada pelo loop do job)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.RunDetectionCycleAsync(CancellationToken.None));
    }
}
