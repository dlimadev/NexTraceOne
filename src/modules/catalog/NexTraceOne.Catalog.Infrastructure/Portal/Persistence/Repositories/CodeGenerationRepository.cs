using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;

/// <summary>
/// Repositório de registos de geração de código, implementando persistência via EF Core.
/// Suporta consultas por API e por utilizador para trilha de auditoria de artefactos gerados.
/// </summary>
internal sealed class CodeGenerationRepository(DeveloperPortalDbContext context) : ICodeGenerationRepository
{
    /// <summary>Busca registo de geração por identificador único.</summary>
    public async Task<CodeGenerationRecord?> GetByIdAsync(CodeGenerationRecordId id, CancellationToken ct = default)
        => await context.CodeGenerationRecords.SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Lista todas as gerações de uma API.</summary>
    public async Task<IReadOnlyList<CodeGenerationRecord>> GetByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.CodeGenerationRecords
            .Where(r => r.ApiAssetId == apiAssetId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

    /// <summary>Lista gerações de um utilizador com paginação.</summary>
    public async Task<IReadOnlyList<CodeGenerationRecord>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await context.CodeGenerationRecords
            .Where(r => r.RequestedById == userId)
            .OrderByDescending(r => r.GeneratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <summary>Adiciona novo registo de geração ao contexto.</summary>
    public void Add(CodeGenerationRecord record)
        => context.CodeGenerationRecords.Add(record);
}
