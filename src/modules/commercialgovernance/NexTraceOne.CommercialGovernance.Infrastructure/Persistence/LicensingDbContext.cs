using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.CommercialCatalog.Domain.Entities;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Licensing.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class LicensingDbContext(
    DbContextOptions<LicensingDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Licenças persistidas do módulo Licensing.</summary>
    public DbSet<License> Licenses => Set<License>();

    /// <summary>Capabilities persistidas do módulo Licensing.</summary>
    public DbSet<LicenseCapability> LicenseCapabilities => Set<LicenseCapability>();

    /// <summary>Ativações persistidas do módulo Licensing.</summary>
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();

    /// <summary>Quotas persistidas do módulo Licensing.</summary>
    public DbSet<UsageQuota> UsageQuotas => Set<UsageQuota>();

    /// <summary>Bindings de hardware persistidos do módulo Licensing.</summary>
    public DbSet<HardwareBinding> HardwareBindings => Set<HardwareBinding>();

    /// <summary>Registros de consentimento de telemetria por licença.</summary>
    public DbSet<TelemetryConsent> TelemetryConsents => Set<TelemetryConsent>();

    /// <summary>Planos comerciais do subdomínio CommercialCatalog.</summary>
    public DbSet<Plan> Plans => Set<Plan>();

    /// <summary>Pacotes de funcionalidades do subdomínio CommercialCatalog.</summary>
    public DbSet<FeaturePack> FeaturePacks => Set<FeaturePack>();

    /// <summary>Itens de pacotes de funcionalidades do subdomínio CommercialCatalog.</summary>
    public DbSet<FeaturePackItem> FeaturePackItems => Set<FeaturePackItem>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(LicensingDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
