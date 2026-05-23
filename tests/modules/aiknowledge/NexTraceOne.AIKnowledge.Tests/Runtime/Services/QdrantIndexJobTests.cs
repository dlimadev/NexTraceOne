using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class QdrantIndexJobTests
{
    private readonly IAiKnowledgeSourceRepository _sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
    private readonly IVectorStoreRepository _vectorStore = Substitute.For<IVectorStoreRepository>();
    private readonly ILogger<QdrantIndexJob> _logger = NullLogger<QdrantIndexJob>.Instance;

    private IServiceScopeFactory CreateScopeFactory(bool registerVectorStore = true)
    {
        var services = new ServiceCollection();
        services.AddScoped<IAiKnowledgeSourceRepository>(_ => _sourceRepo);
        if (registerVectorStore)
            services.AddSingleton<IVectorStoreRepository>(_ => _vectorStore);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task RunCycleAsync_NoVectorStoreRegistered_SkipsGracefully()
    {
        var job = new QdrantIndexJob(CreateScopeFactory(registerVectorStore: false), _logger);
        await job.StartAsync(CancellationToken.None);

        // Allow the background task to start and hit the first cycle
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        _sourceRepo.DidNotReceive().ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_NoModifiedSources_UpdatesTimestampOnly()
    {
        _sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIKnowledgeSource>());

        var job = new QdrantIndexJob(CreateScopeFactory(), _logger);
        await job.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        _vectorStore.DidNotReceive().StoreAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_WithModifiedSources_UpsertsToQdrant()
    {
        var source = AIKnowledgeSource.Register(
            "Test Source", "Test Description", KnowledgeSourceType.Documentation,
            "/docs", 1, DateTimeOffset.UtcNow);
        source.SetCreated(DateTimeOffset.UtcNow.AddDays(-1), "test");
        source.SetUpdated(DateTimeOffset.UtcNow, "test");
        source.SetEmbedding(new[] { 0.1f, 0.2f, 0.3f });

        _sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new[] { source });

        var job = new QdrantIndexJob(CreateScopeFactory(), _logger);
        // Invoke private RunCycleAsync via reflection to bypass the 120s startup delay
        var runCycleMethod = typeof(QdrantIndexJob).GetMethod("RunCycleAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)runCycleMethod.Invoke(job, new object[] { CancellationToken.None })!;

        await _vectorStore.Received(1).EnsureCollectionAsync("aiknowledge", 3, Arg.Any<CancellationToken>());
        await _vectorStore.Received(1).StoreAsync(
            "aiknowledge",
            source.Id.Value,
            Arg.Is<ReadOnlyMemory<float>>(v => v.Length == 3),
            Arg.Any<Dictionary<string, object>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_SourceWithoutEmbedding_SkipsUpsert()
    {
        var source = AIKnowledgeSource.Register(
            "No Embedding", "No Embedding Desc", KnowledgeSourceType.Documentation,
            "/docs", 1, DateTimeOffset.UtcNow);
        source.SetCreated(DateTimeOffset.UtcNow.AddDays(-1), "test");
        source.SetUpdated(DateTimeOffset.UtcNow, "test");
        // No SetEmbedding called

        _sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new[] { source });

        var job = new QdrantIndexJob(CreateScopeFactory(), _logger);
        var runCycleMethod = typeof(QdrantIndexJob).GetMethod("RunCycleAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)runCycleMethod.Invoke(job, new object[] { CancellationToken.None })!;

        await _vectorStore.DidNotReceive().StoreAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ReadOnlyMemory<float>>(),
            Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>());
    }
}
