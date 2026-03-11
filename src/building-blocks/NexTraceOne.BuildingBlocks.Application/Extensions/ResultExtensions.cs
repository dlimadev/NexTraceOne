using Microsoft.AspNetCore.Http;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.BuildingBlocks.Application.Extensions;

/// <summary>
/// Extensões para conversão de Result em IResult (Minimal API).
/// Mapeamento: NotFound→404, Validation→422, Conflict→409, Unauthorized→401,
/// Forbidden→403, Security→500, Business→422, Success→200.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Converte Result para IResult com mapeamento HTTP automático.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        // TODO: Implementar mapeamento de ErrorType para IResult
        throw new NotImplementedException();
    }

    /// <summary>Converte Result para Created (201) com URL do recurso criado.</summary>
    public static IResult ToCreatedResult<TId>(this Result<TId> result, string routeTemplate)
    {
        // TODO: Implementar mapeamento para Results.Created()
        throw new NotImplementedException();
    }
}
