using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Repositório da cadeia de hash de auditoria.
/// </summary>
public interface IAuditChainRepository
{
    /// <summary>Obtém o último link da cadeia de hash.</summary>
    Task<AuditChainLink?> GetLatestLinkAsync(CancellationToken cancellationToken);

    /// <summary>Obtém todos os links da cadeia de hash em ordem sequencial.</summary>
    Task<IReadOnlyList<AuditChainLink>> GetAllLinksAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona um novo link à cadeia.</summary>
    void Add(AuditChainLink chainLink);
}
