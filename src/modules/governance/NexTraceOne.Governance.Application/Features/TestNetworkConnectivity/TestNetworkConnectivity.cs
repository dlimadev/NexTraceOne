using System.Diagnostics;
using System.Net.Http;

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
}
