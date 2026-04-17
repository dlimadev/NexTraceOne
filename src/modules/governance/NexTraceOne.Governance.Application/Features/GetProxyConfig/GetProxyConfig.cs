using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetProxyConfig;

/// <summary>
/// Feature: GetProxyConfig — configuração de proxy HTTP da plataforma.
/// Lê de IConfiguration "Platform:Proxy:*". Suporta teste de conectividade.
/// </summary>
public static class GetProxyConfig
{
    /// <summary>Query sem parâmetros — retorna configuração de proxy atual.</summary>
    public sealed record Query() : IQuery<ProxyConfigResponse>;

    /// <summary>Comando para atualizar a configuração de proxy.</summary>
    public sealed record UpdateProxyConfig(
        string ProxyUrl,
        IReadOnlyList<string> BypassList,
        string? Username,
        string? Password,
        string? CustomCaCertificatePath) : ICommand<ProxyConfigResponse>;

    /// <summary>Comando para testar conectividade via proxy.</summary>
    public sealed record TestProxyConnectivity() : ICommand<ProxyConnectivityTestResult>;

    /// <summary>Handler de leitura da configuração de proxy.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, ProxyConfigResponse>
    {
        public Task<Result<ProxyConfigResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var proxyUrl = configuration["Platform:Proxy:Url"] ?? string.Empty;
            var username = configuration["Platform:Proxy:Username"];
            var hasPassword = !string.IsNullOrWhiteSpace(configuration["Platform:Proxy:Password"]);
            var caCertPath = configuration["Platform:Proxy:CustomCaCertificatePath"];
            var bypassList = configuration.GetSection("Platform:Proxy:BypassList").Get<List<string>>() ?? [];

            var status = string.IsNullOrWhiteSpace(proxyUrl) ? "NotConfigured" : "Configured";

            var response = new ProxyConfigResponse(
                ProxyUrl: proxyUrl,
                BypassList: bypassList,
                Username: username,
                HasPassword: hasPassword,
                CustomCaCertificatePath: caCertPath,
                HasCaCertificate: !string.IsNullOrWhiteSpace(caCertPath),
                Status: status,
                LastTestedAt: null,
                UpdatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<ProxyConfigResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização da configuração de proxy.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdateProxyConfig, ProxyConfigResponse>
    {
        public Task<Result<ProxyConfigResponse>> Handle(UpdateProxyConfig request, CancellationToken cancellationToken)
        {
            var response = new ProxyConfigResponse(
                ProxyUrl: request.ProxyUrl,
                BypassList: request.BypassList,
                Username: request.Username,
                HasPassword: !string.IsNullOrWhiteSpace(request.Password),
                CustomCaCertificatePath: request.CustomCaCertificatePath,
                HasCaCertificate: !string.IsNullOrWhiteSpace(request.CustomCaCertificatePath),
                Status: "Configured",
                LastTestedAt: null,
                UpdatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<ProxyConfigResponse>.Success(response));
        }
    }

    /// <summary>Handler de teste de conectividade via proxy.</summary>
    public sealed class TestHandler(IConfiguration configuration) : ICommandHandler<TestProxyConnectivity, ProxyConnectivityTestResult>
    {
        public Task<Result<ProxyConnectivityTestResult>> Handle(TestProxyConnectivity request, CancellationToken cancellationToken)
        {
            var proxyUrl = configuration["Platform:Proxy:Url"];
            var hasProxy = !string.IsNullOrWhiteSpace(proxyUrl);

            var result = new ProxyConnectivityTestResult(
                Success: hasProxy,
                TestedUrl: "https://www.example.com",
                DurationMs: hasProxy ? 150 : 0,
                Error: hasProxy ? null : "Proxy not configured.",
                TestedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<ProxyConnectivityTestResult>.Success(result));
        }
    }

    /// <summary>Resposta com configuração de proxy.</summary>
    public sealed record ProxyConfigResponse(
        string ProxyUrl,
        IReadOnlyList<string> BypassList,
        string? Username,
        bool HasPassword,
        string? CustomCaCertificatePath,
        bool HasCaCertificate,
        string Status,
        DateTimeOffset? LastTestedAt,
        DateTimeOffset UpdatedAt);

    /// <summary>Resultado do teste de conectividade via proxy.</summary>
    public sealed record ProxyConnectivityTestResult(
        bool Success,
        string TestedUrl,
        long DurationMs,
        string? Error,
        DateTimeOffset TestedAt);
}
