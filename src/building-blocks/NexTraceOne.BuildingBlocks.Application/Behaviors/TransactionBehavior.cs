using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

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
        // TODO: Implementar transaction scope com commit/rollback
        throw new NotImplementedException();
    }
}
