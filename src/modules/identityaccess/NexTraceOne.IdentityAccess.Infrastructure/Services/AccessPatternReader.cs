using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de leitura de padrões de acesso de utilizadores para detecção de anomalias.
/// Consulta SecurityEvents agregados por utilizador no período lookback,
/// extraindo sinais anómalos como volume incomum, acessos fora de horário, etc.
///
/// Wave AD.3 — GetAccessPatternAnomalyReport.
/// </summary>
internal sealed class AccessPatternReader(IdentityDbContext dbContext) : IAccessPatternReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAccessEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken cancellationToken)
    {
        var tenant = TenantId.From(Guid.Parse(tenantId));
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-lookbackDays);

        // Agregação de eventos de segurança por utilizador no período
        var userSecurityEvents = await dbContext.SecurityEvents
            .Where(e => e.TenantId == tenant && e.OccurredAt >= cutoffDate && e.UserId != null)
            .ToListAsync(cancellationToken);

        // Agrupar por utilizador
        var groupedByUser = userSecurityEvents.GroupBy(e => e.UserId);

        var entries = new List<UserAccessEntry>();

        foreach (var userEvents in groupedByUser)
        {
            var userId = userEvents.Key!;
            
            // Buscar dados do utilizador
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                continue;

            // Buscar membership para obter contexto do tenant
            var membership = await dbContext.TenantMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenant, cancellationToken);

            // TeamName pode vir de uma extensão de contexto operacional, por enquanto usar null
            string? teamName = null;

            // Calcular sinais anómalos
            var allUserEvents = userEvents.ToList();
            var totalRequests = allUserEvents.Count;
            var offHoursRequests = CalculateOffHoursRequests(allUserEvents);
            var sensitiveResourceAccesses = CountSignalsByType(allUserEvents, "Sensitive");
            var unusualResourceAccesses = CountSignalsByType(allUserEvents, "Unusual");
            var bulkExportCount = CountSignalsByType(allUserEvents, "BulkExport");

            // Calcular médias diárias
            var eventsByDay = allUserEvents
                .GroupBy(e => e.OccurredAt.Date)
                .Select(g => g.Count())
                .ToList();

            var avgDailyRequests = eventsByDay.Count > 0 ? eventsByDay.Average() : 0;
            var maxDailyRequests = eventsByDay.Count > 0 ? eventsByDay.Max() : 0;

            var entry = new UserAccessEntry(
                UserId: userId.Value.ToString(),
                UserName: user.FullName.Value,
                TeamName: teamName,
                TotalRequests: totalRequests,
                OffHoursRequests: offHoursRequests,
                SensitiveResourceAccesses: sensitiveResourceAccesses,
                UnusualResourceAccesses: unusualResourceAccesses,
                BulkExportCount: bulkExportCount,
                AvgDailyRequests: avgDailyRequests,
                MaxDailyRequests: maxDailyRequests);

            entries.Add(entry);
        }

        return entries.AsReadOnly();
    }

    private static int CalculateOffHoursRequests(List<SecurityEvent> events)
    {
        return events.Count(e =>
        {
            var hour = e.OccurredAt.UtcDateTime.Hour;
            return hour < 8 || hour >= 20;
        });
    }

    private static int CountSignalsByType(List<SecurityEvent> events, string eventTypeFragment)
    {
        return events.Count(e => e.EventType.Contains(eventTypeFragment));
    }
}
