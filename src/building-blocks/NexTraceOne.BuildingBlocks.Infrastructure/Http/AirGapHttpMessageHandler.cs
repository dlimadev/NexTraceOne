using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Http;

/// <summary>
/// Handler de rede que impede chamadas HTTP externas no modo AirGap.
/// Quando Platform:NetworkIsolation:Mode == "AirGap", qualquer tentativa de chamada
/// HTTP de saída é bloqueada e registada como violação de segurança.
/// </summary>
public sealed class AirGapHttpMessageHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AirGapHttpMessageHandler> _logger;

    public AirGapHttpMessageHandler(
        IConfiguration configuration,
        ILogger<AirGapHttpMessageHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var mode = _configuration["Platform:NetworkIsolation:Mode"] ?? "Off";

        if (mode.Equals("AirGap", StringComparison.OrdinalIgnoreCase))
        {
            var destination = request.RequestUri?.Host ?? "unknown";
            _logger.LogWarning(
                "[SECURITY] AirGap violation blocked: outbound HTTP request to '{Destination}' was prevented. " +
                "NetworkIsolation.Mode={Mode}. Request={Method} {Uri}",
                destination, mode, request.Method, request.RequestUri);

            throw new InvalidOperationException(
                $"Outbound HTTP request to '{destination}' is blocked by AirGap network isolation policy " +
                $"(Platform:NetworkIsolation:Mode = {mode}).");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
