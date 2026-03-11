namespace NexTraceOne.BuildingBlocks.Domain.Results;

/// <summary>
/// Padrão Result para operações que podem falhar sem lançar exceção.
/// Evita o uso de exceções para controle de fluxo em casos esperados
/// (validação, not found, conflito) e força o caller a tratar o resultado.
/// Uso: return Result.Success(value); ou return Error.NotFound("...");
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)     { _value = value; IsSuccess = true; }
    private Result(Error error) { _error = error; IsSuccess = false; }

    /// <summary>Indica se a operação foi bem-sucedida.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indica se a operação falhou.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Valor da operação bem-sucedida. Lança se IsFailure.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is failure. Access Error instead.");

    /// <summary>Erro da operação falha. Lança se IsSuccess.</summary>
    public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Result is success. Access Value instead.");

    /// <summary>Cria um Result de sucesso com o valor informado.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Cria um Result de falha com o erro informado.</summary>
    public static implicit operator Result<T>(Error error) => new(error);

    /// <summary>Conversão implícita de T para Result de sucesso.</summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Executa action caso o resultado seja sucesso.</summary>
    public Result<T> OnSuccess(Action<T> action) { if (IsSuccess) action(_value!); return this; }

    /// <summary>Executa action caso o resultado seja falha.</summary>
    public Result<T> OnFailure(Action<Error> action) { if (IsFailure) action(_error!); return this; }

    /// <summary>Projeta o valor de sucesso para outro tipo mantendo o Result.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(_value!)) : new Error(_error!.Code, _error.Message, _error.Type);
}
