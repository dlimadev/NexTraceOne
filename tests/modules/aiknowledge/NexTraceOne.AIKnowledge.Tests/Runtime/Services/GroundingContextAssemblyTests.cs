using System.Linq;

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
    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DocumentRetrievalService MakeDocumentService(
        IAiKnowledgeSourceRepository? sourceRepo = null,
        IKnowledgeDocumentGroundingReader? knowledgeReader = null)
    {
        sourceRepo ??= Substitute.For<IAiKnowledgeSourceRepository>();
        knowledgeReader ??= Substitute.For<IKnowledgeDocumentGroundingReader>();
        return new DocumentRetrievalService(
            sourceRepo,
            knowledgeReader,
            Substitute.For<ILogger<DocumentRetrievalService>>());
    }

    private static DatabaseRetrievalService MakeDatabaseService(
        IAiModelRepository? modelRepo = null,
        ICatalogGroundingReader? catalogReader = null,
        IChangeGroundingReader? changeReader = null,
        IIncidentGroundingReader? incidentReader = null)
    {
        modelRepo ??= Substitute.For<IAiModelRepository>();
        catalogReader ??= Substitute.For<ICatalogGroundingReader>();
        changeReader ??= Substitute.For<IChangeGroundingReader>();
        incidentReader ??= Substitute.For<IIncidentGroundingReader>();
        return new DatabaseRetrievalService(
            modelRepo, catalogReader, changeReader, incidentReader,
            Substitute.For<ILogger<DatabaseRetrievalService>>());
    }

    // ── DocumentRetrievalService ────────────────────────────────────────

    [Fact]
    public async Task DocumentRetrieval_ShouldReturnEmptyWhenNoSourcesAndNoKnowledgeDocs()
    {
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIKnowledgeSource>());

        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<KnowledgeDocumentGroundingContext>());

        var sut = MakeDocumentService(sourceRepo, knowledgeReader);

        var result = await sut.SearchAsync(new DocumentSearchRequest("test query"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task DocumentRetrieval_ShouldReturnKnowledgeDocumentsWhenAvailable()
    {
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIKnowledgeSource>());

        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync("payment", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new KnowledgeDocumentGroundingContext("doc-1", "Payment Runbook", "How to handle payment failures", "Runbook")
            ]);

        var sut = MakeDocumentService(sourceRepo, knowledgeReader);

        var result = await sut.SearchAsync(new DocumentSearchRequest("payment"));

        result.Success.Should().BeTrue();
        result.Hits.Should().HaveCount(1);
        result.Hits[0].SourceId.Should().Be("KnowledgeHub");
        result.Hits[0].Title.Should().Be("Payment Runbook");
        result.Hits[0].RelevanceScore.Should().Be(0.90);
    }

    [Fact]
    public async Task DocumentRetrieval_ShouldContinueWhenKnowledgeReaderFails()
    {
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIKnowledgeSource>());

        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Knowledge DB unavailable"));

        var sut = MakeDocumentService(sourceRepo, knowledgeReader);

        // Should not throw — silent failure
        var result = await sut.SearchAsync(new DocumentSearchRequest("test"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DocumentRetrieval_ShouldContinueWhenSourceRepoFails()
    {
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Source DB unavailable"));

        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<KnowledgeDocumentGroundingContext>());

        var sut = MakeDocumentService(sourceRepo, knowledgeReader);

        var result = await sut.SearchAsync(new DocumentSearchRequest("test"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
    }

    // ── DatabaseRetrievalService ────────────────────────────────────────

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnEmptyWhenAllReadersReturnEmpty()
    {
        var modelRepo = Substitute.For<IAiModelRepository>();
        modelRepo.ListAsync(Arg.Any<string?>(), Arg.Any<ModelType?>(), Arg.Any<ModelStatus?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AIModel>());

        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        catalogReader.FindServicesAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ServiceGroundingContext>());

        var changeReader = Substitute.For<IChangeGroundingReader>();
        changeReader.FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReleaseGroundingContext>());

        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        incidentReader.FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IncidentGroundingContext>());

        var sut = MakeDatabaseService(modelRepo, catalogReader, changeReader, incidentReader);

        var result = await sut.SearchAsync(new DatabaseSearchRequest("nonexistent"));

        result.Success.Should().BeTrue();
        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnServiceContextWhenServiceIdProvided()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        catalogReader.FindServicesAsync("payment-api", Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new ServiceGroundingContext(
                    "svc-1", "Payment API", "Payments Team",
                    "Finance", "Critical", "Active", "RestApi",
                    "Handles all payment processing")
            ]);

        var sut = MakeDatabaseService(catalogReader: catalogReader);

        var result = await sut.SearchAsync(new DatabaseSearchRequest("payment", ServiceId: "payment-api"));

        result.Success.Should().BeTrue();
        result.Hits.Should().Contain(h => h.EntityType == "Service" && h.DisplayName == "Payment API");
        result.Hits.First(h => h.EntityType == "Service").RelevanceScore.Should().Be(0.95);
    }

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnRecentChanges()
    {
        var now = DateTimeOffset.UtcNow;
        var changeReader = Substitute.For<IChangeGroundingReader>();
        changeReader.FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new ReleaseGroundingContext(
                    "rel-1", "payment-api", "2.5.0", "Production",
                    "Deployed", "Minor", 0.3m, "Fix payment timeout", now.AddHours(-2))
            ]);

        var sut = MakeDatabaseService(changeReader: changeReader);

        var result = await sut.SearchAsync(new DatabaseSearchRequest("payment changes"));

        result.Success.Should().BeTrue();
        result.Hits.Should().Contain(h => h.EntityType == "Release");
        var releaseHit = result.Hits.First(h => h.EntityType == "Release");
        releaseHit.DisplayName.Should().Contain("payment-api");
        releaseHit.RelevanceScore.Should().Be(0.85);
    }

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnRecentIncidents()
    {
        var now = DateTimeOffset.UtcNow;
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        incidentReader.FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new IncidentGroundingContext(
                    "inc-1", "Payment Gateway Down", "payment-api",
                    "Critical", "Resolved", "Production",
                    "Payment gateway was unreachable", now.AddDays(-2))
            ]);

        var sut = MakeDatabaseService(incidentReader: incidentReader);

        var result = await sut.SearchAsync(new DatabaseSearchRequest("payment incident"));

        result.Success.Should().BeTrue();
        result.Hits.Should().Contain(h => h.EntityType == "Incident");
        var incidentHit = result.Hits.First(h => h.EntityType == "Incident");
        incidentHit.DisplayName.Should().Be("Payment Gateway Down");
        incidentHit.RelevanceScore.Should().Be(0.80);
    }

    [Fact]
    public async Task DatabaseRetrieval_ShouldReturnPartialResultWhenCatalogFails()
    {
        var now = DateTimeOffset.UtcNow;
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        catalogReader.FindServicesAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Catalog DB error"));

        var changeReader = Substitute.For<IChangeGroundingReader>();
        changeReader.FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new ReleaseGroundingContext("rel-1", "svc-a", "1.0", "Production",
                    "Deployed", "Patch", 0.1m, null, now.AddHours(-1))
            ]);

        var sut = MakeDatabaseService(
            catalogReader: catalogReader,
            changeReader: changeReader);

        // Catalog failure is silent — should still return changes
        var result = await sut.SearchAsync(new DatabaseSearchRequest("svc", ServiceId: "svc-a"));

        result.Success.Should().BeTrue();
        result.Hits.Should().Contain(h => h.EntityType == "Release");
        result.Hits.Should().NotContain(h => h.EntityType == "Service");
    }

    [Fact]
    public async Task DatabaseRetrieval_ShouldUseConfigurableTimeWindowsAndLimits()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        changeReader.FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReleaseGroundingContext>());

        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        incidentReader.FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IncidentGroundingContext>());

        var sut = MakeDatabaseService(changeReader: changeReader, incidentReader: incidentReader);

        var request = new DatabaseSearchRequest(
            "query",
            ChangesWindowDays: 14,
            IncidentsWindowDays: 60,
            MaxChanges: 5,
            MaxIncidents: 3);

        await sut.SearchAsync(request);

        await changeReader.Received(1).FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, null, null,
            5, // MaxChanges
            Arg.Any<CancellationToken>());

        await incidentReader.Received(1).FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), null, null,
            3, // MaxIncidents
            Arg.Any<CancellationToken>());
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
        request.ServiceId.Should().BeNull();
        request.ChangesWindowDays.Should().Be(7);
        request.IncidentsWindowDays.Should().Be(30);
        request.MaxChanges.Should().Be(10);
        request.MaxIncidents.Should().Be(5);
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
