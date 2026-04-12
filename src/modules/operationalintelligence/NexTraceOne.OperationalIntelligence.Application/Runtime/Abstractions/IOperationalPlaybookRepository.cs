using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>Contrato do repositório para playbooks operacionais.</summary>
public interface IOperationalPlaybookRepository
{
    /// <summary>Obtém um playbook pelo identificador.</summary>
    Task<OperationalPlaybook?> GetByIdAsync(OperationalPlaybookId id, CancellationToken cancellationToken);

    /// <summary>Lista playbooks do tenant.</summary>
    Task<IReadOnlyList<OperationalPlaybook>> ListAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>Lista playbooks filtrados por status.</summary>
    Task<IReadOnlyList<OperationalPlaybook>> ListByStatusAsync(string tenantId, PlaybookStatus status, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo playbook.</summary>
    Task AddAsync(OperationalPlaybook playbook, CancellationToken cancellationToken);
}
