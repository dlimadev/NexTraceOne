using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using System.Linq.Expressions;

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
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EventType).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.LastError).HasMaxLength(4000);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.ProcessedAt);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(ConfigurationsAssembly);
        ApplyGlobalSoftDeleteFilter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        WriteDomainEventsToOutbox();
        return await base.SaveChangesAsync(ct);
    }

    private void WriteDomainEventsToOutbox()
    {
        var aggregateEntries = ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .Where(entity => entity is not null && IsAggregateRoot(entity.GetType()))
            .ToList();

        foreach (var entity in aggregateEntries)
        {
            var domainEvents = GetDomainEvents(entity!);
            if (domainEvents.Count == 0)
            {
                continue;
            }

            foreach (var domainEvent in domainEvents)
            {
                Set<OutboxMessage>().Add(OutboxMessage.Create(domainEvent, tenant.Id, clock.UtcNow));
            }

            ClearDomainEvents(entity!);
        }
    }

    private static IReadOnlyList<object> GetDomainEvents(object entity)
        => (entity.GetType().GetProperty("DomainEvents")?.GetValue(entity) as System.Collections.IEnumerable)
            ?.Cast<object>()
            .ToArray()
           ?? [];

    private static void ClearDomainEvents(object entity)
        => entity.GetType().GetMethod("ClearDomainEvents")?.Invoke(entity, []);

    private static bool IsAggregateRoot(Type type)
        => IsAssignableToGenericType(type, typeof(AggregateRoot<>));

    private static bool IsAssignableToGenericType(Type type, Type genericType)
    {
        while (type != typeof(object) && type is not null)
        {
            var current = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (current == genericType)
            {
                return true;
            }

            type = type.BaseType!;
        }

        return false;
    }

    private static void ApplyGlobalSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!IsAssignableToGenericType(entityType.ClrType, typeof(AuditableEntity<>)))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var property = Expression.Property(parameter, "IsDeleted");
            var compare = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(compare, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
