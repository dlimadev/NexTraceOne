using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers.Elasticsearch;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Elasticsearch;

public sealed class ElasticsearchIndexManagerServiceTests
{
    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection([]).Build();

    private static ElasticsearchIndexManagerService CreateService(
        HttpStatusCode healthStatus = HttpStatusCode.OK,
        HttpStatusCode policyStatus = HttpStatusCode.OK,
        IConfiguration? configuration = null)
    {
        var handler = new FakeHttpHandler(healthStatus, policyStatus);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch:9200") };
        return new ElasticsearchIndexManagerService(
            httpClient,
            configuration ?? EmptyConfig(),
            NullLogger<ElasticsearchIndexManagerService>.Instance);
    }

    // ── IsClusterHealthyAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IsClusterHealthyAsync_WhenHealthEndpointReturns200_ReturnsTrue()
    {
        var service = CreateService(healthStatus: HttpStatusCode.OK);

        var result = await service.IsClusterHealthyAsync(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsClusterHealthyAsync_WhenHealthEndpointReturns503_ReturnsFalse()
    {
        var service = CreateService(healthStatus: HttpStatusCode.ServiceUnavailable);

        var result = await service.IsClusterHealthyAsync(CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsClusterHealthyAsync_WhenHttpExceptionThrown_ReturnsFalse()
    {
        var handler = new ThrowingHttpHandler(new HttpRequestException("connection refused"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch:9200") };
        var service = new ElasticsearchIndexManagerService(
            httpClient,
            EmptyConfig(),
            NullLogger<ElasticsearchIndexManagerService>.Instance);

        var result = await service.IsClusterHealthyAsync(CancellationToken.None);

        result.Should().BeFalse();
    }

    // ── ApplyIlmPoliciesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ApplyIlmPoliciesAsync_WhenAllPoliciesSucceed_DoesNotThrow()
    {
        var service = CreateService(policyStatus: HttpStatusCode.OK);

        var act = () => service.ApplyIlmPoliciesAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyIlmPoliciesAsync_SendsRequestsForThreePolicies()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch:9200") };
        var service = new ElasticsearchIndexManagerService(
            httpClient,
            EmptyConfig(),
            NullLogger<ElasticsearchIndexManagerService>.Instance);

        await service.ApplyIlmPoliciesAsync(CancellationToken.None);

        var policyRequests = handler.Requests
            .Where(r => r.RequestUri!.AbsolutePath.StartsWith("/_ilm/policy/"))
            .ToList();

        policyRequests.Should().HaveCount(3);
        policyRequests.Select(r => r.RequestUri!.AbsolutePath).Should().Contain(
            "/_ilm/policy/nxt-traces-policy",
            "/_ilm/policy/nxt-logs-policy",
            "/_ilm/policy/nxt-metrics-policy");
    }

    [Fact]
    public async Task ApplyIlmPoliciesAsync_WhenOnePolicyFails_ContinuesWithOthers()
    {
        // First PUT fails, subsequent ones succeed
        var handler = new FailFirstPutHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch:9200") };
        var service = new ElasticsearchIndexManagerService(
            httpClient,
            EmptyConfig(),
            NullLogger<ElasticsearchIndexManagerService>.Instance);

        // Should not throw even if one policy fails
        var act = () => service.ApplyIlmPoliciesAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();

        // All 3 policies were attempted
        handler.PutCount.Should().Be(3);
    }

    [Fact]
    public async Task ApplyIlmPoliciesAsync_UsesConfiguredRetentionDays()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elasticsearch:ILM:TracesHotRetentionDays"] = "5",
                ["Elasticsearch:ILM:TracesWarmRetentionDays"] = "10",
                ["Elasticsearch:ILM:TracesDeleteAfterDays"] = "60",
            })
            .Build();

        var handler = new RecordingHttpHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://elasticsearch:9200") };
        var service = new ElasticsearchIndexManagerService(
            httpClient,
            config,
            NullLogger<ElasticsearchIndexManagerService>.Instance);

        await service.ApplyIlmPoliciesAsync(CancellationToken.None);

        var tracesRequest = handler.Requests
            .First(r => r.RequestUri!.AbsolutePath == "/_ilm/policy/nxt-traces-policy");
        var body = await tracesRequest.Content!.ReadAsStringAsync();

        body.Should().Contain("5d");   // hot retention
        body.Should().Contain("60d");  // delete after
    }

    // ── Fake handlers ─────────────────────────────────────────────────────────

    private sealed class FakeHttpHandler(
        HttpStatusCode healthStatus,
        HttpStatusCode policyStatus) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var status = path.StartsWith("/_cluster") ? healthStatus : policyStatus;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class ThrowingHttpHandler(Exception ex) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw ex;
    }

    private sealed class RecordingHttpHandler(HttpStatusCode status) : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _requests = [];
        public IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class FailFirstPutHandler : HttpMessageHandler
    {
        public int PutCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Put)
            {
                PutCount++;
                if (PutCount == 1)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
