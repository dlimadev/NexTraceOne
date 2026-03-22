using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Repositório de políticas de compliance do módulo Audit.
/// </summary>
public interface ICompliancePolicyRepository
{
    /// <summary>Obtém uma política pelo identificador.</summary>
    Task<CompliancePolicy?> GetByIdAsync(CompliancePolicyId id, CancellationToken cancellationToken);

    /// <summary>Lista políticas com filtros opcionais.</summary>
    Task<IReadOnlyList<CompliancePolicy>> ListAsync(bool? isActive, string? category, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova política.</summary>
    void Add(CompliancePolicy policy);

    /// <summary>Atualiza uma política existente.</summary>
    void Update(CompliancePolicy policy);
}
