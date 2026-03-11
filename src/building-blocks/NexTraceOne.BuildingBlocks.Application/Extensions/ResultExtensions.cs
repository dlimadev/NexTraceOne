using Microsoft.AspNetCore.Http;
using NexTraceOne.BuildingBlocks.Application.Localization;
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
        => ToHttpResult(result, null);

    /// <summary>Converte Result para IResult com mensagens localizadas.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result, IErrorLocalizer? localizer)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var error = result.Error;
        var title = localizer?.LocalizeTitle(error.Type) ?? GetFallbackTitle(error.Type);
        var detail = localizer?.Localize(error) ?? error.FormattedMessage;

        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: GetStatusCode(error.Type),
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["type"] = error.Type.ToString()
            });
    }

    /// <summary>Converte Result para Created (201) com URL do recurso criado.</summary>
    public static IResult ToCreatedResult<TId>(this Result<TId> result, string routeTemplate)
        => ToCreatedResult(result, routeTemplate, null);

    /// <summary>Converte Result para Created (201) com suporte a localização no fallback de erro.</summary>
    public static IResult ToCreatedResult<TId>(this Result<TId> result, string routeTemplate, IErrorLocalizer? localizer)
    {
        if (result.IsFailure)
        {
            return result.ToHttpResult(localizer);
        }

        var location = string.Format(routeTemplate, result.Value);
        return Results.Created(location, result.Value);
    }

    private static int GetStatusCode(ErrorType type)
        => type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Security => StatusCodes.Status500InternalServerError,
            ErrorType.Business => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetFallbackTitle(ErrorType type)
        => type switch
        {
            ErrorType.NotFound => "Not found",
            ErrorType.Validation => "Validation failed",
            ErrorType.Conflict => "Conflict detected",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Security => "Security error",
            ErrorType.Business => "Business rule violation",
            _ => "Unexpected error"
        };
}
