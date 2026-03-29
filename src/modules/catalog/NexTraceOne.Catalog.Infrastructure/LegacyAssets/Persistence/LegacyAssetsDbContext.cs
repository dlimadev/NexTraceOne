using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence;

/// <summary>
/// DbContext do sub-domínio Legacy Assets dentro do módulo Catalog.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class LegacyAssetsDbContext(
    DbContextOptions<LegacyAssetsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, ILegacyAssetsUnitOfWork
{
    /// <summary>Sistemas mainframe persistidos do sub-domínio Legacy Assets.</summary>
    public DbSet<MainframeSystem> MainframeSystems => Set<MainframeSystem>();

    /// <summary>Programas COBOL persistidos do sub-domínio Legacy Assets.</summary>
    public DbSet<CobolProgram> CobolPrograms => Set<CobolProgram>();

    /// <summary>Copybooks COBOL persistidos do sub-domínio Legacy Assets.</summary>
    public DbSet<Copybook> Copybooks => Set<Copybook>();

    /// <summary>Campos de copybooks persistidos do sub-domínio Legacy Assets.</summary>
    public DbSet<CopybookField> CopybookFields => Set<CopybookField>();

    /// <summary>Transações CICS persistidas do sub-domínio Legacy Assets.</summary>
    public DbSet<CicsTransaction> CicsTransactions => Set<CicsTransaction>();

    /// <summary>Transações IMS persistidas do sub-domínio Legacy Assets.</summary>
    public DbSet<ImsTransaction> ImsTransactions => Set<ImsTransaction>();

    /// <summary>Artefactos DB2 persistidos do sub-domínio Legacy Assets.</summary>
    public DbSet<Db2Artifact> Db2Artifacts => Set<Db2Artifact>();

    /// <summary>Bindings z/OS Connect persistidos do sub-domínio Legacy Assets.</summary>
    public DbSet<ZosConnectBinding> ZosConnectBindings => Set<ZosConnectBinding>();

    /// <summary>Relações de uso copybook-programa persistidas do sub-domínio Legacy Assets.</summary>
    public DbSet<CopybookProgramUsage> CopybookProgramUsages => Set<CopybookProgramUsage>();

    /// <summary>Dependências entre ativos legacy persistidas do sub-domínio Legacy Assets.</summary>
    public DbSet<LegacyDependency> LegacyDependencies => Set<LegacyDependency>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(LegacyAssetsDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Catalog.Infrastructure.LegacyAssets";

    /// <inheritdoc />
    protected override string OutboxTableName => "cat_legacy_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
