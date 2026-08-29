using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace NexTrace.Sdk;

/// <summary>
/// Factory de <see cref="HttpClient"/> com a política de resiliência padrão do NexTrace.Sdk
/// (retry exponencial em 5xx/timeout). Fonte única de configuração HTTP, reutilizada pelo
/// <see cref="NexTraceSdkClient"/> e pelos comandos do CLI que falam diretamente com a API.
/// </summary>
public static class NexTraceHttpClientFactory
{
    /// <summary>
    /// Cria um <see cref="HttpClient"/> configurado com base URL, timeout, autenticação Bearer
    /// e pipeline de retry (quando <see cref="NexTraceSdkOptions.RetryCount"/> &gt; 0).
    /// </summary>
    public static HttpClient Create(NexTraceSdkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        HttpClient client;
        if (options.RetryCount > 0)
        {
            var retryOptions = new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.RetryCount,
                Delay = TimeSpan.FromSeconds(options.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            };

            var resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(retryOptions)
                .Build();

            client = new HttpClient(new ResilienceHandler(resiliencePipeline) { InnerHandler = handler }, disposeHandler: true)
            {
                BaseAddress = new Uri(options.BaseUrl.TrimEnd('/')),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        }
        else
        {
            client = new HttpClient(handler, disposeHandler: true)
            {
                BaseAddress = new Uri(options.BaseUrl.TrimEnd('/')),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        }

        if (!string.IsNullOrWhiteSpace(options.ApiToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiToken);
        }

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
