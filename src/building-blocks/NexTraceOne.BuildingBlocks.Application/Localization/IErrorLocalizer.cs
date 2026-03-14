using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.BuildingBlocks.Application.Localization;

/// <summary>
/// Contrato para localização de erros e títulos de Problem Details.
/// </summary>
public interface IErrorLocalizer
{
    /// <summary>Localiza a mensagem de um erro a partir do código i18n.</summary>
    string Localize(Error error);

    /// <summary>Localiza o título padrão associado ao tipo do erro.</summary>
    string LocalizeTitle(ErrorType errorType);
}
