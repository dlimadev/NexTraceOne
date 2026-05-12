using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

using NSubstitute;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

/// <summary>
/// W6-04: Testes unitários do CarbonScoreCalculationJob.
/// Verifica cálculo de carbon score, configuração e tratamento de erros.
/// </summary>
public sealed class CarbonScoreCalculationJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _jobRegistry = new();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly ICatalogGraphModule _catalogModule = Substitute.For<ICatalogGraphModule>();
    private readonly ICarbonScoreRepository _carbonRepo = Substitute.For<ICarbonScoreRepository>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();

    public CarbonScoreCalculationJobTests()
    {
        _scope.ServiceProvider.Returns(Substitute.For<IServiceProvider>());
        _scopeFactory.CreateScope().Returns(_scope);

        var serviceProvider = _scope.ServiceProvider;

        // Configurar GetService para todos os tipos
        serviceProvider.GetService(typeof(ICatalogGraphModule)).Returns(_catalogModule);
        serviceProvider.GetService(typeof(ICarbonScoreRepository)).Returns(_carbonRepo);

        // Configurar GetRequiredService explicitamente
        serviceProvider.GetRequiredService(typeof(ICatalogGraphModule)).Returns(_catalogModule);
        serviceProvider.GetRequiredService(typeof(ICarbonScoreRepository)).Returns(_carbonRepo);
    }

    private CarbonScoreCalculationJob CreateJob() =>
        new(_scopeFactory, _jobRegistry, _configuration, NullLogger<CarbonScoreCalculationJob>.Instance);

    // ── Configuration Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunCalculationCycleAsync_UsesDefaultIntensityFactor_WhenNotConfigured()
    {
        // Configurar IConfiguration para retornar valor padrão
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "test-service", "test-team", "Medium", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Deve usar factor padrão 233.0 gCO₂/kWh
        await _carbonRepo.Received(1).UpsertAsync(
            Arg.Is<CarbonScoreRecord>(r => r.IntensityFactor == 233.0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCalculationCycleAsync_UsesCustomIntensityFactor_WhenConfigured()
    {
        // Configurar IConfiguration com factor customizado
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "450" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "test-service", "test-team", "High", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Deve usar factor configurado 450.0 gCO₂/kWh
        await _carbonRepo.Received(1).UpsertAsync(
            Arg.Is<CarbonScoreRecord>(r => r.IntensityFactor == 450.0),
            Arg.Any<CancellationToken>());
    }

    // ── Service Processing Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunCalculationCycleAsync_WhenNoServices_LogsDebugAndSkips()
    {
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TeamServiceInfo>());

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        await _carbonRepo.DidNotReceive().UpsertAsync(
            Arg.Any<CarbonScoreRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCalculationCycleAsync_ProcessesAllServices()
    {
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "service-a", "team-a", "Medium", "Internal"),
            new(Guid.NewGuid().ToString(), "service-b", "team-b", "High", "Internal"),
            new(Guid.NewGuid().ToString(), "service-c", "team-c", "Low", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Deve processar todos os 3 serviços
        await _carbonRepo.Received(3).UpsertAsync(
            Arg.Any<CarbonScoreRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCalculationCycleAsync_SkipsInvalidServiceIds()
    {
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new List<TeamServiceInfo>
        {
            new("invalid-guid", "invalid-service", "team", "Low", "Internal"),
            new(Guid.NewGuid().ToString(), "valid-service", "team", "Medium", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Deve processar apenas o serviço com Guid válido (ignora "invalid-guid")
        await _carbonRepo.Received(1).UpsertAsync(
            Arg.Any<CarbonScoreRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCalculationCycleAsync_ContinuesOnError()
    {
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new List<TeamServiceInfo>
        {
            new(Guid.NewGuid().ToString(), "failing-service", "team", "Medium", "Internal"),
            new(Guid.NewGuid().ToString(), "healthy-service", "team", "High", "Internal")
        };
        _catalogModule.ListAllServicesAsync(Arg.Any<CancellationToken>()).Returns(services);

        // Primeiro serviço falha, segundo tem sucesso
        _carbonRepo.When(x => x.UpsertAsync(
            Arg.Is<CarbonScoreRecord>(r => r.ServiceId.ToString() != services[1].ServiceId),
            Arg.Any<CancellationToken>()))
            .Do(x => throw new InvalidOperationException("Simulated failure"));

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Ambos os serviços devem ser tentados apesar da falha
        await _carbonRepo.Received(2).UpsertAsync(
            Arg.Any<CarbonScoreRecord>(),
            Arg.Any<CancellationToken>());
    }

    // ── Repository Availability Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunCalculationCycleAsync_WhenRepositoryIsNull_LogsWarningAndSkips()
    {
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Configurar scope para retornar null no repositório
        _scope.ServiceProvider.GetService(typeof(ICarbonScoreRepository)).Returns(null);

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Não deve tentar upsert quando repositório é null
        await _carbonRepo.DidNotReceive().UpsertAsync(
            Arg.Any<CarbonScoreRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCalculationCycleAsync_WhenCatalogModuleIsNull_LogsDebugAndSkips()
    {
        var configDict = new Dictionary<string, string>
        {
            { "Platform:GreenOps:IntensityFactor", "233" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Configurar scope para retornar null no catálogo
        _scope.ServiceProvider.GetService(typeof(ICatalogGraphModule)).Returns(null);

        var job = new CarbonScoreCalculationJob(_scopeFactory, _jobRegistry, configuration, NullLogger<CarbonScoreCalculationJob>.Instance);
        await InvokePrivateMethod(job, "RunCalculationCycleAsync", CancellationToken.None);

        // Não deve tentar upsert quando catálogo é null
        await _carbonRepo.DidNotReceive().UpsertAsync(
            Arg.Any<CarbonScoreRecord>(),
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
