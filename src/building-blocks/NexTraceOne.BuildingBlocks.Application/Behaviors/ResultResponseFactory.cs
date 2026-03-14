using System.Collections.Concurrent;
using System.Reflection;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Utilitário centralizado para operações de reflection sobre <see cref="Result{T}"/>.
/// Elimina duplicação de lógica de inspeção e criação de respostas Result em
/// múltiplos pipeline behaviors do MediatR. Utiliza cache de reflection para
/// evitar custo repetido de resolução de métodos e propriedades genéricos.
/// </summary>
internal static class ResultResponseFactory
{
    /// <summary>
    /// Cache de <see cref="MethodInfo"/> para o operador implícito <c>op_Implicit(Error)</c>
    /// de cada tipo <c>Result&lt;T&gt;</c> já resolvido. Evita reflection repetida.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, MethodInfo> ImplicitErrorMethodCache = new();

    /// <summary>
    /// Cache de <see cref="PropertyInfo"/> para a propriedade <c>IsSuccess</c>
    /// de cada tipo <c>Result&lt;T&gt;</c> já resolvido.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IsSuccessPropertyCache = new();

    /// <summary>
    /// Cache de <see cref="PropertyInfo"/> para a propriedade <c>Error</c>
    /// de cada tipo <c>Result&lt;T&gt;</c> já resolvido.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> ErrorPropertyCache = new();

    /// <summary>
    /// Cria uma resposta <c>Result&lt;T&gt;</c> de falha a partir de um <see cref="Error"/>,
    /// utilizando o operador implícito <c>op_Implicit(Error)</c> via reflection.
    /// Lança exceção se <typeparamref name="TResponse"/> não for <c>Result&lt;T&gt;</c>.
    /// Substitui os métodos <c>CreateFailureResponse</c> duplicados em
    /// <see cref="ValidationBehavior{TRequest, TResponse}"/> e
    /// <see cref="TenantIsolationBehavior{TRequest, TResponse}"/>.
    /// </summary>
    internal static TResponse CreateFailureResponse<TResponse>(Error error)
    {
        var responseType = typeof(TResponse);

        if (!IsResultType(responseType))
        {
            throw new InvalidOperationException($"Response type '{responseType.Name}' must be a Result<T>.");
        }

        var method = ImplicitErrorMethodCache.GetOrAdd(
            responseType,
            static type => type.GetMethod("op_Implicit", [typeof(Error)])!);

        return (TResponse)method.Invoke(null, [error])!;
    }

    /// <summary>
    /// Verifica se a resposta é um <c>Result&lt;T&gt;</c> bem-sucedido.
    /// Retorna <c>true</c> se o tipo não for <c>Result&lt;T&gt;</c> (assume sucesso
    /// para tipos que não seguem o Result Pattern).
    /// Substitui o método <c>IsSuccessfulResult</c> duplicado em
    /// <see cref="TransactionBehavior{TRequest, TResponse}"/>.
    /// </summary>
    internal static bool IsSuccessfulResult<TResponse>(TResponse response)
    {
        var responseType = typeof(TResponse);

        if (!IsResultType(responseType))
        {
            return true;
        }

        var property = IsSuccessPropertyCache.GetOrAdd(
            responseType,
            static type => type.GetProperty(nameof(Result<object>.IsSuccess)));

        return (bool)(property?.GetValue(response) ?? false);
    }

    /// <summary>
    /// Tenta extrair o estado de sucesso e o erro de uma resposta <c>Result&lt;T&gt;</c>.
    /// Retorna <c>false</c> se <typeparamref name="TResponse"/> não for <c>Result&lt;T&gt;</c>,
    /// permitindo que o chamador trate respostas genéricas de forma diferente.
    /// Substitui o método <c>TryGetResultState</c> duplicado em
    /// <see cref="LoggingBehavior{TRequest, TResponse}"/>.
    /// </summary>
    internal static bool TryGetResultState<TResponse>(
        TResponse response,
        out bool isSuccess,
        out Error? error)
    {
        var responseType = typeof(TResponse);

        if (!IsResultType(responseType))
        {
            isSuccess = false;
            error = null;
            return false;
        }

        var isSuccessProperty = IsSuccessPropertyCache.GetOrAdd(
            responseType,
            static type => type.GetProperty(nameof(Result<object>.IsSuccess)));

        isSuccess = (bool)(isSuccessProperty?.GetValue(response) ?? false);

        error = isSuccess
            ? null
            : GetErrorProperty(responseType)?.GetValue(response) as Error;

        return true;
    }

    /// <summary>
    /// Verifica se o tipo fornecido é uma instância genérica de <c>Result&lt;T&gt;</c>.
    /// </summary>
    private static bool IsResultType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);

    /// <summary>
    /// Obtém (com cache) a propriedade <c>Error</c> de um tipo <c>Result&lt;T&gt;</c>.
    /// </summary>
    private static PropertyInfo? GetErrorProperty(Type responseType)
        => ErrorPropertyCache.GetOrAdd(
            responseType,
            static type => type.GetProperty(nameof(Result<object>.Error)));
}
