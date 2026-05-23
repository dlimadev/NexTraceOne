using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>
/// Verifica que os conectores de datasource usam IHttpClientFactory
/// em vez de new HttpClient(), evitando socket exhaustion.
/// </summary>
public sealed class ConnectorHttpClientFactoryTests
{
    private static (IHttpClientFactory factory, TestHandler handler) CreateFactory()
    {
        var handler = new TestHandler();
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return (factory, handler);
    }

    [Fact]
    public void GitHubConnector_BuildClient_UsesNamedFactory()
    {
        var (factory, _) = CreateFactory();
        var connector = new GitHubConnector(factory, NullLogger<GitHubConnector>.Instance);

        InvokeBuildClient(connector);

        factory.Received(1).CreateClient("GitHubConnector");
    }

    [Fact]
    public void GitLabConnector_BuildClient_UsesNamedFactory()
    {
        var (factory, _) = CreateFactory();
        var connector = new GitLabConnector(factory, NullLogger<GitLabConnector>.Instance);

        InvokeBuildClient(connector);

        factory.Received(1).CreateClient("GitLabConnector");
    }

    [Fact]
    public void CustomHttpConnector_BuildClient_UsesNamedFactory()
    {
        var (factory, _) = CreateFactory();
        var connector = new CustomHttpConnector(factory, NullLogger<CustomHttpConnector>.Instance);

        InvokeBuildClient(connector);

        factory.Received(1).CreateClient("CustomHttpConnector");
    }

    [Fact]
    public async Task GitHubConnector_SetsAuthHeaderPerRequest()
    {
        var (factory, handler) = CreateFactory();
        handler.RespondWith("{\"tree\":[]}");

        var connector = new GitHubConnector(factory, NullLogger<GitHubConnector>.Instance);
        _ = await connector.FetchDocumentsAsync(
            "{\"accessToken\":\"ghp_test\",\"repositories\":[\"owner/repo\"],\"includeExtensions\":[\".md\"],\"branch\":\"main\",\"maxFilesPerRepo\":10}",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("ghp_test");
        handler.LastRequest.Headers.UserAgent.ToString().Should().Contain("NexTraceOne");
        handler.LastRequest.Headers.Accept.Any(a => a.MediaType == "application/vnd.github+json").Should().BeTrue();
    }

    [Fact]
    public async Task GitLabConnector_SetsPrivateTokenHeaderPerRequest()
    {
        var (factory, handler) = CreateFactory();
        handler.RespondWith("[]");

        var connector = new GitLabConnector(factory, NullLogger<GitLabConnector>.Instance);
        _ = await connector.FetchDocumentsAsync(
            "{\"accessToken\":\"glpat_test\",\"baseUrl\":\"https://gitlab.com\",\"projectIds\":[123],\"includeExtensions\":[\".md\"],\"branch\":\"main\",\"maxFilesPerProject\":10}",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Contains("PRIVATE-TOKEN").Should().BeTrue();
        handler.LastRequest.Headers.GetValues("PRIVATE-TOKEN").First().Should().Be("glpat_test");
    }

    [Fact]
    public async Task CustomHttpConnector_SetsAuthHeaderPerRequest()
    {
        var (factory, handler) = CreateFactory();
        handler.RespondWith("[]");

        var connector = new CustomHttpConnector(factory, NullLogger<CustomHttpConnector>.Instance);
        _ = await connector.FetchDocumentsAsync(
            "{\"baseUrl\":\"https://api.example.com\",\"authHeader\":\"X-Key\",\"authValue\":\"secret\",\"fetchEndpoint\":\"/docs\",\"searchEndpoint\":\"/search?q={query}\",\"titleJsonPath\":\"title\",\"contentJsonPath\":\"content\",\"urlJsonPath\":\"url\"}",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Contains("X-Key").Should().BeTrue();
        handler.LastRequest.Headers.GetValues("X-Key").First().Should().Be("secret");
    }

    private static HttpClient InvokeBuildClient(object connector)
    {
        var method = connector.GetType().GetMethod("BuildClient", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull("BuildClient method must exist");
        return (HttpClient)method!.Invoke(connector, null)!;
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private string _responseBody = "{}";
        private HttpStatusCode _statusCode = HttpStatusCode.OK;

        public HttpRequestMessage? LastRequest { get; private set; }

        public void RespondWith(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responseBody = body;
            _statusCode = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody)
            });
        }
    }
}
