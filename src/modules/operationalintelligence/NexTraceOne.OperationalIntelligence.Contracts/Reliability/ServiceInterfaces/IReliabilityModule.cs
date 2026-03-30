namespace NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;

// IMPLEMENTATION STATUS: Planned — TODO(P03.1): implement IReliabilityModule in Infrastructure layer.

/// <summary>
/// Interface pública do módulo Reliability.
/// Outros módulos que precisarem de dados de SLO/SLA ou reliability devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre serviços.
/// </summary>
public interface IReliabilityModule
{
    /// <summary>
    /// Obtém o status de reliability atual de um serviço num ambiente.
    /// Retorna null se nenhum snapshot de reliability foi registado.
    /// </summary>
    Task<string?> GetCurrentReliabilityStatusAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o error budget restante (0.0 a 1.0) de um serviço para o período vigente.
    /// Retorna null se nenhum SLO estiver configurado.
    /// </summary>
    Task<decimal?> GetRemainingErrorBudgetAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a taxa de burn rate atual de um serviço (em múltiplos do budget diário).
    /// Retorna null se não existir baseline configurada.
    /// </summary>
    Task<decimal?> GetCurrentBurnRateAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista definições SLO activas para um serviço num ambiente.
    /// </summary>
    Task<IReadOnlyList<SloSummary>> GetServiceSlosAsync(string serviceName, string environment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumo de uma definição SLO para consumo por outros módulos.
/// Contém os dados essenciais do SLO sem expor detalhes internos do domínio.
/// </summary>
public sealed record SloSummary(
    string SloId,
    string ServiceName,
    string Environment,
    string SloType,
    decimal TargetPercentage,
    string Status);
