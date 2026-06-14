using System.Net;
using System.Net.Sockets;
using FluentAssertions;

namespace NexTrace.Sdk.Tests;

/// <summary>
/// Testes do pipeline de resiliência do SDK.
/// Valida retry automático em falhas transitórias (5xx, timeout).
/// </summary>
public sealed class ResilienceTests : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _baseUrl;
    private int _requestCount;
    private int _failCount;

    public ResilienceTests()
    {
        _listener = new HttpListener();
        var port = GetAvailablePort();
        _baseUrl = $"http://localhost:{port}/";
        _listener.Prefixes.Add(_baseUrl);
        _listener.Start();
        _ = Task.Run(HandleRequestsAsync);
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
    }

    [Fact]
    public async Task RetryCount_GreaterThanZero_Retries_On_500()
    {
        _failCount = 2;
        _requestCount = 0;

        using var client = new NexTraceSdkClient(new NexTraceSdkOptions
        {
            BaseUrl = _baseUrl,
            RetryCount = 2,
            RetryDelaySeconds = 0,
            TimeoutSeconds = 10
        });

        var services = await client.Services.ListServicesAsync(ct: CancellationToken.None);

        services.Should().NotBeNull();
        _requestCount.Should().Be(3, "two failures should be retried before success");
    }

    [Fact]
    public async Task RetryCount_Zero_Does_Not_Retry_On_500()
    {
        _failCount = 1;
        _requestCount = 0;

        using var client = new NexTraceSdkClient(new NexTraceSdkOptions
        {
            BaseUrl = _baseUrl,
            RetryCount = 0,
            TimeoutSeconds = 10
        });

        Func<Task> act = () => client.Services.ListServicesAsync(ct: CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        _requestCount.Should().Be(1, "no retries should occur");
    }

    [Fact]
    public async Task RetryCount_Exhausted_Throws_Exception()
    {
        _failCount = 5;
        _requestCount = 0;

        using var client = new NexTraceSdkClient(new NexTraceSdkOptions
        {
            BaseUrl = _baseUrl,
            RetryCount = 2,
            RetryDelaySeconds = 0,
            TimeoutSeconds = 10
        });

        Func<Task> act = () => client.Services.ListServicesAsync(ct: CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        _requestCount.Should().Be(3, "initial request plus two retries");
    }

    private async Task HandleRequestsAsync()
    {
        while (_listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _requestCount++;

                var response = context.Response;
                if (_requestCount <= _failCount)
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Close();
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.ContentType = "application/json";
                    var bytes = System.Text.Encoding.UTF8.GetBytes("[]");
                    await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
                    response.Close();
                }
            }
            catch (HttpListenerException)
            {
                // Listener stopped.
                break;
            }
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
