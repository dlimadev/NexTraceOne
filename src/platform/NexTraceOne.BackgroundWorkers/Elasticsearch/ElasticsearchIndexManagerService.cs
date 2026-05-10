using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.BackgroundWorkers.Elasticsearch;

/// <summary>
/// Implementação do IElasticsearchIndexManager que chama a API REST do Elasticsearch
/// para aplicar políticas ILM predefinidas.
///
/// W7-01: ES ILM auto-apply.
/// </summary>
public sealed class ElasticsearchIndexManagerService : IElasticsearchIndexManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ElasticsearchIndexManagerService> _logger;

    public ElasticsearchIndexManagerService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ElasticsearchIndexManagerService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsClusterHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("/_cluster/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ElasticsearchIndexManager: cluster não acessível.");
            return false;
        }
    }

    public async Task ApplyIlmPoliciesAsync(CancellationToken cancellationToken)
    {
        var policies = BuildDefaultPolicies();

        foreach (var (policyName, policyBody) in policies)
        {
            await ApplyPolicyAsync(policyName, policyBody, cancellationToken);
        }
    }

    private async Task ApplyPolicyAsync(
        string policyName,
        object policyBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(policyBody, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(
                $"/_ilm/policy/{Uri.EscapeDataString(policyName)}",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "ElasticsearchIndexManager: política ILM '{Policy}' aplicada com sucesso.",
                    policyName);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "ElasticsearchIndexManager: falha ao aplicar política '{Policy}'. Status={Status}, Body={Body}",
                    policyName, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ElasticsearchIndexManager: exceção ao aplicar política ILM '{Policy}'.",
                policyName);
        }
    }

    private IReadOnlyDictionary<string, object> BuildDefaultPolicies()
    {
        var hotRetentionTraces = GetConfigInt("Elasticsearch:ILM:TracesHotRetentionDays", 3);
        var warmRetentionTraces = GetConfigInt("Elasticsearch:ILM:TracesWarmRetentionDays", 7);
        var deleteAfterTraces = GetConfigInt("Elasticsearch:ILM:TracesDeleteAfterDays", 30);

        var hotRetentionLogs = GetConfigInt("Elasticsearch:ILM:LogsHotRetentionDays", 3);
        var warmRetentionLogs = GetConfigInt("Elasticsearch:ILM:LogsWarmRetentionDays", 7);
        var deleteAfterLogs = GetConfigInt("Elasticsearch:ILM:LogsDeleteAfterDays", 14);

        var hotRetentionMetrics = GetConfigInt("Elasticsearch:ILM:MetricsHotRetentionDays", 1);
        var warmRetentionMetrics = GetConfigInt("Elasticsearch:ILM:MetricsWarmRetentionDays", 3);
        var deleteAfterMetrics = GetConfigInt("Elasticsearch:ILM:MetricsDeleteAfterDays", 7);

        return new Dictionary<string, object>
        {
            ["nxt-traces-policy"] = BuildIlmPolicy(hotRetentionTraces, warmRetentionTraces, deleteAfterTraces),
            ["nxt-logs-policy"] = BuildIlmPolicy(hotRetentionLogs, warmRetentionLogs, deleteAfterLogs),
            ["nxt-metrics-policy"] = BuildIlmPolicy(hotRetentionMetrics, warmRetentionMetrics, deleteAfterMetrics),
        };
    }

    private static object BuildIlmPolicy(int hotDays, int warmDays, int deleteDays)
    {
        return new
        {
            policy = new
            {
                phases = new
                {
                    hot = new
                    {
                        min_age = "0ms",
                        actions = new
                        {
                            rollover = new { max_age = $"{hotDays}d", max_size = "10gb" },
                            set_priority = new { priority = 100 },
                        }
                    },
                    warm = new
                    {
                        min_age = $"{hotDays}d",
                        actions = new
                        {
                            allocate = new { number_of_replicas = 0 },
                            shrink = new { number_of_shards = 1 },
                            set_priority = new { priority = 50 },
                        }
                    },
                    delete = new
                    {
                        min_age = $"{deleteDays}d",
                        actions = new { delete = new { } }
                    }
                }
            }
        };
    }

    private int GetConfigInt(string key, int defaultValue)
    {
        var value = _configuration[key];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
