using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Classe base para todos os DbContexts dos módulos.
/// Configura automaticamente: TenantRlsInterceptor (RLS PostgreSQL),
/// AuditInterceptor (CreatedAt/By, UpdatedAt/By),
/// EncryptionInterceptor (AES-256-GCM), OutboxInterceptor (Domain Events → Outbox).
/// </summary>
public abstract class NexTraceDbContextBase(
    DbContextOptions options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock) : DbContext(options)
{
    /// <summary>Assembly com as configurações IEntityTypeConfiguration deste DbContext.</summary>
    protected abstract System.Reflection.Assembly ConfigurationsAssembly { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(ConfigurationsAssembly);
        // TODO: ApplyUtcDateTimeConvention, ApplyStronglyTypedIdConventions, ApplyGlobalSoftDeleteFilter
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // TODO: SetAuditFields(), CollectDomainEvents(), WriteToOutboxAsync()
        return await base.SaveChangesAsync(ct);
    }
}
