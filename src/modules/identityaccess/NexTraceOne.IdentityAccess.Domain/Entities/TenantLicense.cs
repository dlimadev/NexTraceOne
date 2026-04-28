using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Licença SaaS de um tenant — define o plano, host units e validade.
/// Prefixo de tabela: iam_
/// </summary>
public sealed class TenantLicense : Entity<TenantLicenseId>
{
    private TenantLicense() { }

    public Guid TenantId { get; private set; }
    public TenantPlan Plan { get; private set; }
    public TenantLicenseStatus Status { get; private set; }

    /// <summary>Host Units incluídos no plano (para billing).</summary>
    public int IncludedHostUnits { get; private set; }

    /// <summary>Host Units em uso actualmente (recalculado pelo LicenseRecalculationJob).</summary>
    public decimal CurrentHostUnits { get; private set; }

    /// <summary>Host Units além do plano incluído (overage).</summary>
    public decimal OverageHostUnits => Math.Max(0m, CurrentHostUnits - IncludedHostUnits);

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public DateTimeOffset BillingCycleStart { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static TenantLicense Provision(
        Guid tenantId,
        TenantPlan plan,
        int includedHostUnits,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        DateTimeOffset now)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Negative(includedHostUnits);

        var status = plan == TenantPlan.Trial ? TenantLicenseStatus.Trial : TenantLicenseStatus.Active;

        return new TenantLicense
        {
            Id = TenantLicenseId.New(),
            TenantId = tenantId,
            Plan = plan,
            Status = status,
            IncludedHostUnits = includedHostUnits,
            CurrentHostUnits = 0m,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            BillingCycleStart = validFrom,
            CreatedAt = now,
        };
    }

    public void UpdateHostUnits(decimal currentHostUnits, DateTimeOffset now)
    {
        CurrentHostUnits = Math.Round(currentHostUnits, 1);
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = TenantLicenseStatus.Suspended;
        UpdatedAt = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        Status = TenantLicenseStatus.Active;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        Status = TenantLicenseStatus.Expired;
        UpdatedAt = now;
    }

    public void Upgrade(TenantPlan newPlan, int newIncludedHostUnits, DateTimeOffset? newValidUntil, DateTimeOffset now)
    {
        Plan = newPlan;
        IncludedHostUnits = newIncludedHostUnits;
        ValidUntil = newValidUntil;
        Status = TenantLicenseStatus.Active;
        UpdatedAt = now;
    }

    /// <summary>Retorna as capabilities do plano actual.</summary>
    public IReadOnlyList<string> GetCapabilities() => TenantCapabilities.ForPlan(Plan);
}
