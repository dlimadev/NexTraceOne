using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Attributes;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.BuildingBlocks.Infrastructure.Converters;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using System.Linq.Expressions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Classe base para todos os DbContexts dos módulos.
/// Configura automaticamente: TenantRlsInterceptor (RLS PostgreSQL),
/// AuditInterceptor (CreatedAt/By, UpdatedAt/By),
/// EncryptedFieldConvention (AES-256-GCM via EncryptedStringConverter para propriedades
/// marcadas com [EncryptedField]), OutboxMessage (Domain Events → Outbox).
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

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
        ApplyTypedIdConventions(modelBuilder);
        ApplyValueObjectConventions(modelBuilder);

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

    /// <summary>
    /// Aplica automaticamente ValueConverters para todas as propriedades do tipo ITypedId
    /// e define HasKey para entidades que herdam de Entity&lt;TId&gt;.
    /// </summary>
    private static void ApplyTypedIdConventions(ModelBuilder modelBuilder)
    {
        var typedIdTypes = new HashSet<Type>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            // Find if type inherits from Entity<TId> where TId : ITypedId
            var currentType = clrType;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Entity<>))
                {
                    var idType = currentType.GetGenericArguments()[0];
                    if (typeof(ITypedId).IsAssignableFrom(idType))
                    {
                        var entityBuilder = modelBuilder.Entity(clrType);
                        entityBuilder.HasKey("Id");

                        var idProperty = entityBuilder.Property("Id").Metadata;
                        var converterType = typeof(TypedIdValueConverter<>).MakeGenericType(idType);
                        var converter = (ValueConverter?)Activator.CreateInstance(converterType);
                        if (converter != null)
                        {
                            idProperty.SetValueConverter(converter);
                        }
                    }
                    break;
                }
                currentType = currentType.BaseType;
            }

            // Apply converters to all ITypedId properties and collect the types
            foreach (var property in entityType.GetProperties().ToList())
            {
                if (typeof(ITypedId).IsAssignableFrom(property.ClrType) && property.ClrType != typeof(ITypedId))
                {
                    typedIdTypes.Add(property.ClrType);

                    var converterType = typeof(TypedIdValueConverter<>).MakeGenericType(property.ClrType);
                    var converter = (ValueConverter?)Activator.CreateInstance(converterType);
                    if (converter != null)
                    {
                        property.SetValueConverter(converter);
                    }
                }
            }
        }

        // Ensure ITypedId value types are NOT mapped as separate entity types
        foreach (var typedIdType in typedIdTypes)
        {
            // Only ignore if it's not already configured as an entity with a key
            var entityType = modelBuilder.Model.FindEntityType(typedIdType);
            if (entityType is not null && entityType.FindPrimaryKey() is null)
            {
                modelBuilder.Ignore(typedIdType);
            }
        }
    }

    /// <summary>
    /// Ignora automaticamente no modelo EF Core todos os tipos descobertos implicitamente
    /// que não herdam de Entity&lt;&gt; e não têm chave primária configurada.
    /// Isso evita erros de validação para value objects e records usados como propriedades
    /// em entidades — esses tipos devem ser mapeados via ValueConverter nas configurações
    /// das entidades que os contêm, e não como entidades separadas.
    /// </summary>
    private static void ApplyValueObjectConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            // Skip OutboxMessage (it has explicit configuration)
            if (clrType == typeof(OutboxMessage))
                continue;

            // Skip types that already have a primary key
            if (entityType.FindPrimaryKey() is not null)
                continue;

            // Skip types that inherit from Entity<> (these should have a key)
            if (IsAssignableToGenericType(clrType, typeof(Entity<>)))
                continue;

            // Ignore the type so EF Core does not try to map it as a separate entity
            modelBuilder.Ignore(clrType);
        }
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
