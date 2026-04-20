namespace NexTraceOne.Catalog.Application.Graph.ConfigurationKeys;

/// <summary>
/// Chaves de configuração do módulo Catalog — Service Tier e Ownership Drift.
/// Todos os valores são geridos via IConfigurationResolutionService + ConfigurationDefinitionSeeder.
/// </summary>
public static class ServiceCatalogConfigKeys
{
    /// <summary>Dias sem revisão de ownership antes de um serviço ser considerado em drift.</summary>
    public const string OwnershipDriftThresholdDays = "catalog.ownershipDrift.threshold.days";

    /// <summary>SLO mínimo (%) para serviços de tier Critical.</summary>
    public const string TierCriticalSloMinPercent = "catalog.tier.critical.sloMinPercent";

    /// <summary>SLO mínimo (%) para serviços de tier Standard.</summary>
    public const string TierStandardSloMinPercent = "catalog.tier.standard.sloMinPercent";

    /// <summary>SLO mínimo (%) para serviços de tier Experimental.</summary>
    public const string TierExperimentalSloMinPercent = "catalog.tier.experimental.sloMinPercent";

    /// <summary>Score mínimo de maturidade (0-1) para serviços de tier Critical.</summary>
    public const string TierCriticalMaturityMinScore = "catalog.tier.critical.maturityMinScore";

    /// <summary>Score mínimo de maturidade (0-1) para serviços de tier Standard.</summary>
    public const string TierStandardMaturityMinScore = "catalog.tier.standard.maturityMinScore";
}
