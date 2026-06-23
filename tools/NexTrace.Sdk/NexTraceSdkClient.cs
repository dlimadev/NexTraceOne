using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Http.Resilience;
using NexTrace.Sdk.Clients;
using Polly;

namespace NexTrace.Sdk;

/// <summary>
/// Ponto de entrada principal do NexTrace.Sdk.
/// Agrega sub-clientes por domínio: serviços, contratos, mudanças e compliance.
/// Exemplo de uso:
/// <code>
/// var client = new NexTraceSdkClient(new NexTraceSdkOptions { BaseUrl = "...", ApiToken = "..." });
/// var svc = await client.Services.GetServiceAsync("payments", ct);
/// </code>
/// </summary>
public sealed class NexTraceSdkClient : IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>Sub-cliente para o Service Catalog.</summary>
    public ServiceCatalogClient Services { get; }

    /// <summary>Sub-cliente para Contract Governance.</summary>
    public ContractClient Contracts { get; }

    /// <summary>Sub-cliente para Change Intelligence.</summary>
    public ChangeClient Changes { get; }

    /// <summary>Sub-cliente para Compliance.</summary>
    public ComplianceClient Compliance { get; }

    /// <summary>Sub-cliente para aceleração de integrações.</summary>
    public IntegrationClient Integrations { get; }

    /// <summary>Sub-cliente para governança de segurança de dependências (supply chain).</summary>
    public SecurityClient Security { get; }

    /// <summary>
    /// Inicializa o cliente SDK com as opções fornecidas.
    /// </summary>
    public NexTraceSdkClient(NexTraceSdkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = CreateHttpClient(options);

        Services = new ServiceCatalogClient(_httpClient);
        Contracts = new ContractClient(_httpClient);
        Changes = new ChangeClient(_httpClient);
        Compliance = new ComplianceClient(_httpClient);
        Integrations = new IntegrationClient(_httpClient);
        Security = new SecurityClient(_httpClient);
    }

    /// <summary>
    /// Construtor interno para testes com HttpClient mockado.
    /// </summary>
    internal NexTraceSdkClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        Services = new ServiceCatalogClient(_httpClient);
        Contracts = new ContractClient(_httpClient);
        Changes = new ChangeClient(_httpClient);
        Compliance = new ComplianceClient(_httpClient);
        Integrations = new IntegrationClient(_httpClient);
        Security = new SecurityClient(_httpClient);
    }

    private static HttpClient CreateHttpClient(NexTraceSdkOptions options)
    {
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

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}
