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

    /// <summary>
    /// Obtém a previsão de orçamento mais recente para um serviço e ambiente.
    /// Retorna null se nenhuma previsão foi calculada.
    /// </summary>
    Task<BudgetForecastSummary?> GetLatestBudgetForecastAsync(string serviceId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista recomendações de eficiência não reconhecidas para consumo por outros módulos.
    /// </summary>
    Task<IReadOnlyList<EfficiencyRecommendationSummary>> GetUnacknowledgedRecommendationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o contexto de custo por dia (actual e baseline) para um serviço num ambiente.
    /// Usado pelo gate de budget na promoção de releases para comparar custo da release vs baseline.
    /// Retorna null se nenhum dado de custo estiver disponível.
    /// </summary>
    Task<CostContextPerDay?> GetCostContextPerDayAsync(string serviceName, string environment, CancellationToken cancellationToken = default);
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

public sealed record BudgetForecastSummary(
    Guid ForecastId,
    string ServiceId,
    string ForecastPeriod,
    decimal ProjectedCost,
    decimal? BudgetLimit,
    bool IsOverBudgetProjected,
    string Method,
    DateTimeOffset ComputedAt);

public sealed record EfficiencyRecommendationSummary(
    Guid Id,
    string ServiceId,
    string ServiceName,
    decimal DeviationPercent,
    string RecommendationText,
    string Priority);

/// <summary>
/// Contexto de custo por dia para um serviço num ambiente.
/// ActualCostPerDay: custo médio diário do mês corrente.
/// BaselineCostPerDay: custo médio diário do mês anterior (usado como baseline de comparação).
/// Currency: moeda do custo (ex: "USD").
/// </summary>
public sealed record CostContextPerDay(
    decimal ActualCostPerDay,
    decimal BaselineCostPerDay,
    string Currency,
    string ServiceName,
    string Environment);
