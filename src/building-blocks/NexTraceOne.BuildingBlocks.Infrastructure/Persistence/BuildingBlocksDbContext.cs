using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// DbContext central de BuildingBlocks — aloja tabelas transversais a todos os módulos.
/// Actualmente: bb_dead_letter_messages (DLQ do Outbox Pattern).
/// Não herda de NexTraceDbContextBase para evitar dependência circular de interceptors.
/// </summary>
public sealed class BuildingBlocksDbContext(DbContextOptions<BuildingBlocksDbContext> options)
    : DbContext(options)
{
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BuildingBlocksDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
