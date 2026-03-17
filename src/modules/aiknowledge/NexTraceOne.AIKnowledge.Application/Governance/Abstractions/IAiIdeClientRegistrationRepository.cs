using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório para gestão de registos de clientes IDE.
/// </summary>
public interface IAiIdeClientRegistrationRepository
{
    /// <summary>Obtém registo por identificador.</summary>
    Task<AIIDEClientRegistration?> GetByIdAsync(AIIDEClientRegistrationId id, CancellationToken cancellationToken);

    /// <summary>Lista registos filtrados por utilizador e/ou tipo de cliente.</summary>
    Task<IReadOnlyList<AIIDEClientRegistration>> ListAsync(
        string? userId,
        AIClientType? clientType,
        bool? isActive,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Adiciona novo registo.</summary>
    Task AddAsync(AIIDEClientRegistration registration, CancellationToken cancellationToken);

    /// <summary>Atualiza registo existente.</summary>
    Task UpdateAsync(AIIDEClientRegistration registration, CancellationToken cancellationToken);
}
