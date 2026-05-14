using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.HealthChecks;

/// <summary>
/// Health check para verificar conectividade com ClickHouse (analytics).
/// Retorna Degraded se ClickHouse não estiver configurado (usando NullAiAnalyticsRepository).
/// </summary>
public sealed class ClickHouseAiHealthCheck : IHealthCheck
{
    private readonly IAiAnalyticsRepository _repository;

    public ClickHouseAiHealthCheck(IAiAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Verifica se é a implementação Null (ClickHouse não configurado)
        if (_repository.GetType().Name.Contains("Null"))
        {
            return new HealthCheckResult(
                HealthStatus.Degraded,
                "ClickHouse analytics não está configurado. Usando fallback PostgreSQL.",
                data: new Dictionary<string, object>
                {
                    ["configured"] = false,
                    ["type"] = "ClickHouse",
                    ["recommendation"] = "Configure ConnectionString:AiAnalytics para habilitar analytics em tempo real"
                });
        }

        try
        {
            // Tenta executar uma query simples para verificar conectividade
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;
            
            // Query leve apenas para testar conexão
            await _repository.GetTotalAgentExecutionsAsync(from, to);

            return HealthCheckResult.Healthy(
                "ClickHouse analytics está operacional.",
                data: new Dictionary<string, object>
                {
                    ["configured"] = true,
                    ["type"] = "ClickHouse",
                    ["status"] = "healthy"
                });
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"ClickHouse analytics falhou: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["configured"] = true,
                    ["type"] = "ClickHouse",
                    ["status"] = "unhealthy",
                    ["error"] = ex.Message
                });
        }
    }
}

/// <summary>
/// Health check para verificar conectividade com ElasticSearch (search).
/// Retorna Degraded se ElasticSearch não estiver configurado (usando NullAiSearchRepository).
/// </summary>
public sealed class ElasticSearchAiHealthCheck : IHealthCheck
{
    private readonly IAiSearchRepository _repository;

    public ElasticSearchAiHealthCheck(IAiSearchRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Verifica se é a implementação Null (ElasticSearch não configurado)
        if (_repository.GetType().Name.Contains("Null"))
        {
            return new HealthCheckResult(
                HealthStatus.Degraded,
                "ElasticSearch search não está configurado. Usando fallback PostgreSQL.",
                data: new Dictionary<string, object>
                {
                    ["configured"] = false,
                    ["type"] = "ElasticSearch",
                    ["recommendation"] = "Configure ConnectionString:AiSearch para habilitar busca full-text avançada"
                });
        }

        try
        {
            // Tenta executar uma busca simples para verificar conectividade
            await _repository.SearchPromptsAsync(
                query: "health_check_test",
                page: 1,
                pageSize: 1);

            return HealthCheckResult.Healthy(
                "ElasticSearch search está operacional.",
                data: new Dictionary<string, object>
                {
                    ["configured"] = true,
                    ["type"] = "ElasticSearch",
                    ["status"] = "healthy"
                });
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"ElasticSearch search falhou: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["configured"] = true,
                    ["type"] = "ElasticSearch",
                    ["status"] = "unhealthy",
                    ["error"] = ex.Message
                });
        }
    }
}
