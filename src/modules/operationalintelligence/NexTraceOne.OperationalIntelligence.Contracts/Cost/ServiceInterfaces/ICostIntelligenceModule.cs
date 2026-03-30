namespace NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

// IMPLEMENTATION STATUS: Implemented — CostIntelligenceModuleService (Infrastructure).

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

    /// <summary>
    /// Lista registos de custo importados, opcionalmente filtrados por período.
    /// </summary>
    Task<IReadOnlyList<CostRecordSummary>> GetCostRecordsAsync(string? period = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o custo de um serviço específico, opcionalmente filtrado por período.
    /// Retorna null se nenhum registo for encontrado.
    /// </summary>
    Task<CostRecordSummary?> GetServiceCostAsync(string serviceId, string? period = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista registos de custo de uma equipa específica, opcionalmente filtrados por período.
    /// </summary>
    Task<IReadOnlyList<CostRecordSummary>> GetCostsByTeamAsync(string team, string? period = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista registos de custo de um domínio específico, opcionalmente filtrados por período.
    /// </summary>
    Task<IReadOnlyList<CostRecordSummary>> GetCostsByDomainAsync(string domain, string? period = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumo de um registo de custo para consumo por outros módulos.
/// Contém os dados essenciais de atribuição de custo a serviço/equipa/domínio.
/// </summary>
public sealed record CostRecordSummary(
    string ServiceId,
    string ServiceName,
    string? Team,
    string? Domain,
    string? Environment,
    decimal TotalCost,
    string Currency,
    string Period,
    string Source);
