using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Services;

/// <summary>
/// Implementação nula de <see cref="IPlatformAdoptionReader"/> com dados de teste codificados.
///
/// Devolve cinco equipas com padrões de adoção variados para validar o comportamento
/// do handler <c>GetPlatformAdoptionReport</c> sem necessidade de base de dados.
/// Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AC.3 — GetPlatformAdoptionReport.
/// </summary>
public sealed class NullPlatformAdoptionReader : IPlatformAdoptionReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<TeamCapabilityAdoptionEntry>> ListByTenantAsync(
        string tenantId,
        int sloLookbackDays,
        int featureLookbackDays,
        CancellationToken ct)
    {
        IReadOnlyList<TeamCapabilityAdoptionEntry> entries =
        [
            // Equipa Pioneer — adota todas as capacidades
            new TeamCapabilityAdoptionEntry(
                TeamName: "team-payments",
                UsesSloTracking: true,
                UsesChaosEngineering: true,
                UsesContinuousProfiling: true,
                UsesComplianceReports: true,
                UsesChangeConfidence: true,
                UsesReleaseCalendar: true,
                UsesAiAssistant: true),

            // Equipa Adopter — usa 5 de 7 capacidades
            new TeamCapabilityAdoptionEntry(
                TeamName: "team-logistics",
                UsesSloTracking: true,
                UsesChaosEngineering: false,
                UsesContinuousProfiling: true,
                UsesComplianceReports: true,
                UsesChangeConfidence: true,
                UsesReleaseCalendar: true,
                UsesAiAssistant: false),

            // Equipa Explorer — usa 3 de 7 capacidades
            new TeamCapabilityAdoptionEntry(
                TeamName: "team-platform",
                UsesSloTracking: true,
                UsesChaosEngineering: false,
                UsesContinuousProfiling: false,
                UsesComplianceReports: false,
                UsesChangeConfidence: true,
                UsesReleaseCalendar: true,
                UsesAiAssistant: false),

            // Equipa Laggard — usa apenas 1 capacidade
            new TeamCapabilityAdoptionEntry(
                TeamName: "team-legacy",
                UsesSloTracking: true,
                UsesChaosEngineering: false,
                UsesContinuousProfiling: false,
                UsesComplianceReports: false,
                UsesChangeConfidence: false,
                UsesReleaseCalendar: false,
                UsesAiAssistant: false),

            // Equipa sem adoção — Laggard absoluto
            new TeamCapabilityAdoptionEntry(
                TeamName: "team-external",
                UsesSloTracking: false,
                UsesChaosEngineering: false,
                UsesContinuousProfiling: false,
                UsesComplianceReports: false,
                UsesChangeConfidence: false,
                UsesReleaseCalendar: false,
                UsesAiAssistant: false),
        ];

        return Task.FromResult(entries);
    }
}
