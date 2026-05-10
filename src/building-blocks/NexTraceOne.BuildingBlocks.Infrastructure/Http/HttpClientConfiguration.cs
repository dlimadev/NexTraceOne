using System.Net;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Configura o handler primário de todos os HttpClients com proxy corporativo
/// e certificado CA interno, lidos de Platform:Proxy:*.
///
/// Proxy:
///   Platform:Proxy:Url               — ex. "http://proxy.corp:3128"
///   Platform:Proxy:Username           — credenciais opcionais
///   Platform:Proxy:Password
///   Platform:Proxy:BypassList         — array JSON de hostnames a ignorar
///
/// CA interna:
///   Platform:Proxy:CustomCaCertificatePath — caminho para ficheiro .pem/.crt
/// </summary>
public sealed class HttpClientConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HttpClientConfiguration> _logger;

    public HttpClientConfiguration(
        IConfiguration configuration,
        ILogger<HttpClientConfiguration> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Cria e configura um SocketsHttpHandler com proxy e CA customizados.
    /// Retorna null quando nenhuma configuração de proxy estiver presente.
    /// </summary>
    public SocketsHttpHandler? BuildHandler()
    {
        var proxyUrl = _configuration["Platform:Proxy:Url"];
        var caCertPath = _configuration["Platform:Proxy:CustomCaCertificatePath"];

        var hasProxy = !string.IsNullOrWhiteSpace(proxyUrl);
        var hasCa = !string.IsNullOrWhiteSpace(caCertPath);

        if (!hasProxy && !hasCa)
            return null;

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        if (hasProxy)
        {
            var proxy = BuildWebProxy(proxyUrl!);
            handler.UseProxy = true;
            handler.Proxy = proxy;
            _logger.LogInformation(
                "HttpClientConfiguration: proxy configurado em {ProxyUrl}.",
                MaskCredentials(proxyUrl!));
        }

        if (hasCa)
        {
            ApplyCustomCaCertificate(handler, caCertPath!);
        }

        return handler;
    }

    private WebProxy BuildWebProxy(string proxyUrl)
    {
        var bypassList = _configuration
            .GetSection("Platform:Proxy:BypassList")
            .Get<string[]>() ?? [];

        var username = _configuration["Platform:Proxy:Username"];
        var password = _configuration["Platform:Proxy:Password"];

        var proxy = new WebProxy(proxyUrl, bypassOnLocal: true)
        {
            BypassList = bypassList,
        };

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            proxy.Credentials = new NetworkCredential(username, password);
            _logger.LogDebug(
                "HttpClientConfiguration: credenciais de proxy configuradas para o utilizador '{User}'.",
                username);
        }
        else
        {
            proxy.UseDefaultCredentials = true;
        }

        return proxy;
    }

    private void ApplyCustomCaCertificate(SocketsHttpHandler handler, string caCertPath)
    {
        if (!File.Exists(caCertPath))
        {
            _logger.LogWarning(
                "HttpClientConfiguration: ficheiro de CA não encontrado em '{Path}' — a ignorar.",
                caCertPath);
            return;
        }

        try
        {
            var cert = X509Certificate2.CreateFromPemFile(caCertPath);

            handler.SslOptions.RemoteCertificateValidationCallback =
                (_, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        return true;

                    if (chain is null || certificate is null)
                        return false;

                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(cert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    return chain.Build(new X509Certificate2(certificate));
                };

            _logger.LogInformation(
                "HttpClientConfiguration: CA interna carregada de '{Path}'.",
                caCertPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HttpClientConfiguration: falha ao carregar CA de '{Path}'.",
                caCertPath);
        }
    }

    private static string MaskCredentials(string url)
    {
        try
        {
            var uri = new Uri(url);
            if (string.IsNullOrEmpty(uri.UserInfo))
                return url;
            return url.Replace(uri.UserInfo + "@", "***@");
        }
        catch
        {
            return url;
        }
    }
}
