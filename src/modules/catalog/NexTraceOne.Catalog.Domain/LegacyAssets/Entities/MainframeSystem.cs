using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Sistema mainframe — aggregate root do sub-domínio Legacy Assets.
/// Representa um sistema mainframe completo (LPAR/sysplex/região) com ownership e classificação.
/// </summary>
public sealed class MainframeSystem : Entity<MainframeSystemId>
{
    private MainframeSystem() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome único do sistema (identificador técnico).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do sistema.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do sistema e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Referência LPAR (sysplex, LPAR, região) onde o sistema reside.</summary>
    public LparReference Lpar { get; private set; } = null!;

    // ── Ownership ─────────────────────────────────────────────────────

    /// <summary>Equipa responsável pelo sistema.</summary>
    public string TeamName { get; private set; } = string.Empty;

    /// <summary>Domínio de negócio ao qual o sistema pertence.</summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>Owner técnico do sistema.</summary>
    public string TechnicalOwner { get; private set; } = string.Empty;

    /// <summary>Owner de negócio do sistema.</summary>
    public string BusinessOwner { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do sistema para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do sistema.</summary>
    public LifecycleStatus LifecycleStatus { get; private set; } = LifecycleStatus.Active;

    // ── Metadata ──────────────────────────────────────────────────────

    /// <summary>Sistema operativo do mainframe (z/OS, z/VSE, etc.).</summary>
    public string OperatingSystem { get; private set; } = string.Empty;

    /// <summary>Capacidade MIPS do sistema.</summary>
    public string MipsCapacity { get; private set; } = string.Empty;

    // ── Auditoria ─────────────────────────────────────────────────────

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo sistema mainframe com os campos obrigatórios.</summary>
    public static MainframeSystem Create(
        string name,
        string domain,
        string teamName,
        LparReference lpar)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(domain);
        Guard.Against.NullOrWhiteSpace(teamName);
        Guard.Against.Null(lpar);

        return new MainframeSystem
        {
            Id = MainframeSystemId.New(),
            Name = name.Trim(),
            DisplayName = name.Trim(),
            Domain = domain.Trim(),
            TeamName = teamName.Trim(),
            Lpar = lpar,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação do sistema.</summary>
    public void UpdateDetails(
        string displayName,
        string description,
        Criticality criticality,
        LifecycleStatus lifecycleStatus,
        string operatingSystem,
        string mipsCapacity)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        OperatingSystem = operatingSystem ?? string.Empty;
        MipsCapacity = mipsCapacity ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Atualiza o ownership do sistema.</summary>
    public void UpdateOwnership(string teamName, string technicalOwner, string businessOwner)
    {
        TeamName = Guard.Against.NullOrWhiteSpace(teamName);
        TechnicalOwner = technicalOwner ?? string.Empty;
        BusinessOwner = businessOwner ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Atualiza a referência LPAR do sistema.</summary>
    public void UpdateLpar(LparReference lpar)
    {
        Guard.Against.Null(lpar);
        Lpar = lpar;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de MainframeSystem.</summary>
public sealed record MainframeSystemId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static MainframeSystemId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static MainframeSystemId From(Guid id) => new(id);
}
