namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do cache de quotas de tokens.
/// Valores em appsettings.json sob a secção "AiRuntime:QuotaCache".
/// </summary>
public sealed class TokenQuotaCacheOptions
{
    public const string SectionName = "AiRuntime:QuotaCache";

    /// <summary>TTL em segundos das entradas de uso (daily/monthly). Default: 60s.</summary>
    public int UsageTtlSeconds { get; set; } = 60;

    /// <summary>TTL em segundos das entradas de política. Default: 300s (5 min).</summary>
    public int PolicyTtlSeconds { get; set; } = 300;

    /// <summary>Número máximo de entradas no cache. Default: 10 000.</summary>
    public int MaxEntries { get; set; } = 10_000;
}
