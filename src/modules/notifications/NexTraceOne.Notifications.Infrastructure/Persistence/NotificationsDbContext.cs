using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Notifications.
/// REGRA: Outros módulos NUNCA referenciam este DbContext.
/// </summary>
public sealed class NotificationsDbContext(
    DbContextOptions<NotificationsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Notificações da central interna.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Registos de entrega externa (email, Teams).</summary>
    public DbSet<NotificationDelivery> Deliveries => Set<NotificationDelivery>();

    /// <summary>Preferências de notificação dos utilizadores.</summary>
    public DbSet<NotificationPreference> Preferences => Set<NotificationPreference>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(NotificationsDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "ntf_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
