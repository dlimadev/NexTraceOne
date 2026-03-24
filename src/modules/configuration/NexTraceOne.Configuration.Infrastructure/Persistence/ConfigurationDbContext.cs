using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Configuration.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ConfigurationDbContext(
    DbContextOptions<ConfigurationDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Definições de configuração (metadados e schema).</summary>
    public DbSet<ConfigurationDefinition> Definitions => Set<ConfigurationDefinition>();

    /// <summary>Valores concretos de configuração por âmbito.</summary>
    public DbSet<ConfigurationEntry> Entries => Set<ConfigurationEntry>();

    /// <summary>Registos de auditoria de alterações em configurações.</summary>
    public DbSet<ConfigurationAuditEntry> AuditEntries => Set<ConfigurationAuditEntry>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ConfigurationDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Configuration.Infrastructure";

    /// <inheritdoc />
    protected override string OutboxTableName => "cfg_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
