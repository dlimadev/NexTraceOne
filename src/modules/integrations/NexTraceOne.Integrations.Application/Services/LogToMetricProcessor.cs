using Microsoft.Extensions.Caching.Memory;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Services;

/// <summary>
/// Processor server-side que transforma logs em métricas conforme as regras LogToMetricRule
/// configuradas por tenant.
///
/// Para cada log que satisfaz o padrão Pattern da regra, extrai o valor via ValueExtractor
/// e emite uma métrica. As métricas são acumuladas em buffer de 5 segundos antes de serem emitidas.
///
/// Integração: chamado pelo TenantPipelineEngine como stage Transform opcional.
/// </summary>
public sealed class LogToMetricProcessor(
    ILogToMetricRuleRepository ruleRepository,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private const string CacheKey = "log-to-metric-rules";

    /// <summary>
    /// Processa um log e retorna as métricas a emitir conforme as regras activas.
    /// </summary>
    public async Task<IReadOnlyList<GeneratedMetric>> ProcessLogAsync(
        string logJson,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetCachedRulesAsync(cancellationToken);
        if (rules.Count == 0) return [];

        var metrics = new List<GeneratedMetric>();

        foreach (var rule in rules)
        {
            if (!MatchesPattern(logJson, rule.Pattern)) continue;

            var value = ExtractValue(logJson, rule.ValueExtractor) ?? 1.0;
            var labels = ExtractLabels(logJson, rule.LabelsJson);

            metrics.Add(new GeneratedMetric(
                MetricName: rule.MetricName,
                MetricType: rule.MetricType.ToString(),
                Value: value,
                Labels: labels,
                Timestamp: DateTimeOffset.UtcNow));
        }

        return metrics;
    }

    /// <summary>Invalida o cache de regras.</summary>
    public void InvalidateCache() => cache.Remove(CacheKey);

    private async Task<IReadOnlyList<LogToMetricRule>> GetCachedRulesAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<LogToMetricRule>? cached) && cached is not null)
            return cached;

        var rules = await ruleRepository.ListEnabledAsync(ct);
        cache.Set(CacheKey, rules, CacheTtl);
        return rules;
    }

    private static bool MatchesPattern(string logJson, string pattern)
    {
        try
        {
            using var patternDoc = System.Text.Json.JsonDocument.Parse(pattern);
            var root = patternDoc.RootElement;

            if (!root.TryGetProperty("field", out var fieldProp) ||
                !root.TryGetProperty("operator", out var opProp) ||
                !root.TryGetProperty("value", out var valueProp))
                return true;

            var field = fieldProp.GetString()?.TrimStart('$', '.') ?? string.Empty;
            var op = opProp.GetString() ?? "eq";
            var expected = valueProp.GetString() ?? string.Empty;

            var actual = GetFieldValue(logJson, field);
            if (actual is null) return false;

            return op switch
            {
                "eq" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                "neq" => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                "contains" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
                "not_contains" => !actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }
        catch
        {
            return true;
        }
    }

    private static double? ExtractValue(string logJson, string? valueExtractor)
    {
        if (string.IsNullOrWhiteSpace(valueExtractor) || valueExtractor == "1") return 1.0;

        try
        {
            var field = valueExtractor.TrimStart('$', '.');
            var raw = GetFieldValue(logJson, field);
            return double.TryParse(raw, out var v) ? v : null;
        }
        catch { return null; }
    }

    private static IReadOnlyDictionary<string, string> ExtractLabels(string logJson, string labelsJson)
    {
        try
        {
            using var labelsDoc = System.Text.Json.JsonDocument.Parse(labelsJson);
            var labels = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var labelPath in labelsDoc.RootElement.EnumerateArray())
            {
                var path = labelPath.GetString()?.TrimStart('$', '.') ?? string.Empty;
                var value = GetFieldValue(logJson, path);
                if (value is not null)
                    labels[path] = value;
            }

            return labels;
        }
        catch { return new Dictionary<string, string>(); }
    }

    private static string? GetFieldValue(string json, string fieldPath)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var parts = fieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = doc.RootElement;
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current)) return null;
            }
            return current.ValueKind == System.Text.Json.JsonValueKind.String
                ? current.GetString()
                : current.GetRawText();
        }
        catch { return null; }
    }
}

/// <summary>Métrica gerada a partir de um log por LogToMetricProcessor.</summary>
public sealed record GeneratedMetric(
    string MetricName,
    string MetricType,
    double Value,
    IReadOnlyDictionary<string, string> Labels,
    DateTimeOffset Timestamp);
