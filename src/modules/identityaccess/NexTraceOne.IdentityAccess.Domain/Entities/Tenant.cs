using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um Tenant (organização/cliente/conta).
///
/// O Tenant é a unidade raiz de isolamento multi-tenant do NexTraceOne.
/// Cada tenant possui um slug único para identificação amigável,
/// um nome de exibição e um flag de ativação.
///
/// Regras de negócio:
/// - Slug deve ser único no sistema.
/// - Tenant inativo impede login e operações.
/// - Operações de modificação geram eventos de auditoria.
/// - Tenant nunca é excluído fisicamente — apenas desativado.
/// </summary>
public sealed class Tenant : AggregateRoot<TenantId>
{
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

    /// <summary>
    /// Factory method para criação de um novo tenant.
    /// Garante que nome e slug são informados e gera o Id automaticamente.
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
            CreatedAt = now
        };
    }

    /// <summary>Atualiza o nome de exibição do tenant.</summary>
    public void UpdateName(string name, DateTimeOffset now)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, message: "Tenant name is required.");
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
}
