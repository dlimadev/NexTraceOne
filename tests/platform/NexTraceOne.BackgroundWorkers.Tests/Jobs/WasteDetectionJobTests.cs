using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectWasteSignals;
using NexTraceOne.BuildingBlocks.Core.Results;

using MediatR;
using NSubstitute;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

/// <summary>
/// W6-01: Testes unitários do WasteDetectionJob.
/// Verifica detecção de desperdício, notificações e tratamento de erros.
/// </summary>
public sealed class WasteDetectionJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _jobRegistry = new();
    private readonly ICatalogGraphModule _catalogModule = Substitute.For<ICatalogGraphModule>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IWasteSignalRepository _wasteRepo = Substitute.For<IWasteSignalRepository>();
    private readonly INotificationModule _notificationModule = Substitute.For<INotificationModule>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();

    public WasteDetectionJobTests()
    {
        _scope.ServiceProvider.Returns(Substitute.For<IServiceProvider>());
        _scopeFactory.CreateScope().Returns(_scope);

        var serviceProvider = _scope.ServiceProvider;

        // Configurar GetService e GetRequiredService para todos os tipos
        ConfigureServiceProvider(serviceProvider);
    }

    private void ConfigureServiceProvider(IServiceProvider serviceProvider)
    {
        // Configurar GetService para todos os tipos
        serviceProvider.GetService(typeof(ICatalogGraphModule)).Returns(_catalogModule);
        serviceProvider.GetService(typeof(IMediator)).Returns(_mediator);
        serviceProvider.GetService(typeof(IWasteSignalRepository)).Returns(_wasteRepo);
        serviceProvider.GetService(typeof(INotificationModule)).Returns(_notificationModule);

        // Configurar GetRequiredService explicitamente (NSubstitute suporta ambos)
        serviceProvider.GetRequiredService(typeof(ICatalogGraphModule)).Returns(_catalogModule);
        serviceProvider.GetRequiredService(typeof(IMediator)).Returns(_mediator);
        serviceProvider.GetRequiredService(typeof(IWasteSignalRepository)).Returns(_wasteRepo);
    }

    private WasteDetectionJob CreateJob() =>
        new(_scopeFactory, _jobRegistry, NullLogger<WasteDetectionJob>.Instance);

    // ── Cycle Detection Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunDetectionCycleAsync_WhenNoServices_LogsDebugAndSkips()
    {
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TeamServiceInfo>());

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        await _mediator.DidNotReceive().Send(
            Arg.Any<DetectWasteSignals.Command>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_WhenServicesExist_DetectsWasteSignals()
    {
        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "payment-service", "payments", "High", "Internal"),
            new(Guid.NewGuid().ToString(), "user-service", "identity", "Medium", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("test-svc", "production", 2, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Deve chamar mediator para cada serviço x ambiente (2 serviços x 2 ambientes = 4 chamadas)
        await _mediator.Received(4).Send(
            Arg.Any<DetectWasteSignals.Command>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_WhenDetectionFails_ContinuesWithNextService()
    {
        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "failing-service", "test", "Low", "Internal"),
            new(Guid.NewGuid().ToString(), "healthy-service", "test", "Low", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        // Primeiro serviço falha, segundo tem sucesso
        _mediator.When(x => x.Send(
            Arg.Is<DetectWasteSignals.Command>(c => c.ServiceName == "failing-service"),
            Arg.Any<CancellationToken>()))
            .Do(x => throw new InvalidOperationException("Simulated failure"));

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("healthy-service", "production", 1, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(
            Arg.Is<DetectWasteSignals.Command>(c => c.ServiceName == "healthy-service"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Ambos os serviços devem ser processados apesar da falha
        await _mediator.Received(4).Send(
            Arg.Any<DetectWasteSignals.Command>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_WhenSignalsDetected_NotifiesTeamOwners()
    {
        var service = new TeamServiceInfo(Guid.NewGuid().ToString(), "test-service", "test-team", "Medium", "Internal");
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { service });

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("test-service", "production", 3, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        var signals = new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>
        {
            NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal.Create(
                "test-service", "production",
                NexTraceOne.OperationalIntelligence.Domain.Cost.Enums.WasteSignalType.IdleResources,
                500m, "Idle resource", DateTimeOffset.UtcNow.AddHours(-1), "test-team")
        };
        _wasteRepo.ListAllAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(signals);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Deve enviar notificação quando há sinais detectados
        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType == "WasteDetected" && r.Severity == "Warning"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_WhenNoSignals_NoNotificationSent()
    {
        var service = new TeamServiceInfo(Guid.NewGuid().ToString(), "clean-service", "test-team", "Low", "Internal");
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { service });

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("clean-service", "production", 0, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Não deve enviar notificação quando não há sinais
        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Any<NotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_WhenHighSavings_FlagsRequiresAction()
    {
        var service = new TeamServiceInfo(Guid.NewGuid().ToString(), "expensive-service", "test-team", "High", "Internal");
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { service });

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("expensive-service", "production", 1, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        var signals = new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>
        {
            NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal.Create(
                "expensive-service", "production",
                NexTraceOne.OperationalIntelligence.Domain.Cost.Enums.WasteSignalType.Overprovisioned,
                750m, "Over budget by 50%", DateTimeOffset.UtcNow.AddHours(-1), "test-team")
        };
        _wasteRepo.ListAllAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(signals);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Poupança > 500 USD deve requerer ação
        await _notificationModule.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.RequiresAction == true && r.Message.Contains("750")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_GroupsNotificationsByTeam()
    {
        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "svc-a", "team-alpha", "Medium", "Internal"),
            new(Guid.NewGuid().ToString(), "svc-b", "team-beta", "Medium", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("test-svc", "production", 1, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        var signals = new List<NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal>
        {
            NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal.Create(
                "svc-a", "production",
                NexTraceOne.OperationalIntelligence.Domain.Cost.Enums.WasteSignalType.IdleResources,
                100m, "Idle", DateTimeOffset.UtcNow.AddHours(-1), "team-alpha"),
            NexTraceOne.OperationalIntelligence.Domain.Cost.Entities.WasteSignal.Create(
                "svc-b", "production",
                NexTraceOne.OperationalIntelligence.Domain.Cost.Enums.WasteSignalType.Overprovisioned,
                200m, "Over budget", DateTimeOffset.UtcNow.AddHours(-1), "team-beta")
        };
        _wasteRepo.ListAllAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(signals);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Deve enviar uma notificação por equipa (2 equipas = 2 notificações)
        await _notificationModule.Received(2).SubmitAsync(
            Arg.Any<NotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDetectionCycleAsync_WhenNotificationModuleIsNull_SkipsNotification()
    {
        var service = new TeamServiceInfo(Guid.NewGuid().ToString(), "test-service", "test-team", "Low", "Internal");
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { service });

        var wasteResult = Result<DetectWasteSignals.Response>.Success(
            new DetectWasteSignals.Response("test-service", "production", 1, Array.Empty<DetectWasteSignals.WasteSignalDto>()));
        _mediator.Send(Arg.Any<DetectWasteSignals.Command>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(wasteResult));

        // Configurar scope para retornar null no NotificationModule
        _scope.ServiceProvider.GetService(typeof(INotificationModule)).Returns(null);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunDetectionCycleAsync", CancellationToken.None);

        // Não deve lançar exceção mesmo sem módulo de notificação
        await _notificationModule.DidNotReceive().SubmitAsync(
            Arg.Any<NotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    // Helper para invocar métodos privados
    private static async Task InvokePrivateMethod(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null)
            throw new InvalidOperationException($"Método '{methodName}' não encontrado.");

        var task = (Task)method.Invoke(obj, parameters)!;
        await task.ConfigureAwait(false);
    }
}
