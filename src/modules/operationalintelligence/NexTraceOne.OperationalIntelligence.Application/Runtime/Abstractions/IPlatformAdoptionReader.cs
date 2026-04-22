namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstração de leitura de adoção de capacidades da plataforma por equipa para um tenant.
///
/// Fornece dados de utilização das sete capacidades core da plataforma NexTraceOne por equipa,
/// considerando janelas de lookback configuráveis para SLO e features.
/// Desacopla o handler de adoção de plataforma das implementações concretas de repositório.
///
/// Wave AC.3 — GetPlatformAdoptionReport.
/// </summary>
public interface IPlatformAdoptionReader
{
    /// <summary>
    /// Lista entradas de adoção de capacidades por equipa para um tenant.
    /// Cada entrada agrega o estado de utilização das sete capacidades por equipa.
    /// </summary>
    Task<IReadOnlyList<TeamCapabilityAdoptionEntry>> ListByTenantAsync(
        string tenantId,
        int sloLookbackDays,
        int featureLookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de adoção de capacidades de uma equipa.
/// Agrega o estado de utilização das sete capacidades core da plataforma.
/// Wave AC.3.
/// </summary>
public sealed record TeamCapabilityAdoptionEntry(
    /// <summary>Nome da equipa.</summary>
    string TeamName,
    /// <summary>Indica se a equipa usa SLO Tracking.</summary>
    bool UsesSloTracking,
    /// <summary>Indica se a equipa usa Chaos Engineering.</summary>
    bool UsesChaosEngineering,
    /// <summary>Indica se a equipa usa Continuous Profiling.</summary>
    bool UsesContinuousProfiling,
    /// <summary>Indica se a equipa usa Compliance Reports.</summary>
    bool UsesComplianceReports,
    /// <summary>Indica se a equipa usa Change Confidence.</summary>
    bool UsesChangeConfidence,
    /// <summary>Indica se a equipa usa Release Calendar.</summary>
    bool UsesReleaseCalendar,
    /// <summary>Indica se a equipa usa AI Assistant.</summary>
    bool UsesAiAssistant);
