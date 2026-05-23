using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.TestNetworkConnectivity;

/// <summary>
/// Feature: TestNetworkConnectivity — verifica conectividade de saída real da plataforma.
/// Utiliza IHttpClientFactory para testar através do proxy configurado (se existir).
/// Destina-se a validação pós-configuração de proxy corporativo ou CA interna.
/// </summary>
public static class TestNetworkConnectivity
{
    private const string DefaultTargetUrl = "https://www.example.com";

    public sealed record Command(string? TargetUrl) : ICommand<Response>;

    public sealed record Response(
        bool Success,
        string TestedUrl,
        long DurationMs,
        int? HttpStatusCode,
        string? Error,
        DateTimeOffset TestedAt);

    public sealed class Handler(IHttpClientFactory httpClientFactory) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var targetUrl = string.IsNullOrWhiteSpace(request.TargetUrl)
                ? DefaultTargetUrl
                : request.TargetUrl;

            if (!IsAllowedTarget(targetUrl))
                return Error.Business("InvalidUrl",
                    "The target URL must use HTTPS and must not point to internal or reserved IP ranges.");


            var sw = Stopwatch.StartNew();

            try
            {
                using var client = httpClientFactory.CreateClient("network-test");
                using var httpRequest = new HttpRequestMessage(HttpMethod.Head, targetUrl);
                httpRequest.Headers.TryAddWithoutValidation("User-Agent", "NexTraceOne/NetworkTest");

                using var response = await client.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                sw.Stop();

                return Result<Response>.Success(new Response(
                    Success: true,
                    TestedUrl: targetUrl,
                    DurationMs: sw.ElapsedMilliseconds,
                    HttpStatusCode: (int)response.StatusCode,
                    Error: null,
                    TestedAt: DateTimeOffset.UtcNow));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                sw.Stop();

                return Result<Response>.Success(new Response(
                    Success: false,
                    TestedUrl: targetUrl,
                    DurationMs: sw.ElapsedMilliseconds,
                    HttpStatusCode: null,
                    Error: ex.Message,
                    TestedAt: DateTimeOffset.UtcNow));
            }
        }
    }

    /// <summary>
    /// Valida que o URL de destino é seguro para chamadas de saída.
    /// Bloqueia IPs privados, loopback e link-local (inclui 169.254.169.254 AWS metadata)
    /// para prevenir SSRF.
    /// </summary>
    internal static bool IsAllowedTarget(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return false;

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!IPAddress.TryParse(host, out var ip))
            return true;

        return !IsPrivateOrReserved(ip);
    }

    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return
                b[0] == 127 ||
                b[0] == 10 ||
                (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                (b[0] == 192 && b[1] == 168) ||
                (b[0] == 169 && b[1] == 254) ||
                b[0] == 0;
        }

        return IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;
    }
}
