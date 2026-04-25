using Microsoft.Extensions.Caching.Memory;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Services;

/// <summary>
/// Motor de processamento de pipeline por tenant.
/// Aplica regras ordenadas por Priority a cada sinal de telemetria ingerido,
/// executando stages de Masking → Filtering → Enrichment → Transform em sequência.
///
/// As regras são cacheadas por 60 segundos (IMemoryCache) para evitar round-trips
/// à base de dados em cada evento ingerido.
/// </summary>
public sealed class TenantPipelineEngine(
    ITenantPipelineRuleRepository ruleRepository,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Processa um sinal de telemetria passando-o pelas regras activas do tenant.
    /// </summary>
    /// <param name="signalType">Tipo de sinal (Span, Log, Metric).</param>
    /// <param name="signalJson">Payload JSON do sinal a processar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>
    /// Resultado do processamento: <c>ShouldDiscard = true</c> se alguma regra Filtering
    /// aplicou discard; <c>ProcessedJson</c> com o payload final após masking/enrichment/transform.
    /// </returns>
    public async Task<PipelineProcessingResult> ProcessAsync(
        PipelineSignalType signalType,
        string signalJson,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetCachedRulesAsync(signalType, cancellationToken);

        if (rules.Count == 0)
            return new PipelineProcessingResult(ShouldDiscard: false, ProcessedJson: signalJson, AppliedRules: []);

        var currentJson = signalJson;
        var appliedRules = new List<string>();

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (!EvaluateCondition(currentJson, rule.ConditionJson))
                continue;

            switch (rule.RuleType)
            {
                case PipelineRuleType.Masking:
                    currentJson = ApplyMasking(currentJson, rule.ActionJson);
                    appliedRules.Add(rule.Name);
                    break;

                case PipelineRuleType.Filtering:
                    if (ShouldFilter(rule.ActionJson))
                        return new PipelineProcessingResult(ShouldDiscard: true, ProcessedJson: currentJson, AppliedRules: appliedRules);
                    break;

                case PipelineRuleType.Enrichment:
                    currentJson = ApplyEnrichment(currentJson, rule.ActionJson);
                    appliedRules.Add(rule.Name);
                    break;

                case PipelineRuleType.Transform:
                    currentJson = ApplyTransform(currentJson, rule.ActionJson);
                    appliedRules.Add(rule.Name);
                    break;
            }
        }

        return new PipelineProcessingResult(ShouldDiscard: false, ProcessedJson: currentJson, AppliedRules: appliedRules);
    }

    /// <summary>Invalida o cache de regras forçando reload na próxima execução.</summary>
    public void InvalidateCache(PipelineSignalType signalType)
        => cache.Remove(CacheKey(signalType));

    private async Task<IReadOnlyList<TenantPipelineRule>> GetCachedRulesAsync(
        PipelineSignalType signalType,
        CancellationToken ct)
    {
        var key = CacheKey(signalType);
        if (cache.TryGetValue(key, out IReadOnlyList<TenantPipelineRule>? cached) && cached is not null)
            return cached;

        var rules = await ruleRepository.ListEnabledBySignalTypeAsync(signalType, ct);
        cache.Set(key, rules, CacheTtl);
        return rules;
    }

    private static string CacheKey(PipelineSignalType signalType) =>
        $"pipeline-rules:{signalType}";

    private static bool EvaluateCondition(string signalJson, string conditionJson)
    {
        try
        {
            if (conditionJson is "{}" or "null" or "")
                return true;

            using var condDoc = System.Text.Json.JsonDocument.Parse(conditionJson);
            var root = condDoc.RootElement;

            if (!root.TryGetProperty("field", out var fieldProp) ||
                !root.TryGetProperty("operator", out var opProp) ||
                !root.TryGetProperty("value", out var valueProp))
                return true;

            var fieldPath = fieldProp.GetString() ?? string.Empty;
            var op = opProp.GetString() ?? "eq";
            var expectedValue = valueProp.GetString() ?? string.Empty;

            var actualValue = ExtractFieldValue(signalJson, fieldPath);
            if (actualValue is null) return false;

            return op switch
            {
                "eq" => string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
                "neq" => !string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
                "contains" => actualValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
                "not_contains" => !actualValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }
        catch
        {
            return true;
        }
    }

    private static string? ExtractFieldValue(string signalJson, string fieldPath)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(signalJson);
            var segment = fieldPath.TrimStart('$', '.');
            var parts = segment.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = doc.RootElement;
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return null;
            }
            return current.ValueKind == System.Text.Json.JsonValueKind.String
                ? current.GetString()
                : current.GetRawText();
        }
        catch
        {
            return null;
        }
    }

    private static string ApplyMasking(string signalJson, string actionJson)
    {
        try
        {
            using var actionDoc = System.Text.Json.JsonDocument.Parse(actionJson);
            var root = actionDoc.RootElement;
            if (!root.TryGetProperty("field", out var fieldProp)) return signalJson;

            var fieldPath = fieldProp.GetString() ?? string.Empty;
            var replacement = root.TryGetProperty("replacement", out var repProp)
                ? repProp.GetString() ?? "[REDACTED]"
                : "[REDACTED]";

            return ReplaceFieldValue(signalJson, fieldPath, replacement);
        }
        catch
        {
            return signalJson;
        }
    }

    private static bool ShouldFilter(string actionJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(actionJson);
            return doc.RootElement.TryGetProperty("action", out var action) &&
                   string.Equals(action.GetString(), "discard", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string ApplyEnrichment(string signalJson, string actionJson)
    {
        try
        {
            using var actionDoc = System.Text.Json.JsonDocument.Parse(actionJson);
            if (!actionDoc.RootElement.TryGetProperty("attributes", out var attrs))
                return signalJson;

            using var signalDoc = System.Text.Json.JsonDocument.Parse(signalJson);
            var merged = new System.Text.Json.Nodes.JsonObject();

            foreach (var prop in signalDoc.RootElement.EnumerateObject())
                merged[prop.Name] = System.Text.Json.Nodes.JsonNode.Parse(prop.Value.GetRawText());

            foreach (var attr in attrs.EnumerateObject())
                merged[attr.Name] = System.Text.Json.Nodes.JsonNode.Parse(attr.Value.GetRawText());

            return merged.ToJsonString();
        }
        catch
        {
            return signalJson;
        }
    }

    private static string ApplyTransform(string signalJson, string actionJson)
    {
        try
        {
            using var actionDoc = System.Text.Json.JsonDocument.Parse(actionJson);
            var root = actionDoc.RootElement;
            if (!root.TryGetProperty("field", out var fieldProp) ||
                !root.TryGetProperty("rename", out var renameProp))
                return signalJson;

            var oldField = fieldProp.GetString()?.TrimStart('$', '.') ?? string.Empty;
            var newField = renameProp.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(oldField) || string.IsNullOrWhiteSpace(newField))
                return signalJson;

            using var signalDoc = System.Text.Json.JsonDocument.Parse(signalJson);
            var obj = new System.Text.Json.Nodes.JsonObject();

            foreach (var prop in signalDoc.RootElement.EnumerateObject())
            {
                var key = string.Equals(prop.Name, oldField, StringComparison.Ordinal) ? newField : prop.Name;
                obj[key] = System.Text.Json.Nodes.JsonNode.Parse(prop.Value.GetRawText());
            }

            return obj.ToJsonString();
        }
        catch
        {
            return signalJson;
        }
    }

    private static string ReplaceFieldValue(string signalJson, string fieldPath, string replacement)
    {
        try
        {
            var segment = fieldPath.TrimStart('$', '.');
            var parts = segment.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return signalJson;

            using var doc = System.Text.Json.JsonDocument.Parse(signalJson);
            var obj = System.Text.Json.Nodes.JsonObject.Create(doc.RootElement)!;
            SetNestedValue(obj, parts, replacement);
            return obj.ToJsonString();
        }
        catch
        {
            return signalJson;
        }
    }

    private static void SetNestedValue(System.Text.Json.Nodes.JsonObject obj, string[] parts, string replacement)
    {
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (obj[parts[i]] is System.Text.Json.Nodes.JsonObject nested)
                obj = nested;
            else
                return;
        }
        obj[parts[^1]] = replacement;
    }
}

/// <summary>Resultado do processamento de um sinal pelo pipeline.</summary>
public sealed record PipelineProcessingResult(
    bool ShouldDiscard,
    string ProcessedJson,
    IReadOnlyList<string> AppliedRules);
