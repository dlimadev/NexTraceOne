namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Leitor do snapshot de status público de um tenant.
/// Retorna apenas dados não sensíveis (serviços afetados e incidentes abertos)
/// para exibição em status page pública, sem contexto autenticado.
/// </summary>
public interface IPublicStatusReader
{
    /// <summary>Obtém o snapshot de incidentes abertos do tenant informado.</summary>
    Task<PublicStatusSnapshot> GetSnapshotAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>Snapshot de status público: incidentes abertos do tenant.</summary>
public sealed record PublicStatusSnapshot(
    IReadOnlyList<PublicStatusIncident> ActiveIncidents);

/// <summary>Incidente aberto com os campos públicos mínimos.</summary>
public sealed record PublicStatusIncident(
    string Reference,
    string Title,
    string Severity,
    string Status,
    string ServiceName,
    DateTimeOffset CreatedAt);
