using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa um ambiente dentro de um tenant (ex.: Development, Pre-Production, Production).
///
/// Ambientes são a dimensão de autorização que permite atribuir níveis de acesso
/// diferentes por estágio do ciclo de vida. Um usuário pode ter permissão de escrita
/// em Development mas apenas leitura em Production, por exemplo.
///
/// Regras de negócio:
/// - Cada ambiente pertence a exatamente um tenant.
/// - Slug deve ser único dentro do tenant (validação no Application/Infrastructure).
/// - SortOrder define a ordenação visual e indica o nível de restrição (maior = mais restrito).
/// - Ambiente nunca é excluído fisicamente — apenas desativado.
/// </summary>
public sealed class Environment : Entity<EnvironmentId>
{
    private Environment() { }

    /// <summary>Tenant ao qual o ambiente pertence.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Nome de exibição do ambiente (ex.: "Production").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Identificador URL-friendly e único dentro do tenant (ex.: "production").</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Ordem de exibição — valor mais alto indica ambiente mais restrito.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Indica se o ambiente está ativo e disponível para uso.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação do ambiente.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Factory method para criação de um novo ambiente.
    /// Garante que nome, slug e tenant são informados.
    /// Slug é normalizado para lowercase.
    /// </summary>
    /// <param name="tenantId">Tenant proprietário do ambiente.</param>
    /// <param name="name">Nome de exibição do ambiente.</param>
    /// <param name="slug">Slug único dentro do tenant (será normalizado para lowercase).</param>
    /// <param name="sortOrder">Ordem de exibição (0 = menos restrito).</param>
    /// <param name="now">Data/hora UTC atual fornecida pelo IDateTimeProvider.</param>
    /// <returns>Nova instância de Environment ativa.</returns>
    public static Environment Create(
        TenantId tenantId,
        string name,
        string slug,
        int sortOrder,
        DateTimeOffset now)
    {
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(name, message: "Environment name is required.");
        Guard.Against.NullOrWhiteSpace(slug, message: "Environment slug is required.");
        Guard.Against.Negative(sortOrder, message: "Environment sort order must be zero or positive.");

        return new Environment
        {
            Id = EnvironmentId.New(),
            TenantId = tenantId,
            Name = name,
            Slug = slug.ToLowerInvariant(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = now
        };
    }

    /// <summary>Ativa o ambiente, tornando-o disponível para atribuição de acessos.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Desativa o ambiente, impedindo novos acessos e operações.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Atualiza a ordem de exibição do ambiente.</summary>
    /// <param name="sortOrder">Novo valor de ordenação (deve ser zero ou positivo).</param>
    public void UpdateSortOrder(int sortOrder)
    {
        Guard.Against.Negative(sortOrder, message: "Environment sort order must be zero or positive.");
        SortOrder = sortOrder;
    }
}

/// <summary>Identificador fortemente tipado de Environment.</summary>
public sealed record EnvironmentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EnvironmentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EnvironmentId From(Guid id) => new(id);
}
