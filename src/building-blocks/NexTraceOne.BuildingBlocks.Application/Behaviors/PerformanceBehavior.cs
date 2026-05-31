using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que detecta requests lentos e emite alerta de performance.
/// >500ms → Warning. >2000ms → Error com stack trace.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;
    private const int ErrorThresholdMs = 2000;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            LogIfThresholdExceeded(stopwatch.ElapsedMilliseconds, typeof(TRequest).Name);
            return response;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Request {RequestName} was cancelled after {ElapsedMilliseconds}ms",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogIfThresholdExceeded(long elapsedMs, string requestName)
    {
        if (elapsedMs >= ErrorThresholdMs)
        {
            logger.LogError(
                "Request {RequestName} exceeded critical threshold with {ElapsedMilliseconds}ms",
                requestName,
                elapsedMs);
        }
        else if (elapsedMs >= WarningThresholdMs)
        {
            logger.LogWarning(
                "Request {RequestName} exceeded warning threshold with {ElapsedMilliseconds}ms",
                requestName,
                elapsedMs);
        }
    }
}
