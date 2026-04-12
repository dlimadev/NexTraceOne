using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do serviço de retrieval de dados estruturados para grounding de IA.
/// Consulta:
///   1. Catálogo de serviços (quando ServiceId ou EntityType=Service fornecidos)
///   2. Releases recentes do ChangeIntelligence (janela configurável, padrão 7 dias)
///   3. Incidentes recentes do OperationalIntelligence (janela configurável, padrão 30 dias)
///   4. Modelos de IA (fallback para queries genéricas)
/// Todas as queries são somente-leitura via interfaces ICatalogGroundingReader, IChangeGroundingReader,
/// IIncidentGroundingReader — nunca escreve em módulos externos.
/// Falhas são silenciosas — log de aviso e resultado parcial, nunca bloqueia o pipeline de IA.
/// </summary>
public sealed class DatabaseRetrievalService : IDatabaseRetrievalService
{
    private const int MaxSnippetLength = 200;

    private readonly IAiModelRepository _modelRepository;
    private readonly ICatalogGroundingReader _catalogReader;
    private readonly IChangeGroundingReader _changeReader;
    private readonly IIncidentGroundingReader _incidentReader;
    private readonly ILogger<DatabaseRetrievalService> _logger;

    public DatabaseRetrievalService(
        IAiModelRepository modelRepository,
        ICatalogGroundingReader catalogReader,
        IChangeGroundingReader changeReader,
        IIncidentGroundingReader incidentReader,
        ILogger<DatabaseRetrievalService> logger)
    {
        _modelRepository = modelRepository;
        _catalogReader = catalogReader;
        _changeReader = changeReader;
        _incidentReader = incidentReader;
        _logger = logger;
    }

    public async Task<DatabaseSearchResult> SearchAsync(
        DatabaseSearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Database grounding retrieval: query='{Query}' entityType='{EntityType}' serviceId='{ServiceId}'",
            request.Query, request.EntityType, request.ServiceId);

        var hits = new List<DatabaseSearchHit>();

        // ── 1. Service context from Catalog ───────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.ServiceId)
            || string.Equals(request.EntityType, "Service", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var services = await _catalogReader.FindServicesAsync(
                    request.ServiceId, request.Query, maxResults: 3, ct);

                foreach (var svc in services)
                {
                    var summary = $"Service: {svc.DisplayName}, Team: {svc.TeamName}, " +
                                  $"Domain: {svc.Domain}, Criticality: {svc.Criticality}, " +
                                  $"Lifecycle: {svc.Lifecycle}, Type: {svc.ServiceType}. " +
                                  Truncate(svc.Description, MaxSnippetLength);

                    hits.Add(new DatabaseSearchHit(
                        EntityType: "Service",
                        EntityId: svc.ServiceId,
                        DisplayName: svc.DisplayName,
                        Summary: summary,
                        RelevanceScore: 0.95,
                        IsTruncated: WasTruncated(svc.Description, MaxSnippetLength)));
                }

                _logger.LogDebug(
                    "Catalog grounding: {Count} services retrieved for serviceId='{ServiceId}'",
                    services.Count, request.ServiceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Catalog grounding failed for serviceId='{ServiceId}' — returning partial result",
                    request.ServiceId);
            }
        }

        // ── 2. Recent changes from ChangeIntelligence ─────────────────────────────────────────
        try
        {
            var from = DateTimeOffset.UtcNow.AddDays(-request.ChangesWindowDays);
            var to = DateTimeOffset.UtcNow;
            var tenantId = ParseGuid(request.TenantId);

            var releases = await _changeReader.FindRecentReleasesAsync(
                from, to, request.ServiceId, request.Environment, tenantId,
                request.MaxChanges, ct);

            foreach (var release in releases)
            {
                var summary = $"Release on {release.ServiceName} v{release.Version} " +
                              $"in {release.Environment} — Status: {release.Status}, " +
                              $"ChangeLevel: {release.ChangeLevel}, Risk: {release.ChangeScore:F2}. " +
                              Truncate(release.Description, MaxSnippetLength);

                hits.Add(new DatabaseSearchHit(
                    EntityType: "Release",
                    EntityId: release.ReleaseId,
                    DisplayName: $"{release.ServiceName} v{release.Version} ({release.Environment})",
                    Summary: summary,
                    RelevanceScore: 0.85,
                    IsTruncated: WasTruncated(release.Description, MaxSnippetLength)));
            }

            _logger.LogDebug(
                "ChangeIntelligence grounding: {Count} releases in last {Days} days for serviceId='{ServiceId}'",
                releases.Count, request.ChangesWindowDays, request.ServiceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ChangeIntelligence grounding failed for serviceId='{ServiceId}' — continuing",
                request.ServiceId);
        }

        // ── 3. Recent incidents from OperationalIntelligence ─────────────────────────────────
        try
        {
            var from = DateTimeOffset.UtcNow.AddDays(-request.IncidentsWindowDays);

            var incidents = await _incidentReader.FindRecentIncidentsAsync(
                from, request.ServiceId, request.Environment, request.MaxIncidents, ct);

            foreach (var incident in incidents)
            {
                var summary = $"Incident: {incident.Title} ({incident.Severity}) " +
                              $"on {incident.ServiceName}, Status: {incident.Status}, " +
                              $"Detected: {incident.DetectedAt:yyyy-MM-dd HH:mm} UTC. " +
                              Truncate(incident.Description, MaxSnippetLength);

                hits.Add(new DatabaseSearchHit(
                    EntityType: "Incident",
                    EntityId: incident.IncidentId,
                    DisplayName: incident.Title,
                    Summary: summary,
                    RelevanceScore: 0.80,
                    IsTruncated: WasTruncated(incident.Description, MaxSnippetLength)));
            }

            _logger.LogDebug(
                "IncidentDB grounding: {Count} incidents in last {Days} days for serviceId='{ServiceId}'",
                incidents.Count, request.IncidentsWindowDays, request.ServiceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "IncidentDB grounding failed for serviceId='{ServiceId}' — continuing",
                request.ServiceId);
        }

        // ── 4. AI model context (for AI-related queries) ──────────────────────────────────────
        try
        {
            var models = await _modelRepository.ListAsync(
                provider: null, modelType: null, status: ModelStatus.Active, isInternal: null, ct: ct);

            var query = request.Query;
            var modelHits = models
                .Where(m =>
                    m.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.Provider.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .Select((m, index) => new DatabaseSearchHit(
                    EntityType: "AIModel",
                    EntityId: m.Id.Value.ToString(),
                    DisplayName: m.DisplayName,
                    Summary: $"AI Model '{m.Name}' from provider '{m.Provider}' — {Truncate(m.Capabilities, MaxSnippetLength)}",
                    RelevanceScore: Math.Max(0.0, 0.70 - (index * 0.1)),
                    IsTruncated: WasTruncated(m.Capabilities, MaxSnippetLength)))
                .ToList();

            hits.AddRange(modelHits);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "AI model grounding failed for query='{Query}' — continuing",
                request.Query);
        }

        _logger.LogDebug(
            "Database grounding retrieval completed: {HitCount} hits for query='{Query}'",
            hits.Count, request.Query);

        return new DatabaseSearchResult(true, hits.Take(request.MaxResults).ToList());
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }

    private static bool WasTruncated(string? text, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(text) && text.Length > maxLength;
    }

    private static Guid? ParseGuid(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out var result))
            return result;
        return null;
    }
}
