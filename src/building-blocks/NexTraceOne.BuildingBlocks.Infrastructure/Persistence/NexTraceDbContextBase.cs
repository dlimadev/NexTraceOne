using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Attributes;
using NexTraceOne.BuildingBlocks.Infrastructure.Converters;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using System.Linq.Expressions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Classe base para todos os DbContexts dos módulos.
/// Configura automaticamente: TenantRlsInterceptor (RLS PostgreSQL),
/// AuditInterceptor (CreatedAt/By, UpdatedAt/By),
/// EncryptionInterceptor (AES-256-GCM via EncryptedStringConverter para propriedades
/// marcadas com [EncryptedField]), OutboxInterceptor (Domain Events → Outbox).
/// </summary>
public abstract class NexTraceDbContextBase(
    DbContextOptions options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock) : DbContext(options)
{
    /// <summary>Assembly com as configurações IEntityTypeConfiguration deste DbContext.</summary>
    protected abstract System.Reflection.Assembly ConfigurationsAssembly { get; }

    /// <summary>
    /// Prefixo de namespace para filtrar configurações ao carregar de um assembly partilhado.
    /// Quando múltiplos DbContexts partilham o mesmo assembly, cada um deve sobrescrever
    /// esta propriedade para carregar apenas as suas configurações.
    /// Se null, todas as configurações do assembly são carregadas.
    /// </summary>
    protected virtual string? ConfigurationsNamespace => null;

    /// <summary>
    /// Nome da tabela de outbox para este DbContext.
    /// Cada módulo deve sobrescrever com um nome prefixado para evitar colisões entre
    /// DbContexts que partilham o mesmo schema PostgreSQL.
    /// Exemplo: "wf_outbox_messages" para WorkflowDbContext.
    /// </summary>
    protected virtual string OutboxTableName => "outbox_messages";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable(OutboxTableName);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EventType).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.LastError).HasMaxLength(4000);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(500).IsRequired();
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.ProcessedAt);
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        var ns = ConfigurationsNamespace;
        if (ns is not null)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                ConfigurationsAssembly,
                type => type.Namespace is not null && type.Namespace.StartsWith(ns, StringComparison.Ordinal));
        }
        else
        {
            modelBuilder.ApplyConfigurationsFromAssembly(ConfigurationsAssembly);
        }

        ApplyGlobalSoftDeleteFilter(modelBuilder);
        ApplyEncryptedFieldConvention(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        WriteDomainEventsToOutbox();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entityType = ex.Entries.Count > 0 ? ex.Entries[0].Entity.GetType().Name : "Unknown";
            throw new NexTraceOne.BuildingBlocks.Application.Abstractions.ConcurrencyException(entityType, ex);
        }
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
            if (domainEvents.Length == 0)
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

    private static object[] GetDomainEvents(object entity)
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

    /// <summary>
    /// Aplica automaticamente o EncryptedStringConverter a todas as propriedades string
    /// marcadas com [EncryptedField], garantindo encriptação at-rest transparente via AES-256-GCM.
    /// </summary>
    private static void ApplyEncryptedFieldConvention(ModelBuilder modelBuilder)
    {
        var converter = new EncryptedStringConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            foreach (var propertyInfo in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.PropertyType != typeof(string))
                {
                    continue;
                }

                var encryptedAttr = propertyInfo.GetCustomAttribute<EncryptedFieldAttribute>();
                if (encryptedAttr is null)
                {
                    continue;
                }

                var efProperty = entityType.FindProperty(propertyInfo.Name);
                efProperty?.SetValueConverter(converter);
            }
        }
    }
}
