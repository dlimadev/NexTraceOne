using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Tratamento centralizado de exceções não capturadas via IExceptionHandler (ASP.NET Core 8+).
/// Registado como serviço e invocado ANTES do logging automático do ExceptionHandlerMiddleware,
/// permitindo distinguir cancelamentos legítimos de erros reais sem gerar falsos alertas.
///
/// OperationCanceledException → HTTP 499 (Client Closed Request), log Debug.
/// Outras exceções           → HTTP 500, log Error com detalhe estruturado.
///
/// Requer SuppressExceptionLogging = true no UseExceptionHandler para suprimir
/// o LogError automático do middleware e delegar o logging a esta classe.
/// </summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            logger.LogDebug(
                "Request {Method} {Path} cancelled by client",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = 499;
            return true;
        }

        if (exception is BadHttpRequestException badHttpRequest)
        {
            logger.LogDebug(
                "Bad request for {Method} {Path}: {Message}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                badHttpRequest.Message);

            httpContext.Response.StatusCode = badHttpRequest.StatusCode;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    title = "Bad Request",
                    detail = badHttpRequest.Message,
                    status = badHttpRequest.StatusCode
                },
                cancellationToken: CancellationToken.None);

            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                title = "Unexpected error",
                detail = "An unexpected error occurred while processing the request.",
                status = StatusCodes.Status500InternalServerError
            },
            cancellationToken: CancellationToken.None);

        return true;
    }
}
