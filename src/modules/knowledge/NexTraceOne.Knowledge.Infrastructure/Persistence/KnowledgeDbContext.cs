using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Knowledge.
/// Contém as entidades centrais do Knowledge Hub:
///   - KnowledgeDocument (documentos de conhecimento operacional e técnico)
///   - OperationalNote (notas operacionais persistidas)
///   - KnowledgeRelation (relações entre objectos de conhecimento e outros contextos)
///
/// Prefixo de tabelas: knw_
///
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class KnowledgeDbContext(
    DbContextOptions<KnowledgeDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Documentos de conhecimento operacional e técnico.</summary>
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    /// <summary>Notas operacionais persistidas.</summary>
    public DbSet<OperationalNote> OperationalNotes => Set<OperationalNote>();

    /// <summary>Relações entre objectos de conhecimento e outros contextos do sistema.</summary>
    public DbSet<KnowledgeRelation> KnowledgeRelations => Set<KnowledgeRelation>();

    /// <summary>Snapshots do knowledge graph operacional.</summary>
    public DbSet<KnowledgeGraphSnapshot> KnowledgeGraphSnapshots => Set<KnowledgeGraphSnapshot>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(KnowledgeDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "knw_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
