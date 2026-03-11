using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Domain.Results;

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
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .Select(error => error.ErrorMessage)
            .Distinct()
            .ToArray();

        if (failures.Length == 0)
        {
            return await next();
        }

        var error = Error.Validation(
            "Validation.Failed",
            "Validation failed for one or more fields: {0}",
            string.Join("; ", failures));

        return CreateFailureResponse(error);
    }

    private static TResponse CreateFailureResponse(Error error)
    {
        var responseType = typeof(TResponse);

        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            throw new InvalidOperationException($"Response type '{responseType.Name}' must be a Result<T>.");
        }

        return (TResponse)(responseType
            .GetMethod("op_Implicit", [typeof(Error)])!
            .Invoke(null, [error])!);
    }
}
