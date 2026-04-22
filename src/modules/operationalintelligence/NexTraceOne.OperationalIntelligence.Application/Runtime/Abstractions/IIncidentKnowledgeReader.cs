namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstração de leitura de conhecimento operacional por tipo de incidente.
///
/// Fornece dados agregados de ocorrências de incidentes, cobertura de runbooks e
/// métricas de efectividade de resolução por tipo de incidente num tenant.
/// Desacopla o handler de base de conhecimento das implementações concretas de repositório.
///
/// Wave AB.3 — GetIncidentKnowledgeBaseReport.
/// </summary>
public interface IIncidentKnowledgeReader
{
    /// <summary>
    /// Lista entradas de conhecimento operacional por tipo de incidente.
    /// Cada entrada agrega ocorrências e métricas de runbook no período de análise.
    /// </summary>
    Task<IReadOnlyList<IncidentTypeKnowledgeEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct);
}

/// <summary>
/// Entrada de conhecimento operacional por tipo de incidente.
/// Agrega métricas de frequência, cobertura de runbook e efectividade de resolução.
/// Wave AB.3.
/// </summary>
public sealed record IncidentTypeKnowledgeEntry(
    /// <summary>Tipo de incidente (categoria ou tag).</summary>
    string IncidentType,
    /// <summary>Total de ocorrências no período de análise.</summary>
    int TotalOccurrences,
    /// <summary>Ocorrências em que existia runbook aprovado disponível.</summary>
    int OccurrencesWithApprovedRunbook,
    /// <summary>Tempo médio entre abertura do incidente e aplicação de runbook (minutos).</summary>
    double AvgTimeToRunbookMinutes,
    /// <summary>Resoluções via runbook sem reabertura posterior do incidente.</summary>
    int RunbookEffectiveResolutions,
    /// <summary>Indica se existe pelo menos um runbook aprovado para este tipo.</summary>
    bool HasApprovedRunbook,
    /// <summary>Indica se o runbook associado não foi revisto dentro do prazo configurado.</summary>
    bool IsRunbookStale,
    /// <summary>Data da última ocorrência registada, ou null se não disponível.</summary>
    DateTimeOffset? LastOccurredAt,
    /// <summary>Indica se a tendência de ocorrências é crescente na segunda metade do período.</summary>
    bool IsTrendIncreasing);
