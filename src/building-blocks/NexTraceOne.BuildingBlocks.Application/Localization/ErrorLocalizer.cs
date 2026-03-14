using Microsoft.Extensions.Localization;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Localization;

/// <summary>
/// Implementação padrão de localização de erros baseada em recursos compartilhados.
/// </summary>
public sealed class ErrorLocalizer(IStringLocalizer<SharedMessages> localizer) : IErrorLocalizer
{
    /// <inheritdoc />
    public string Localize(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var localized = localizer[error.Code];
        var template = localized.ResourceNotFound ? error.Message : localized.Value;

        return Format(template, error.FormattedMessage, error.MessageArgs);
    }

    /// <inheritdoc />
    public string LocalizeTitle(ErrorType errorType)
    {
        var key = $"Title.{errorType}";
        var localized = localizer[key];

        if (!localized.ResourceNotFound)
        {
            return localized.Value;
        }

        return errorType switch
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

    private static string Format(string template, string fallback, object[] args)
    {
        if (args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }
}
