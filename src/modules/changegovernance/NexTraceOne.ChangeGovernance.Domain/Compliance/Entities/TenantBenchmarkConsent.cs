using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

/// <summary>
/// Entidade que regista o consentimento de um tenant para participar em benchmarks
/// cross-tenant anonimizados. Segue a base legal LGPD/GDPR.
/// Opt-in explícito — sem consentimento não há partilha de dados.
/// </summary>
public sealed class TenantBenchmarkConsent : AuditableEntity<TenantBenchmarkConsentId>
{
    private TenantBenchmarkConsent() { }

    /// <summary>Identificador do tenant que presta (ou revoga) o consentimento.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Estado atual do consentimento.</summary>
    public BenchmarkConsentStatus Status { get; private set; }

    /// <summary>Data/hora em que o consentimento foi concedido.</summary>
    public DateTimeOffset? ConsentedAt { get; private set; }

    /// <summary>Data/hora em que o consentimento foi revogado.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Id do utilizador que executou a última ação de consentimento (auditoria).</summary>
    public string? ConsentedByUserId { get; private set; }

    /// <summary>Base legal LGPD/GDPR invocada para este consentimento.</summary>
    public string LgpdLawfulBasis { get; private set; } = string.Empty;

    /// <summary>Indica se o tenant está activamente participante nos benchmarks.</summary>
    public bool IsOptedIn => Status == BenchmarkConsentStatus.Granted;

    /// <summary>
    /// Cria um novo registo de consentimento no estado Pending.
    /// Chamado quando o tenant manifesta intenção de participar nos benchmarks.
    /// </summary>
    public static TenantBenchmarkConsent RequestConsent(string tenantId, string lgpdLawfulBasis)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(lgpdLawfulBasis);

        return new TenantBenchmarkConsent
        {
            Id = TenantBenchmarkConsentId.New(),
            TenantId = tenantId,
            Status = BenchmarkConsentStatus.Pending,
            LgpdLawfulBasis = lgpdLawfulBasis
        };
    }

    /// <summary>Concede o consentimento — regista utilizador e timestamp.</summary>
    public void Grant(string? userId, DateTimeOffset now)
    {
        Status = BenchmarkConsentStatus.Granted;
        ConsentedAt = now;
        ConsentedByUserId = userId;
        RevokedAt = null;
    }

    /// <summary>Revoga o consentimento — regista utilizador e timestamp.</summary>
    public void Revoke(string? userId, DateTimeOffset now)
    {
        Status = BenchmarkConsentStatus.Revoked;
        RevokedAt = now;
        ConsentedByUserId = userId;
    }
}

/// <summary>Identificador fortemente tipado de TenantBenchmarkConsent.</summary>
public sealed record TenantBenchmarkConsentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static TenantBenchmarkConsentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static TenantBenchmarkConsentId From(Guid id) => new(id);
}
