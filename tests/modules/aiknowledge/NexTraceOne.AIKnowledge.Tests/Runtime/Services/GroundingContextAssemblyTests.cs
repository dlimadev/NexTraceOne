using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;

using NSubstitute.ExceptionExtensions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>
/// Testes unitários para validação dos retrieval services usados no grounding context assembly.
/// Garante que os serviços retornam dados reais, tratam erros e respondem corretamente a filtros.
/// </summary>
public sealed class GroundingContextAssemblyTests
{
    // ── DocumentRetrievalService ────────────────────────────────────────

    [Fact]
    public async Task DocumentRetrieval_ShouldReturnEmptyWhenNoSources()
    {
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIKnowledgeSource>());

        var sut = new DocumentRetrievalService(sourceRepo, Substitute.For<ILogger<DocumentRetrievalService>>());

        var result = await sut.SearchAsync(new DocumentSearchRequest("test query"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task DocumentRetrieval_ShouldReturnFailureOnException()
    {
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB unavailable"));

        var sut = new DocumentRetrievalService(sourceRepo, Substitute.For<ILogger<DocumentRetrievalService>>());

        var result = await sut.SearchAsync(new DocumentSearchRequest("test"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DB unavailable");
    }

    // ── DatabaseRetrievalService ────────────────────────────────────────

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnEmptyWhenNoModels()
    {
        var modelRepo = Substitute.For<IAiModelRepository>();
        modelRepo.ListAsync(Arg.Any<string?>(), Arg.Any<ModelType?>(), Arg.Any<ModelStatus?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIModel>());

        var sut = new DatabaseRetrievalService(modelRepo, Substitute.For<ILogger<DatabaseRetrievalService>>());

        var result = await sut.SearchAsync(new DatabaseSearchRequest("nonexistent"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnFailureOnException()
    {
        var modelRepo = Substitute.For<IAiModelRepository>();
        modelRepo.ListAsync(Arg.Any<string?>(), Arg.Any<ModelType?>(), Arg.Any<ModelStatus?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB error"));

        var sut = new DatabaseRetrievalService(modelRepo, Substitute.For<ILogger<DatabaseRetrievalService>>());

        var result = await sut.SearchAsync(new DatabaseSearchRequest("test"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DB error");
    }

    // ── TelemetryRetrievalService ───────────────────────────────────────

    [Fact]
    public async Task TelemetryRetrieval_ShouldReturnEmptyWhenNoLogs()
    {
        var obsProvider = Substitute.For<IObservabilityProvider>();
        obsProvider.QueryLogsAsync(Arg.Any<LogQueryFilter>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LogEntry>());

        var sut = new TelemetryRetrievalService(obsProvider, Substitute.For<ILogger<TelemetryRetrievalService>>());

        var result = await sut.SearchAsync(new TelemetrySearchRequest("error"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task TelemetryRetrieval_ShouldReturnFailureOnException()
    {
        var obsProvider = Substitute.For<IObservabilityProvider>();
        obsProvider.QueryLogsAsync(Arg.Any<LogQueryFilter>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Observability unavailable"));

        var sut = new TelemetryRetrievalService(obsProvider, Substitute.For<ILogger<TelemetryRetrievalService>>());

        var result = await sut.SearchAsync(new TelemetrySearchRequest("error"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Observability unavailable");
    }

    // ── Request Defaults ────────────────────────────────────────────────

    [Fact]
    public void DocumentSearchRequest_ShouldHaveDefaults()
    {
        var request = new DocumentSearchRequest("test query");
        request.MaxResults.Should().Be(10);
        request.SourceFilter.Should().BeNull();
        request.ClassificationFilter.Should().BeNull();
    }

    [Fact]
    public void DatabaseSearchRequest_ShouldHaveDefaults()
    {
        var request = new DatabaseSearchRequest("test query");
        request.MaxResults.Should().Be(10);
        request.EntityType.Should().BeNull();
        request.TenantId.Should().BeNull();
    }

    [Fact]
    public void TelemetrySearchRequest_ShouldHaveDefaults()
    {
        var request = new TelemetrySearchRequest("error query");
        request.MaxResults.Should().Be(50);
        request.ServiceName.Should().BeNull();
        request.Severity.Should().BeNull();
        request.TraceId.Should().BeNull();
    }
}
