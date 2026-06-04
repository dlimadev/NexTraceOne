using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.ClickHouse;

/// <summary>
/// Interface para repositório ClickHouse de eventos de observabilidade.
/// Fornece consultas analíticas de alta performance para runtime intelligence, SLOs e health monitoring.
/// </summary>
public interface IClickHouseRepository
{
    /// <summary>Insere um evento individual no ClickHouse.</summary>
    Task InsertEventAsync(ClickHouseEvent evt);

    /// <summary>Insere batch de eventos para melhor performance.</summary>
    Task InsertEventsBatchAsync(IEnumerable<ClickHouseEvent> events);

    /// <summary>Obtém métricas de requisições (latência, throughput, error rate) agrupadas por endpoint.</summary>
    Task<List<RequestMetrics>> GetRequestMetricsAsync(DateTime from, DateTime to, string? endpoint = null);

    /// <summary>Obtém analytics de erros agrupados por tipo com stack traces amostrais.</summary>
    Task<List<ErrorAnalytics>> GetErrorAnalyticsAsync(DateTime from, DateTime to, string? errorType = null);

    /// <summary>Obtém métricas de atividade de usuários (ações, sessões, endpoints mais usados).</summary>
    Task<List<UserActivityMetrics>> GetUserActivityAsync(DateTime from, DateTime to, string? userId = null);

    /// <summary>Obtém métricas de saúde do sistema (CPU, memória, conexões, RPS).</summary>
    Task<List<SystemHealthMetrics>> GetSystemHealthAsync(DateTime from, DateTime to, string? serviceName = null);

    /// <summary>Calcula tempo médio de resposta em um período.</summary>
    Task<double> GetAverageResponseTimeAsync(DateTime from, DateTime to, string? endpoint = null);

    /// <summary>Conta total de requisições em um período.</summary>
    Task<long> GetTotalRequestsAsync(DateTime from, DateTime to);

    /// <summary>Calcula taxa de erro percentual em um período.</summary>
    Task<double> GetErrorRateAsync(DateTime from, DateTime to);
}
