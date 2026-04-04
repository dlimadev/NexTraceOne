namespace NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

// IMPLEMENTATION STATUS: Implemented — RuntimeIntelligenceModule (Infrastructure).

/// <summary>
/// Interface pública do módulo RuntimeIntelligence.
/// Outros módulos que precisarem de dados de runtime devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre serviços.
/// </summary>
public interface IRuntimeIntelligenceModule
{
    /// <summary>
    /// Obtém o status de saúde atual de um serviço e ambiente.
    /// Retorna null se nenhum snapshot de runtime foi capturado.
    /// </summary>
    Task<string?> GetCurrentHealthStatusAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o score de observabilidade (0.0 a 1.0) para um serviço.
    /// Retorna null se o perfil de observabilidade ainda não foi avaliado.
    /// </summary>
    Task<decimal?> GetObservabilityScoreAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém métricas agregadas de runtime (latência média e taxa de erro) para um serviço.
    /// Calcula a média dos snapshots mais recentes (últimas 24 horas).
    /// Retorna null se nenhum snapshot de runtime foi capturado.
    /// </summary>
    Task<ServiceRuntimeMetrics?> GetServiceMetricsAsync(string serviceName, string environment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Métricas de runtime agregadas para um serviço num ambiente específico.
/// Valores derivados dos RuntimeSnapshots mais recentes.
/// AverageLatencyMs: latência média em milissegundos.
/// ErrorRate: taxa de erro como fração entre 0 e 1 (ex: 0.05 = 5%).
/// SampleCount: número de snapshots usados no cálculo.
/// </summary>
public sealed record ServiceRuntimeMetrics(
    long AverageLatencyMs,
    decimal ErrorRate,
    int SampleCount);
