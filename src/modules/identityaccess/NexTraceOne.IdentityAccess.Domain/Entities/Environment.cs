using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.IdentityAccess.Domain.Enums;

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

    /// <summary>
    /// Perfil operacional do ambiente.
    /// Define a natureza base do ambiente (Development, Validation, Staging, Production, etc.)
    /// independente do nome livre dado pelo tenant.
    /// </summary>
    public EnvironmentProfile Profile { get; private set; } = EnvironmentProfile.Development;

    /// <summary>
    /// Código curto definido pelo tenant para identificação rápida (ex.: "DEV", "QA-EU", "PROD-BR").
    /// Diferente do Slug (que é URL-friendly), o Code é livre e de uso interno.
    /// </summary>
    public string? Code { get; private set; }

    /// <summary>
    /// Descrição livre do ambiente, usada para fins informativos e de documentação interna.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Criticidade operacional do ambiente.
    /// Determina nível de proteção, auditoria e rigor em mudanças.
    /// </summary>
    public EnvironmentCriticality Criticality { get; private set; } = EnvironmentCriticality.Low;

    /// <summary>
    /// Região ou localização do ambiente (ex.: "br-east", "eu-west-1", "on-prem-sp").
    /// Informação opcional para contexto geográfico e roteamento.
    /// </summary>
    public string? Region { get; private set; }

    /// <summary>
    /// Indica se este ambiente tem comportamento e políticas similares ao de produção.
    /// Verdadeiro para Production, DisasterRecovery e ambientes de alta criticidade.
    /// Influencia decisões de IA, políticas de promoção e auditoria.
    /// </summary>
    public bool IsProductionLike { get; private set; }

    /// <summary>
    /// Indica se este é o ambiente produtivo principal do tenant.
    /// Cada tenant deve ter no máximo um ambiente marcado como produção principal ativa.
    /// A regra de unicidade é garantida por índice parcial no banco de dados:
    /// apenas um ambiente ativo com IsPrimaryProduction=true por TenantId.
    ///
    /// Este campo é a fonte de verdade para o conceito de "ambiente de produção" no tenant —
    /// usado pela IA para comparação de ambientes não produtivos, análise de risco de release
    /// e avaliação de readiness para promoção.
    /// </summary>
    public bool IsPrimaryProduction { get; private set; }

    /// <summary>Data/hora UTC de criação do ambiente.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador do utilizador que criou o ambiente.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>Data/hora UTC da última atualização do ambiente.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Identificador do utilizador que realizou a última atualização.</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>Indica se o ambiente foi removido logicamente (soft-delete).</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

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

    /// <summary>
    /// Factory method completo para criação de ambiente com perfil operacional explícito.
    /// Permite configurar o perfil operacional, criticidade e demais atributos da Fase 1.
    /// </summary>
    public static Environment Create(
        TenantId tenantId,
        string name,
        string slug,
        int sortOrder,
        DateTimeOffset now,
        EnvironmentProfile profile,
        EnvironmentCriticality criticality = EnvironmentCriticality.Low,
        string? code = null,
        string? description = null,
        string? region = null,
        bool? isProductionLike = null,
        bool isPrimaryProduction = false)
    {
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(name, message: "Environment name is required.");
        Guard.Against.NullOrWhiteSpace(slug, message: "Environment slug is required.");
        Guard.Against.Negative(sortOrder, message: "Environment sort order must be zero or positive.");

        var productionLike = isProductionLike ?? profile is EnvironmentProfile.Production or EnvironmentProfile.DisasterRecovery;

        return new Environment
        {
            Id = EnvironmentId.New(),
            TenantId = tenantId,
            Name = name,
            Slug = slug.ToLowerInvariant(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = now,
            Profile = profile,
            Criticality = criticality,
            Code = code,
            Description = description,
            Region = region,
            IsProductionLike = productionLike,
            IsPrimaryProduction = isPrimaryProduction
        };
    }

    /// <summary>Atualiza o perfil operacional do ambiente.</summary>
    public void UpdateProfile(EnvironmentProfile profile, EnvironmentCriticality criticality, bool? isProductionLike = null)
    {
        Profile = profile;
        Criticality = criticality;
        IsProductionLike = isProductionLike ?? profile is EnvironmentProfile.Production or EnvironmentProfile.DisasterRecovery;
    }

    /// <summary>Atualiza o código curto e a região do ambiente.</summary>
    public void UpdateLocationInfo(string? code, string? region, string? description)
    {
        Code = code;
        Region = region;
        Description = description;
    }

    /// <summary>Atualiza o nome e a ordem do ambiente.</summary>
    public void UpdateBasicInfo(string name, int sortOrder)
    {
        Guard.Against.NullOrWhiteSpace(name, message: "Environment name is required.");
        Guard.Against.Negative(sortOrder, message: "Environment sort order must be zero or positive.");
        Name = name;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Designa este ambiente como o ambiente de produção principal do tenant.
    /// O ambiente deve estar ativo para poder ser designado como produção principal.
    /// O isolamento de unicidade é garantido a nível de banco de dados.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o ambiente não estiver ativo.</exception>
    public void DesignateAsPrimaryProduction()
    {
        if (!IsActive)
            throw new InvalidOperationException("An inactive environment cannot be designated as the primary production environment.");
        IsPrimaryProduction = true;
    }

    /// <summary>Remove a designação de produção principal deste ambiente.</summary>
    public void RevokePrimaryProductionDesignation() => IsPrimaryProduction = false;

    /// <summary>
    /// Desativa o ambiente, impedindo novos acessos e operações.
    /// Um ambiente marcado como produção principal não pode ser desativado diretamente.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o ambiente for produção principal.</exception>
    public void Deactivate()
    {
        if (IsPrimaryProduction)
            throw new InvalidOperationException("Cannot deactivate the primary production environment. Remove the primary production designation first.");

        IsActive = false;
    }

    /// <summary>Atualiza a ordem de exibição do ambiente.</summary>
    /// <param name="sortOrder">Novo valor de ordenação (deve ser zero ou positivo).</param>
    public void UpdateSortOrder(int sortOrder)
    {
        Guard.Against.Negative(sortOrder, message: "Environment sort order must be zero or positive.");
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Marca o registo de atualização com utilizador e timestamp.
    /// Deve ser chamado pelo handler após qualquer mutação.
    /// </summary>
    public void SetUpdated(string updatedBy, DateTimeOffset now)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }

    /// <summary>
    /// Remove logicamente o ambiente (soft-delete).
    /// Um ambiente marcado como produção principal não pode ser removido.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se o ambiente for produção principal.</exception>
    public void SoftDelete()
    {
        if (IsPrimaryProduction)
            throw new InvalidOperationException("Cannot delete the primary production environment.");

        IsDeleted = true;
        IsActive = false;
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
