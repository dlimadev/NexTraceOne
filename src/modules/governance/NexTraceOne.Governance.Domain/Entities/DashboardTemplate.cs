using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para DashboardTemplate.</summary>
public sealed record DashboardTemplateId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Template de dashboard publicado na galeria interna (V3.8 — Marketplace &amp; Plugin SDK).
/// Pode ser per-tenant ou global (system-owned). Instalar = clonar com resolve de variáveis.
/// </summary>
public sealed class DashboardTemplate : Entity<DashboardTemplateId>
{
    /// <summary>Nome do template.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do template.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Persona alvo do template.</summary>
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Categoria do template (ex: "services", "finops", "compliance", "operations", "teams").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Tags do template (JSON array de strings).</summary>
    public string TagsJson { get; private set; } = "[]";

    /// <summary>Snapshot JSON do dashboard (widgets + variables + layout).</summary>
    public string DashboardSnapshotJson { get; private set; } = "{}";

    /// <summary>Variáveis requeridas para instanciar o template (JSON array de { key, label, type }).</summary>
    public string RequiredVariablesJson { get; private set; } = "[]";

    /// <summary>Número de vezes que o template foi instanciado.</summary>
    public int InstallCount { get; private set; }

    /// <summary>Versão semântica do template.</summary>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>Indica se é um template de sistema (global para todos os tenants).</summary>
    public bool IsSystem { get; private init; }

    /// <summary>Tenant proprietário (null para templates de sistema).</summary>
    public string? TenantId { get; private init; }

    /// <summary>Utilizador criador do template.</summary>
    public string CreatedByUserId { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última modificação.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private DashboardTemplate() { }

    public static DashboardTemplate Create(
        string name,
        string description,
        string persona,
        string category,
        string dashboardSnapshotJson,
        string userId,
        DateTimeOffset now,
        string? tenantId = null,
        bool isSystem = false,
        string version = "1.0.0",
        IReadOnlyList<string>? tags = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(persona, nameof(persona));
        Guard.Against.NullOrWhiteSpace(category, nameof(category));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        return new DashboardTemplate
        {
            Id = new DashboardTemplateId(Guid.NewGuid()),
            Name = name.Trim(),
            Description = description.Trim(),
            Persona = persona.Trim(),
            Category = category.ToLowerInvariant(),
            DashboardSnapshotJson = dashboardSnapshotJson,
            RequiredVariablesJson = "[]",
            TagsJson = tags is { Count: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(tags)
                : "[]",
            Version = version,
            IsSystem = isSystem,
            TenantId = tenantId,
            CreatedByUserId = userId,
            InstallCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void IncrementInstallCount() => InstallCount++;

    public void Update(string name, string description, string category, string version, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Description = description.Trim();
        Category = category.ToLowerInvariant();
        Version = version;
        UpdatedAt = now;
    }
}
