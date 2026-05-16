using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application;

/// <summary>
/// Helpers compartilhados para queries de product analytics.
/// Centraliza lógica duplicada entre handlers (DRY).
/// </summary>
public static class AnalyticsQueryHelper
{
    /// <summary>
    /// Resolve um range label (ex: "last_7d") para um par de datas (from, to) e o label normalizado.
    /// Respeita o limite máximo de dias configurado.
    /// </summary>
    public static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(
        DateTimeOffset utcNow, string? range, int maxDays = AnalyticsConstants.MaxRangeDays, string? defaultRange = null)
    {
        var label = string.IsNullOrWhiteSpace(range) ? (defaultRange ?? "last_30d") : range;
        var days = label switch
        {
            "last_7d" => 7,
            "last_1d" => 1,
            "last_90d" => 90,
            _ => 30
        };

        if (days > maxDays) days = maxDays;
        return (utcNow.AddDays(-days), utcNow, label);
    }

    /// <summary>
    /// Converte um <see cref="ProductModule"/> para um nome amigável de exibição.
    /// </summary>
    public static string ToModuleDisplayName(ProductModule module)
        => module switch
        {
            ProductModule.AiAssistant => "AI Assistant",
            ProductModule.SourceOfTruth => "Source of Truth",
            ProductModule.ChangeIntelligence => "Change Intelligence",
            ProductModule.ContractStudio => "Contract Studio",
            ProductModule.ServiceCatalog => "Service Catalog",
            ProductModule.IntegrationHub => "Integration Hub",
            ProductModule.ExecutiveViews => "Executive Views",
            ProductModule.DeveloperPortal => "Developer Portal",
            _ => module.ToString()
        };
}
