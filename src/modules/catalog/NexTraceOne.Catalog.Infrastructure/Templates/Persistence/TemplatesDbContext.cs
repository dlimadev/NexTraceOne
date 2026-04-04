using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Templates.Persistence;

/// <summary>
/// DbContext do módulo Templates (Service Templates &amp; Scaffolding — Phase 3.1).
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class TemplatesDbContext(
    DbContextOptions<TemplatesDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, ITemplatesUnitOfWork
{
    /// <summary>Templates de serviço governados disponíveis para scaffolding.</summary>
    public DbSet<ServiceTemplate> ServiceTemplates => Set<ServiceTemplate>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(TemplatesDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Catalog.Infrastructure.Templates.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "tpl_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
