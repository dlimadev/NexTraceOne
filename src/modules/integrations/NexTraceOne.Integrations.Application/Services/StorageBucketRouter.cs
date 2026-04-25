using Microsoft.Extensions.Caching.Memory;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Services;

/// <summary>
/// Router que avalia buckets de storage por Priority ascendente e determina
/// para qual backend encaminhar um evento de telemetria.
///
/// Buckets default por tenant: audit (2555 dias, ES), debug (3 dias, CH), default (90 dias, ES).
/// Os buckets são cacheados por 60 segundos para evitar round-trips à BD em cada evento.
/// </summary>
public sealed class StorageBucketRouter(
    IStorageBucketRepository repository,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private static readonly StorageBucketRouteResult DefaultRoute = new(
        BucketName: "default",
        BackendType: StorageBucketBackendType.Elasticsearch,
        RetentionDays: 90,
        IsDefault: true);

    /// <summary>
    /// Determina o bucket de destino para o sinal de telemetria fornecido.
    /// Avalia condições em ordem de Priority; retorna o primeiro bucket com condição satisfeita.
    /// Se nenhum bucket corresponder e existir bucket de fallback, usa-o.
    /// Caso contrário, retorna o bucket default built-in (ES, 90 dias).
    /// </summary>
    public async Task<StorageBucketRouteResult> RouteAsync(
        string signalJson,
        CancellationToken cancellationToken = default)
    {
        var buckets = await GetCachedBucketsAsync(cancellationToken);

        StorageBucket? fallback = null;

        foreach (var bucket in buckets)
        {
            if (bucket.IsFallback)
            {
                fallback ??= bucket;
                continue;
            }

            if (MatchesFilter(signalJson, bucket.FilterJson))
            {
                return new StorageBucketRouteResult(
                    BucketName: bucket.BucketName,
                    BackendType: bucket.BackendType,
                    RetentionDays: bucket.RetentionDays,
                    IsDefault: false);
            }
        }

        if (fallback is not null)
        {
            return new StorageBucketRouteResult(
                BucketName: fallback.BucketName,
                BackendType: fallback.BackendType,
                RetentionDays: fallback.RetentionDays,
                IsDefault: false);
        }

        return DefaultRoute;
    }

    /// <summary>Invalida o cache de buckets.</summary>
    public void InvalidateCache() => cache.Remove(CacheKey());

    private async Task<IReadOnlyList<StorageBucket>> GetCachedBucketsAsync(CancellationToken ct)
    {
        var key = CacheKey();
        if (cache.TryGetValue(key, out IReadOnlyList<StorageBucket>? cached) && cached is not null)
            return cached;

        var buckets = await repository.ListEnabledOrderedAsync(ct);
        cache.Set(key, buckets, CacheTtl);
        return buckets;
    }

    private static string CacheKey() => "storage-buckets";

    private static bool MatchesFilter(string signalJson, string? filterJson)
    {
        if (string.IsNullOrWhiteSpace(filterJson) || filterJson is "{}" or "null")
            return true;

        try
        {
            using var filterDoc = System.Text.Json.JsonDocument.Parse(filterJson);
            var root = filterDoc.RootElement;

            if (!root.TryGetProperty("field", out var fieldProp) ||
                !root.TryGetProperty("operator", out var opProp) ||
                !root.TryGetProperty("value", out var valueProp))
                return true;

            var fieldPath = fieldProp.GetString()?.TrimStart('$', '.') ?? string.Empty;
            var op = opProp.GetString() ?? "eq";
            var expected = valueProp.GetString() ?? string.Empty;

            var actualValue = ExtractValue(signalJson, fieldPath);
            if (actualValue is null) return false;

            return op switch
            {
                "eq" => string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase),
                "neq" => !string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase),
                "contains" => actualValue.Contains(expected, StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }
        catch
        {
            return true;
        }
    }

    private static string? ExtractValue(string json, string fieldPath)
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

/// <summary>Resultado do routing de um sinal para um bucket de storage.</summary>
public sealed record StorageBucketRouteResult(
    string BucketName,
    StorageBucketBackendType BackendType,
    int RetentionDays,
    bool IsDefault);
