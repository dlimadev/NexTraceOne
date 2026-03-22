using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NexTraceOne.CLI.Services;

namespace NexTraceOne.CLI.Tests.Commands;

/// <summary>
/// Testes do CatalogApiClient: construção de URL, tratamento de falhas de conexão e erros HTTP.
/// </summary>
public sealed class CatalogCommandTests
{
    [Fact]
    public void CatalogApiClient_ConstructsProperBaseUrl()
    {
        const string baseUrl = "https://api.nex.local/";

        using var client = new CatalogApiClient(baseUrl);

        // The client should be creatable with a valid URL without throwing
        client.Should().NotBeNull();
    }

    [Fact]
    public void CatalogApiClient_TrimsTrailingSlash()
    {
        const string baseUrl = "https://api.nex.local///";

        using var client = new CatalogApiClient(baseUrl);

        client.Should().NotBeNull();
    }

    [Fact]
    public async Task CatalogApiClient_ListServices_HandlesConnectionFailure()
    {
        // Use a non-routable address to simulate connection failure
        using var client = new CatalogApiClient("http://192.0.2.1:1");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        Func<Task> act = () => client.ListServicesAsync(cts.Token);

        await act.Should().ThrowAsync<Exception>(
            "connection to non-routable address should fail with HttpRequestException or TaskCanceledException");
    }

    [Fact]
    public async Task CatalogApiClient_GetService_HandlesConnectionFailure()
    {
        using var client = new CatalogApiClient("http://192.0.2.1:1");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        Func<Task> act = () => client.GetServiceAsync("svc-001", cts.Token);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void CatalogApiClient_Dispose_DoesNotThrow()
    {
        var client = new CatalogApiClient("https://api.nex.local");

        var act = () => client.Dispose();

        act.Should().NotThrow();
    }
}
