namespace NexTraceOne.CostIntelligence.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo CostIntelligence.
/// Outros módulos que precisarem de dados de custo devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre serviços.
/// </summary>
public interface ICostIntelligenceModule
{
    /// <summary>
    /// Obtém o custo total atual do mês corrente para um serviço e ambiente.
    /// Retorna null se nenhum perfil de custo foi registrado.
    /// </summary>
    Task<decimal?> GetCurrentMonthlyCostAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o score de tendência de custo (-100 a +100) para um serviço.
    /// Valores positivos indicam custo crescente, negativos indicam decrescente.
    /// </summary>
    Task<decimal?> GetCostTrendPercentageAsync(string serviceName, string environment, CancellationToken cancellationToken = default);
}
