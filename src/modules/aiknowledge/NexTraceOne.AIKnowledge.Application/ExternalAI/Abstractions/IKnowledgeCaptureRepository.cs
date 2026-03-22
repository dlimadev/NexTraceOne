using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;

/// <summary>
/// Repositório de captures de conhecimento extraídos de IA externa.
/// </summary>
public interface IKnowledgeCaptureRepository
{
    /// <summary>Obtém um capture pelo identificador.</summary>
    Task<KnowledgeCapture?> GetByIdAsync(KnowledgeCaptureId id, CancellationToken ct);

    /// <summary>Adiciona e persiste um novo capture.</summary>
    Task AddAsync(KnowledgeCapture capture, CancellationToken ct);

    /// <summary>Actualiza e persiste um capture existente.</summary>
    Task UpdateAsync(KnowledgeCapture capture, CancellationToken ct);

    /// <summary>Lista captures com filtros opcionais e paginação.</summary>
    Task<(IReadOnlyList<KnowledgeCapture> Items, int Total)> ListAsync(
        KnowledgeStatus? status,
        string? category,
        string? tags,
        string? textFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Retorna métricas agregadas de uso de IA externa.</summary>
    Task<ExternalAiUsageMetrics> GetUsageMetricsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct);
}

/// <summary>Métricas agregadas de uso de IA externa.</summary>
public sealed record ExternalAiUsageMetrics(
    int TotalConsultations,
    int CompletedConsultations,
    int FailedConsultations,
    long TotalTokensUsed,
    IReadOnlyList<ProviderUsageMetric> ByProvider,
    int TotalCaptures,
    int ApprovedCaptures,
    int RejectedCaptures,
    int PendingCaptures,
    long TotalReuses);

/// <summary>Métricas de uso por provedor de IA.</summary>
public sealed record ProviderUsageMetric(string ProviderId, int ConsultationCount, long TokensUsed);
