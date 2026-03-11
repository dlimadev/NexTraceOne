using MediatR;
using Microsoft.Extensions.Logging;

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
        // TODO: Implementar logging com Stopwatch e log estruturado
        throw new NotImplementedException();
    }
}
