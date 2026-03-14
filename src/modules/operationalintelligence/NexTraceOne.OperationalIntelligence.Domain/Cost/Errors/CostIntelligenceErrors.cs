using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.CostIntelligence.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo CostIntelligence com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: CostIntelligence.{Entidade}.{Descrição}
/// </summary>
public static class CostIntelligenceErrors
{
    /// <summary>Snapshot de custo não encontrado pelo identificador informado.</summary>
    public static Error SnapshotNotFound(string snapshotId)
        => Error.NotFound("CostIntelligence.CostSnapshot.NotFound", "Cost snapshot '{0}' was not found.", snapshotId);

    /// <summary>Shares de custo (CPU, memória, rede, storage) excedem o custo total.</summary>
    public static Error InvalidCostShares(decimal sharesSum, decimal totalCost)
        => Error.Validation("CostIntelligence.CostSnapshot.InvalidCostShares", "Sum of cost shares ({0}) exceeds total cost ({1}).", sharesSum, totalCost);

    /// <summary>Perfil de custo de serviço não encontrado.</summary>
    public static Error ProfileNotFound(string profileId)
        => Error.NotFound("CostIntelligence.ServiceCostProfile.NotFound", "Service cost profile '{0}' was not found.", profileId);

    /// <summary>O custo corrente do mês ultrapassou o orçamento definido.</summary>
    public static Error BudgetExceeded(string serviceName, decimal currentCost, decimal budget)
        => Error.Business("CostIntelligence.ServiceCostProfile.BudgetExceeded", "Service '{0}' current cost ({1}) has exceeded the monthly budget ({2}).", serviceName, currentCost, budget);

    /// <summary>Período inválido — data de início deve ser anterior à data de fim.</summary>
    public static Error InvalidPeriod(DateTimeOffset start, DateTimeOffset end)
        => Error.Validation("CostIntelligence.Period.Invalid", "Period start ({0}) must be before period end ({1}).", start, end);

    /// <summary>Atribuição de custo não encontrada pelo identificador informado.</summary>
    public static Error AttributionNotFound(string attributionId)
        => Error.NotFound("CostIntelligence.CostAttribution.NotFound", "Cost attribution '{0}' was not found.", attributionId);

    /// <summary>Valor de custo não pode ser negativo.</summary>
    public static Error NegativeCost(decimal cost)
        => Error.Validation("CostIntelligence.Cost.Negative", "Cost value ({0}) cannot be negative.", cost);

    /// <summary>Já existe um snapshot para o mesmo serviço, ambiente e período no instante informado.</summary>
    public static Error DuplicateSnapshot(string serviceName, string environment, DateTimeOffset capturedAt)
        => Error.Conflict("CostIntelligence.CostSnapshot.Duplicate", "A cost snapshot already exists for service '{0}' in environment '{1}' at '{2}'.", serviceName, environment, capturedAt);
}
