using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Repositório de registros de geração de código do módulo DeveloperPortal.
/// Mantém trilha de auditoria para artefatos gerados a partir de contratos OpenAPI.
/// </summary>
public interface ICodeGenerationRepository
{
    Task<CodeGenerationRecord?> GetByIdAsync(CodeGenerationRecordId id, CancellationToken ct = default);
    Task<IReadOnlyList<CodeGenerationRecord>> GetByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default);
    Task<IReadOnlyList<CodeGenerationRecord>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    void Add(CodeGenerationRecord record);
}
