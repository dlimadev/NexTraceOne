using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;

/// <summary>
/// Repositório de consultas enviadas a provedores externos de IA.
/// </summary>
public interface IExternalAiConsultationRepository
{
    /// <summary>Adiciona e persiste uma nova consulta externa.</summary>
    Task AddAsync(ExternalAiConsultation consultation, CancellationToken ct);
}
