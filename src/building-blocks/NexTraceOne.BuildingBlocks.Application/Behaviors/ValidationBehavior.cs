using FluentValidation;
using MediatR;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline behavior do MediatR que executa FluentValidation automaticamente
/// antes de qualquer Command Handler.
/// Se houver erros de validação, o handler NÃO é chamado — o Result de falha
/// com ErrorType.Validation é retornado diretamente.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar execução de validators e retorno de Result de falha
        throw new NotImplementedException();
    }
}
