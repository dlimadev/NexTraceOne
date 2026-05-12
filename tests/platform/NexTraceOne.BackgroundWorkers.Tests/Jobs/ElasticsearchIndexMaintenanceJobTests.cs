using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers.Elasticsearch;
using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.BackgroundWorkers.Jobs;

using NSubstitute;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

/// <summary>
/// W7-01: Testes unitários do ElasticsearchIndexMaintenanceJob.
/// Verifica aplicação de políticas ILM, health check e tratamento de erros.
/// </summary>
public sealed class ElasticsearchIndexMaintenanceJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _jobRegistry = new();
    private readonly IElasticsearchIndexManager _indexManager = Substitute.For<IElasticsearchIndexManager>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();

    public ElasticsearchIndexMaintenanceJobTests()
    {
        _scope.ServiceProvider.Returns(Substitute.For<IServiceProvider>());
        _scopeFactory.CreateScope().Returns(_scope);

        var serviceProvider = _scope.ServiceProvider;

        // Configurar GetService e GetRequiredService
        serviceProvider.GetService(typeof(IElasticsearchIndexManager)).Returns(_indexManager);
        serviceProvider.GetRequiredService(typeof(IElasticsearchIndexManager)).Returns(_indexManager);
    }

    private ElasticsearchIndexMaintenanceJob CreateJob() =>
        new(_scopeFactory, _jobRegistry, NullLogger<ElasticsearchIndexMaintenanceJob>.Instance);

    // ── Cluster Health Check Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunMaintenanceCycleAsync_WhenClusterIsUnhealthy_SkipsIlmApplication()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None);

        // Não deve aplicar políticas ILM quando cluster não está saudável
        await _indexManager.DidNotReceive().ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunMaintenanceCycleAsync_WhenClusterIsHealthy_AppliesIlmPolicies()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None);

        // Deve aplicar políticas ILM quando cluster está saudável
        await _indexManager.Received(1).ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    // ── Error Handling Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunMaintenanceCycleAsync_WhenIlmApplicationFails_PropagatesException()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _indexManager.When(x => x.ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>()))
            .Do(x => throw new InvalidOperationException("Simulated ILM failure"));

        var job = CreateJob();

        // O método RunMaintenanceCycleAsync propaga a exceção (é capturada em ExecuteAsync)
        await FluentActions.Awaiting(() => InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        // Ainda assim tentou aplicar
        await _indexManager.Received(1).ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunMaintenanceCycleAsync_WhenHealthCheckThrows_PropagatesException()
    {
        _indexManager.When(x => x.IsClusterHealthyAsync(Arg.Any<CancellationToken>()))
            .Do(x => throw new HttpRequestException("Connection refused"));

        var job = CreateJob();

        // O método RunMaintenanceCycleAsync propaga a exceção (é capturada em ExecuteAsync)
        await FluentActions.Awaiting(() => InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();

        // Não deve tentar aplicar políticas após falha no health check
        await _indexManager.DidNotReceive().ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    // ── Integration Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunMaintenanceCycleAsync_CompleteFlow_Successful()
    {
        // Configurar cenário de sucesso completo
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var job = CreateJob();
        await InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None);

        // Verificar sequência completa
        await _indexManager.Received(1).IsClusterHealthyAsync(Arg.Any<CancellationToken>());
        await _indexManager.Received(1).ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunMaintenanceCycleAsync_MultipleCycles_CallsIndexManagerEachTime()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var job = CreateJob();

        // Simular 3 ciclos consecutivos
        await InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None);
        await InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None);
        await InvokePrivateMethod(job, "RunMaintenanceCycleAsync", CancellationToken.None);

        // Deve chamar index manager em cada ciclo
        await _indexManager.Received(3).IsClusterHealthyAsync(Arg.Any<CancellationToken>());
        await _indexManager.Received(3).ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
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
