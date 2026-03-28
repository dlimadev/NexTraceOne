namespace NexTraceOne.ChangeGovernance.Contracts.Promotion.ServiceInterfaces;

// IMPLEMENTATION STATUS: Implemented — PromotionModuleService in ChangeGovernance.Infrastructure.

/// <summary>
/// Interface pública do módulo Promotion.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IPromotionModule
{
    /// <summary>Verifica se a promoção de uma release para o ambiente destino está aprovada.</summary>
    Task<bool> IsPromotionApprovedAsync(Guid releaseId, Guid targetEnvironmentId, CancellationToken cancellationToken);

    /// <summary>Obtém o status atual da promoção de uma release.</summary>
    Task<string?> GetPromotionStatusAsync(Guid promotionRequestId, CancellationToken cancellationToken);
}
