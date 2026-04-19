namespace NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;

/// <summary>
/// Chaves de configuração da plataforma para o módulo ProductAnalytics.
/// Centraliza os literais de string utilizados em consultas à IConfigurationResolutionService.
/// Os valores padrão estão em <see cref="Constants.AnalyticsConstants"/>.
/// </summary>
public static class AnalyticsConfigKeys
{
    /// <summary>
    /// Número máximo de dias permitidos numa janela de consulta de analytics.
    /// Padrão: 180. Tipo: Integer.
    /// </summary>
    public const string MaxRangeDays = "analytics.max_range_days";

    /// <summary>
    /// Número de módulos top a incluir no resumo de analytics.
    /// Padrão: 6. Tipo: Integer.
    /// </summary>
    public const string TopModulesLimit = "analytics.top_modules_limit";

    /// <summary>
    /// Número de features top a incluir no heatmap.
    /// Padrão: 5. Tipo: Integer.
    /// </summary>
    public const string TopFeaturesLimit = "analytics.top_features_limit";

    /// <summary>
    /// Threshold de variação (0.0–1.0) para classificação de tendência.
    /// Padrão: 0.05 (5%). Tipo: Decimal.
    /// </summary>
    public const string TrendThresholdPercent = "analytics.trend_threshold_percent";

    /// <summary>
    /// Período padrão quando o parâmetro range não é fornecido.
    /// Padrão: "last_30d". Tipo: String.
    /// </summary>
    public const string DefaultRange = "analytics.default_range";

    /// <summary>
    /// Número de dias de retenção de eventos de analytics.
    /// Padrão: 90. Tipo: Integer.
    /// </summary>
    public const string RetentionDays = "analytics.retention_days";
}
