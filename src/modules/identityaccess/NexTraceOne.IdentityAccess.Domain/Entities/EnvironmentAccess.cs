using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Entidade que representa o acesso de um usuário a um ambiente específico dentro de um tenant.
///
/// Permite granularidade de autorização por ambiente — por exemplo, um desenvolvedor
/// pode ter acesso "write" em Development mas apenas "read" em Production.
///
/// Regras de negócio:
/// - Cada registro vincula um usuário, um tenant e um ambiente a um nível de acesso.
/// - Unicidade: apenas um registro ativo por combinação (UserId, TenantId, EnvironmentId).
/// - Acesso pode ter data de expiração (grants temporários).
/// - Acesso inativo (revogado ou expirado) não concede permissões.
/// - Toda concessão registra quem concedeu (GrantedBy) para auditoria.
/// </summary>
public sealed class EnvironmentAccess : Entity<EnvironmentAccessId>
{
    private EnvironmentAccess() { }

    /// <summary>Usuário que possui o acesso ao ambiente.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Tenant no qual o acesso é válido.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Ambiente ao qual o acesso se refere.</summary>
    public EnvironmentId EnvironmentId { get; private set; } = null!;

    /// <summary>
    /// Nível de acesso concedido ao usuário no ambiente.
    /// Valores válidos definidos em <see cref="EnvironmentAccessLevel"/>.
    /// </summary>
    public string AccessLevel { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o acesso foi concedido.</summary>
    public DateTimeOffset GrantedAt { get; private set; }

    /// <summary>Data/hora UTC de expiração do acesso (null = sem expiração).</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Usuário que concedeu o acesso (para trilha de auditoria).</summary>
    public UserId GrantedBy { get; private set; } = null!;

    /// <summary>Indica se o acesso está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC em que o acesso foi revogado (null se ainda ativo).</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Factory method para criação de um novo acesso a ambiente.
    /// Valida que todos os campos obrigatórios estão preenchidos e que o nível de acesso é válido.
    /// Se ExpiresAt for informado, deve ser posterior a GrantedAt.
    /// </summary>
    /// <param name="userId">Usuário que receberá o acesso.</param>
    /// <param name="tenantId">Tenant no qual o acesso é válido.</param>
    /// <param name="environmentId">Ambiente alvo do acesso.</param>
    /// <param name="accessLevel">Nível de acesso (usar constantes de <see cref="EnvironmentAccessLevel"/>).</param>
    /// <param name="grantedBy">Usuário que está concedendo o acesso.</param>
    /// <param name="now">Data/hora UTC atual fornecida pelo IDateTimeProvider.</param>
    /// <param name="expiresAt">Data/hora UTC de expiração opcional.</param>
    /// <returns>Nova instância de EnvironmentAccess ativa.</returns>
    public static EnvironmentAccess Create(
        UserId userId,
        TenantId tenantId,
        EnvironmentId environmentId,
        string accessLevel,
        UserId grantedBy,
        DateTimeOffset now,
        DateTimeOffset? expiresAt = null)
    {
        Guard.Against.Null(userId);
        Guard.Against.Null(tenantId);
        Guard.Against.Null(environmentId);
        Guard.Against.NullOrWhiteSpace(accessLevel, message: "Access level is required.");
        Guard.Against.Null(grantedBy);

        if (!EnvironmentAccessLevel.IsValid(accessLevel))
            throw new InvalidOperationException($"Access level '{accessLevel}' is not valid. Valid levels: {string.Join(", ", EnvironmentAccessLevel.All)}.");

        if (expiresAt.HasValue && expiresAt.Value <= now)
            throw new InvalidOperationException("Expiration date must be in the future.");

        return new EnvironmentAccess
        {
            Id = EnvironmentAccessId.New(),
            UserId = userId,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            AccessLevel = accessLevel,
            GrantedBy = grantedBy,
            GrantedAt = now,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    /// <summary>
    /// Revoga o acesso ao ambiente, desativando-o e registrando a data de revogação.
    /// </summary>
    /// <param name="now">Data/hora UTC da revogação, fornecida pelo IDateTimeProvider.</param>
    public void Revoke(DateTimeOffset now)
    {
        IsActive = false;
        RevokedAt = now;
    }

    /// <summary>
    /// Verifica se o acesso está efetivamente ativo na data informada.
    /// Considera tanto o flag IsActive quanto a data de expiração.
    /// </summary>
    /// <param name="now">Data/hora UTC a ser verificada.</param>
    /// <returns>True se o acesso está ativo e não expirado.</returns>
    public bool IsActiveAt(DateTimeOffset now)
        => IsActive && (!ExpiresAt.HasValue || now < ExpiresAt.Value);

    /// <summary>
    /// Altera o nível de acesso do usuário no ambiente.
    /// Valida que o novo nível é um valor reconhecido.
    /// </summary>
    /// <param name="newLevel">Novo nível de acesso (usar constantes de <see cref="EnvironmentAccessLevel"/>).</param>
    public void ChangeAccessLevel(string newLevel)
    {
        Guard.Against.NullOrWhiteSpace(newLevel, message: "Access level is required.");

        if (!EnvironmentAccessLevel.IsValid(newLevel))
            throw new InvalidOperationException($"Access level '{newLevel}' is not valid. Valid levels: {string.Join(", ", EnvironmentAccessLevel.All)}.");

        AccessLevel = newLevel;
    }
}

/// <summary>Identificador fortemente tipado de EnvironmentAccess.</summary>
public sealed record EnvironmentAccessId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EnvironmentAccessId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EnvironmentAccessId From(Guid id) => new(id);
}

/// <summary>
/// Constantes para os níveis de acesso válidos em um ambiente.
/// Usados para validação e comparação consistente no domínio.
/// </summary>
public static class EnvironmentAccessLevel
{
    /// <summary>Acesso somente leitura ao ambiente.</summary>
    public const string Read = "read";

    /// <summary>Acesso de leitura e escrita ao ambiente.</summary>
    public const string Write = "write";

    /// <summary>Acesso administrativo ao ambiente (inclui leitura e escrita).</summary>
    public const string Admin = "admin";

    /// <summary>Sem acesso ao ambiente (bloqueio explícito).</summary>
    public const string None = "none";

    /// <summary>Lista de todos os níveis válidos para validação.</summary>
    public static readonly IReadOnlyList<string> All = [Read, Write, Admin, None];

    /// <summary>Verifica se o nível informado é válido.</summary>
    /// <param name="level">Nível de acesso a ser verificado.</param>
    /// <returns>True se o nível é reconhecido.</returns>
    public static bool IsValid(string level) => All.Contains(level);
}
