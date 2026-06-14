using System.Net;
using System.Net.Http.Headers;

namespace NexTrace.Sdk.Tests;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
    public List<HttpRequestMessage> Requests { get; } = new();

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return await _handler(request, cancellationToken).ConfigureAwait(false);
    }

    public static MockHttpMessageHandler WithJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new MockHttpMessageHandler((_, ct) =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = CreateJsonContent(json)
            };
            return Task.FromResult(response);
        });
    }

    public static StringContent CreateJsonContent(string json)
    {
        var content = new StringContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
