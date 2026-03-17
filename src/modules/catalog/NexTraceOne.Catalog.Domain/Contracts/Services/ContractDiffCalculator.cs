using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Orquestrador de diff semântico multi-protocolo que delega ao calculador específico
/// do protocolo do contrato. Ponto de entrada único para computar diffs independentemente
/// do formato da especificação (OpenAPI, Swagger, AsyncAPI, WSDL).
/// Para protocolos ainda não suportados (Protobuf, GraphQL), retorna resultado vazio
/// com nível NonBreaking para não bloquear o fluxo de governança.
/// </summary>
public static class ContractDiffCalculator
{
    /// <summary>
    /// Computa o diff semântico entre duas especificações de contrato, delegando ao
    /// calculador específico do protocolo informado.
    /// </summary>
    /// <param name="baseSpec">Conteúdo da spec base (versão anterior).</param>
    /// <param name="targetSpec">Conteúdo da spec alvo (versão mais recente).</param>
    /// <param name="protocol">Protocolo do contrato que determina o parser/calculador a utilizar.</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static OpenApiDiffCalculator.DiffResult ComputeDiff(
        string baseSpec, string targetSpec, ContractProtocol protocol)
    {
        return protocol switch
        {
            ContractProtocol.OpenApi => OpenApiDiffCalculator.ComputeDiff(baseSpec, targetSpec),
            ContractProtocol.Swagger => SwaggerDiffCalculator.ComputeDiff(baseSpec, targetSpec),
            ContractProtocol.AsyncApi => AsyncApiDiffCalculator.ComputeDiff(baseSpec, targetSpec),
            ContractProtocol.Wsdl => WsdlDiffCalculator.ComputeDiff(baseSpec, targetSpec),
            _ => EmptyResult()
        };
    }

    /// <summary>
    /// Retorna resultado vazio para protocolos sem suporte a diff semântico (Protobuf, GraphQL).
    /// Utiliza nível NonBreaking para não bloquear o fluxo de governança.
    /// </summary>
    private static OpenApiDiffCalculator.DiffResult EmptyResult()
    {
        return new OpenApiDiffCalculator.DiffResult(
            Array.Empty<ChangeEntry>(),
            Array.Empty<ChangeEntry>(),
            Array.Empty<ChangeEntry>(),
            ChangeLevel.NonBreaking);
    }
}
