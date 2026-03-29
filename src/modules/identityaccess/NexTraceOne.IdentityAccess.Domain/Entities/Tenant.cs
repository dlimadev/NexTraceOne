using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um Tenant (organização/cliente/conta).
///
/// O Tenant é a unidade raiz de isolamento multi-tenant do NexTraceOne.
/// Cada tenant possui um slug único para identificação amigável,
/// um nome de exibição e um flag de ativação.
///
/// Suporta hierarquia organizacional (Holding → Subsidiária → Departamento)
/// para cenários enterprise e SaaS:
/// - SaaS: cada cliente é um tenant raiz; subsidiárias são child tenants.
/// - Self-hosted: matriz como raiz, filiais como subsidiárias.
/// - Híbrido: flexível conforme deployment.
///
/// Regras de negócio:
/// - Slug deve ser único no sistema.
/// - Tenant inativo impede login e operações.
/// - Operações de modificação geram eventos de auditoria.
/// - Tenant nunca é excluído fisicamente — apenas desativado.
/// - Hierarquia máxima de 3 níveis (Holding → Subsidiary → Department).
/// - ParentTenantId nulo indica tenant raiz (backward-compatible).
/// </summary>
public sealed class Tenant : AggregateRoot<TenantId>
{
    /// <summary>Profundidade máxima permitida na hierarquia de tenants.</summary>
    public const int MaxHierarchyDepth = 3;

    private Tenant() { }

    /// <summary>Nome de exibição do tenant (ex.: "Banco XYZ").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Slug único do tenant (ex.: "banco-xyz"). Usado em URLs e seleção.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Indica se o tenant está ativo e pode realizar operações.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação do tenant.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC da última atualização do tenant.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Hierarquia organizacional ─────────────────────────────────────────

    /// <summary>
    /// Identificador do tenant pai na hierarquia organizacional.
    /// Nulo indica tenant raiz (organização/holding independente).
    /// Compatível com modelo anterior — tenants existentes sem parent continuam a funcionar.
    /// </summary>
    public TenantId? ParentTenantId { get; private set; }

    /// <summary>
    /// Tipo organizacional do tenant na hierarquia.
    /// Default: Organization (backward-compatible).
    /// </summary>
    public TenantType TenantType { get; private set; } = TenantType.Organization;

    /// <summary>
    /// Razão social / nome legal da organização. Relevante para compliance e billing.
    /// </summary>
    public string? LegalName { get; private set; }

    /// <summary>
    /// Identificação fiscal (CNPJ/Tax ID). Relevante para compliance, billing e auditoria.
    /// </summary>
    public string? TaxId { get; private set; }

    /// <summary>Concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Factory method para criação de um novo tenant raiz.
    /// Garante que nome e slug são informados e gera o Id automaticamente.
    /// Backward-compatible: cria um tenant Organization sem parent.
    /// </summary>
    /// <param name="name">Nome de exibição do tenant.</param>
    /// <param name="slug">Slug único do tenant (será usado em URLs).</param>
    /// <param name="now">Data/hora UTC atual.</param>
    /// <returns>Nova instância de Tenant ativa.</returns>
    public static Tenant Create(string name, string slug, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(name, message: "Tenant name is required.");
        Guard.Against.NullOrWhiteSpace(slug, message: "Tenant slug is required.");

        return new Tenant
        {
            Id = TenantId.New(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            IsActive = true,
            CreatedAt = now,
            TenantType = TenantType.Organization
        };
    }

    /// <summary>
    /// Factory method para criação de um tenant com tipo organizacional e hierarquia.
    /// Usado para cenários enterprise (Holding/Subsidiary/Department) e SaaS (Partner).
    /// </summary>
    /// <param name="name">Nome de exibição do tenant.</param>
    /// <param name="slug">Slug único do tenant.</param>
    /// <param name="tenantType">Tipo organizacional.</param>
    /// <param name="now">Data/hora UTC atual.</param>
    /// <param name="parentTenantId">Tenant pai na hierarquia, nulo para tenant raiz.</param>
    /// <param name="legalName">Razão social, opcional.</param>
    /// <param name="taxId">Identificação fiscal, opcional.</param>
    /// <returns>Nova instância de Tenant ativa com hierarquia.</returns>
    public static Tenant CreateWithHierarchy(
        string name,
        string slug,
        TenantType tenantType,
        DateTimeOffset now,
        TenantId? parentTenantId = null,
        string? legalName = null,
        string? taxId = null)
    {
        Guard.Against.NullOrWhiteSpace(name, message: "Tenant name is required.");
        Guard.Against.NullOrWhiteSpace(slug, message: "Tenant slug is required.");

        // Validação: Holdings e Organizations não devem ter parent (são raiz).
        if (tenantType is TenantType.Organization or TenantType.Holding && parentTenantId is not null)
            throw new ArgumentException(
                $"Tenant of type {tenantType} must be a root tenant (no parent).");

        // Validação: Subsidiárias e Departamentos requerem parent.
        if (tenantType is TenantType.Subsidiary or TenantType.Department && parentTenantId is null)
            throw new ArgumentException(
                $"Tenant of type {tenantType} requires a parent tenant.");

        return new Tenant
        {
            Id = TenantId.New(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            IsActive = true,
            CreatedAt = now,
            TenantType = tenantType,
            ParentTenantId = parentTenantId,
            LegalName = legalName,
            TaxId = taxId
        };
    }

    /// <summary>Atualiza o nome de exibição do tenant.</summary>
    public void UpdateName(string name, DateTimeOffset now)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, message: "Tenant name is required.");
        UpdatedAt = now;
    }

    /// <summary>Atualiza dados organizacionais do tenant.</summary>
    public void UpdateOrganizationInfo(string? legalName, string? taxId, DateTimeOffset now)
    {
        LegalName = legalName;
        TaxId = taxId;
        UpdatedAt = now;
    }

    /// <summary>Desativa o tenant, impedindo novas operações e logins.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>Reativa um tenant previamente desativado.</summary>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>Indica se o tenant é raiz na hierarquia (sem parent).</summary>
    public bool IsRoot => ParentTenantId is null;
}
