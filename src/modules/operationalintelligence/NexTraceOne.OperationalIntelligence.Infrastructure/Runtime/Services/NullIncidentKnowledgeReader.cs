using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação nula de <see cref="IIncidentKnowledgeReader"/> com dados de teste codificados.
///
/// Devolve cinco tipos de incidente com dados variados para validar o comportamento
/// do handler <c>GetIncidentKnowledgeBaseReport</c> sem necessidade de base de dados.
/// Substituir por implementação real baseada em EF/PostgreSQL em produção.
///
/// Wave AB.3 — GetIncidentKnowledgeBaseReport.
/// </summary>
internal sealed class NullIncidentKnowledgeReader : IIncidentKnowledgeReader
{
    private static readonly DateTimeOffset BaseDate = new(2025, 10, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public Task<IReadOnlyList<IncidentTypeKnowledgeEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
    {
        IReadOnlyList<IncidentTypeKnowledgeEntry> entries =
        [
            // Tipo com runbook aprovado e alta efectividade
            new IncidentTypeKnowledgeEntry(
                IncidentType: "PaymentTimeout",
                TotalOccurrences: 12,
                OccurrencesWithApprovedRunbook: 12,
                AvgTimeToRunbookMinutes: 3.5,
                RunbookEffectiveResolutions: 11,
                HasApprovedRunbook: true,
                IsRunbookStale: false,
                LastOccurredAt: BaseDate.AddDays(-5),
                IsTrendIncreasing: false),

            // Tipo com runbook mas stale
            new IncidentTypeKnowledgeEntry(
                IncidentType: "DatabaseConnectionPool",
                TotalOccurrences: 8,
                OccurrencesWithApprovedRunbook: 6,
                AvgTimeToRunbookMinutes: 8.2,
                RunbookEffectiveResolutions: 5,
                HasApprovedRunbook: true,
                IsRunbookStale: true,
                LastOccurredAt: BaseDate.AddDays(-12),
                IsTrendIncreasing: true),

            // Tipo recorrente sem runbook (knowledge gap)
            new IncidentTypeKnowledgeEntry(
                IncidentType: "OrderProcessingFailure",
                TotalOccurrences: 15,
                OccurrencesWithApprovedRunbook: 0,
                AvgTimeToRunbookMinutes: 0,
                RunbookEffectiveResolutions: 0,
                HasApprovedRunbook: false,
                IsRunbookStale: false,
                LastOccurredAt: BaseDate.AddDays(-3),
                IsTrendIncreasing: true),

            // Tipo raro sem runbook (sem knowledge gap — ocorrências ≤ 3)
            new IncidentTypeKnowledgeEntry(
                IncidentType: "CacheEviction",
                TotalOccurrences: 2,
                OccurrencesWithApprovedRunbook: 0,
                AvgTimeToRunbookMinutes: 0,
                RunbookEffectiveResolutions: 0,
                HasApprovedRunbook: false,
                IsRunbookStale: false,
                LastOccurredAt: BaseDate.AddDays(-45),
                IsTrendIncreasing: false),

            // Tipo com runbook de efectividade parcial
            new IncidentTypeKnowledgeEntry(
                IncidentType: "StockSyncFailure",
                TotalOccurrences: 5,
                OccurrencesWithApprovedRunbook: 4,
                AvgTimeToRunbookMinutes: 12.0,
                RunbookEffectiveResolutions: 2,
                HasApprovedRunbook: true,
                IsRunbookStale: false,
                LastOccurredAt: BaseDate.AddDays(-20),
                IsTrendIncreasing: false),
        ];

        return Task.FromResult(entries);
    }
}
