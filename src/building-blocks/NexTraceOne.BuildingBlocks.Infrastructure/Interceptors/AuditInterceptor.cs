using Microsoft.EntityFrameworkCore.Diagnostics;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

/// <summary>
/// Interceptor que preenche CreatedAt/By e UpdatedAt/By automaticamente
/// em todas as AuditableEntity antes do SaveChanges.
/// </summary>
public sealed class AuditInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var actor = currentUser.IsAuthenticated ? currentUser.Id : "system";
        var now = dateTimeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not object entity || !IsAuditableEntity(entity.GetType()))
            {
                continue;
            }

            dynamic auditableEntity = entity;

            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            {
                auditableEntity.SetCreated(now, actor);
                auditableEntity.SetUpdated(now, actor);
            }
            else if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
            {
                auditableEntity.SetUpdated(now, actor);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool IsAuditableEntity(Type type)
        => IsAssignableToGenericType(type, typeof(AuditableEntity<>));

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
}
