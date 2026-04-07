using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.TrackPersonaActivity;

/// <summary>
/// Feature: TrackPersonaActivity — rastreia atividade de configuração por persona.
/// Analisa quem alterou que parâmetros, útil para auditoria e otimização da UX por persona.
/// Pilar: Persona Awareness, Audit & Traceability.
/// </summary>
public static class TrackPersonaActivity
{
    /// <summary>Query para obter atividade de configuração por persona.</summary>
    public sealed record Query(
        string? Key = null,
        int Limit = 100) : IQuery<PersonaActivityReportDto>;

    /// <summary>DTO com relatório de atividade por persona.</summary>
    public sealed record PersonaActivityReportDto(
        int TotalChanges,
        IReadOnlyList<PersonaActivityDto> ByUser,
        IReadOnlyList<ParameterActivityDto> ByParameter,
        IReadOnlyList<RecentActivityDto> RecentActivity);

    /// <summary>DTO para atividade por utilizador.</summary>
    public sealed record PersonaActivityDto(
        string UserId,
        int ChangeCount,
        DateTimeOffset? LastActivityAt);

    /// <summary>DTO para atividade por parâmetro.</summary>
    public sealed record ParameterActivityDto(
        string Key,
        int ChangeCount,
        int UniqueUsers);

    /// <summary>DTO para atividade recente.</summary>
    public sealed record RecentActivityDto(
        string Key,
        string Action,
        string ChangedBy,
        DateTimeOffset ChangedAt,
        string? Scope);

    /// <summary>Handler que recolhe dados de auditoria para gerar o relatório.</summary>
    public sealed class Handler(
        IConfigurationAuditRepository auditRepository,
        IConfigurationDefinitionRepository definitionRepository)
        : IQueryHandler<Query, PersonaActivityReportDto>
    {
        public async Task<Result<PersonaActivityReportDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var limit = request.Limit > 0 ? request.Limit : 100;

            // If a specific key is provided, get audit for that key; otherwise get all definitions
            var allAuditEntries = new List<Domain.Entities.ConfigurationAuditEntry>();

            if (!string.IsNullOrWhiteSpace(request.Key))
            {
                var entries = await auditRepository.GetByKeyAsync(request.Key, limit, cancellationToken);
                allAuditEntries.AddRange(entries);
            }
            else
            {
                var definitions = await definitionRepository.GetAllAsync(cancellationToken);
                // Get recent audit entries for each definition (limited per key for performance)
                foreach (var def in definitions)
                {
                    var entries = await auditRepository.GetByKeyAsync(def.Key, 10, cancellationToken);
                    allAuditEntries.AddRange(entries);
                }
            }

            // By user
            var byUser = allAuditEntries
                .Where(e => !string.IsNullOrWhiteSpace(e.ChangedBy))
                .GroupBy(e => e.ChangedBy)
                .Select(g => new PersonaActivityDto(
                    UserId: g.Key,
                    ChangeCount: g.Count(),
                    LastActivityAt: g.Max(e => e.ChangedAt)))
                .OrderByDescending(u => u.ChangeCount)
                .Take(20)
                .ToList();

            // By parameter
            var byParameter = allAuditEntries
                .GroupBy(e => e.Key)
                .Select(g => new ParameterActivityDto(
                    Key: g.Key,
                    ChangeCount: g.Count(),
                    UniqueUsers: g.Select(e => e.ChangedBy).Distinct().Count()))
                .OrderByDescending(p => p.ChangeCount)
                .Take(20)
                .ToList();

            // Recent activity
            var recent = allAuditEntries
                .OrderByDescending(e => e.ChangedAt)
                .Take(limit)
                .Select(e => new RecentActivityDto(
                    Key: e.Key,
                    Action: e.Action,
                    ChangedBy: e.ChangedBy,
                    ChangedAt: e.ChangedAt,
                    Scope: e.Scope.ToString()))
                .ToList();

            return new PersonaActivityReportDto(
                TotalChanges: allAuditEntries.Count,
                ByUser: byUser,
                ByParameter: byParameter,
                RecentActivity: recent);
        }
    }
}
