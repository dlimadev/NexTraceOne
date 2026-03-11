using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Domain.Results;
using System.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que loga entrada, saída e duração de cada request MediatR.
/// Log de entrada inclui: tipo do request e dados (sem informação sensível).
/// Log de saída inclui: sucesso/falha e tempo de execução em ms.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling request {RequestName}: {@Request}", requestName, request);

        var response = await next();

        stopwatch.Stop();

        if (TryGetResultState(response, out var isSuccess, out var error))
        {
            if (isSuccess)
            {
                logger.LogInformation(
                    "Request {RequestName} completed successfully in {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarning(
                    "Request {RequestName} failed in {ElapsedMilliseconds}ms with code {ErrorCode}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    error?.Code);
            }
        }
        else
        {
            logger.LogInformation(
                "Request {RequestName} completed in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }

    private static bool TryGetResultState(TResponse response, out bool isSuccess, out Error? error)
    {
        var responseType = typeof(TResponse);

        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            isSuccess = false;
            error = null;
            return false;
        }

        isSuccess = (bool)(responseType.GetProperty(nameof(Result<object>.IsSuccess))?.GetValue(response) ?? false);
        error = isSuccess
            ? null
            : responseType.GetProperty(nameof(Result<object>.Error))?.GetValue(response) as Error;

        return true;
    }
}
