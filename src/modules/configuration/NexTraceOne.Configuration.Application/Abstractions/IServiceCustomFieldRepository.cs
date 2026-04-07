using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de campos personalizados de serviços.</summary>
public interface IServiceCustomFieldRepository
{
    Task<ServiceCustomField?> GetByIdAsync(ServiceCustomFieldId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceCustomField>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken);
    Task AddAsync(ServiceCustomField field, CancellationToken cancellationToken);
    Task UpdateAsync(ServiceCustomField field, CancellationToken cancellationToken);
    Task DeleteAsync(ServiceCustomFieldId id, CancellationToken cancellationToken);
}
