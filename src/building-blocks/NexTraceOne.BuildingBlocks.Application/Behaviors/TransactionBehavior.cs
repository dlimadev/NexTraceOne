using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior que gerencia transações de banco de dados.
/// Abre transação antes do Command Handler, commit ao final.
/// Em caso de exceção, rollback automático.
/// NOTA: Apenas Commands recebem transação. Queries usam AsNoTracking.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (IsCommandRequest())
        {
            if (ResultResponseFactory.IsSuccessfulResult(response))
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }

        return response;
    }

    private static bool IsCommandRequest()
        => typeof(ICommand).IsAssignableFrom(typeof(TRequest))
           || typeof(TRequest).GetInterfaces().Any(
               i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
}
