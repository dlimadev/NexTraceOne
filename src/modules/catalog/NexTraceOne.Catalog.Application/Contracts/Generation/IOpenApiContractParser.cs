using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Application.Contracts.Generation;

/// <summary>
/// Parser de especificações OpenAPI (YAML ou JSON) para o modelo neutro
/// <see cref="OpenApiContractModel"/>. A implementação concreta vive na Infrastructure
/// e isola a dependência da biblioteca de parsing de OpenAPI.
/// </summary>
public interface IOpenApiContractParser
{
    /// <summary>
    /// Faz parse do conteúdo da especificação. Retorna falha (Validation) se o conteúdo
    /// não for um OpenAPI válido.
    /// </summary>
    Result<OpenApiContractModel> Parse(string specContent);
}
