using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma campanha de recertificação de acessos.
///
/// Processo trimestral automático:
/// 1. Sistema lista usuários e permissões ativas no tenant.
/// 2. Managers recebem notificação para revisar os itens.
/// 3. Manager confirma, remove ou ajusta cada item.
/// 4. Permissões não revisadas em X dias são revogadas automaticamente.
/// 5. Trilha completa para auditoria de compliance (SOX, DORA, ISO 27001, BACEN 4.893).
///
/// Cada campanha contém AccessReviewItems individuais para cada usuário/role/permissão a revisar.
/// </summary>
public sealed class AccessReviewCampaign : AggregateRoot<AccessReviewCampaignId>
{
    /// <summary>Prazo padrão para revisão antes de auto-revogação.</summary>
    public static readonly TimeSpan DefaultReviewDeadline = TimeSpan.FromDays(14);

    private readonly List<AccessReviewItem> _items = [];

    private AccessReviewCampaign() { }

    /// <summary>Tenant ao qual a campanha se aplica.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Nome descritivo da campanha (e.g., "Q1 2025 Access Review").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Estado atual da campanha.</summary>
    public AccessReviewCampaignStatus Status { get; private set; }

    /// <summary>Data/hora UTC de criação da campanha.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC limite para revisão dos itens.</summary>
    public DateTimeOffset Deadline { get; private set; }

    /// <summary>Data/hora UTC de conclusão da campanha.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Usuário ou processo que iniciou a campanha.</summary>
    public UserId? InitiatedBy { get; private set; }

    /// <summary>Itens individuais de revisão da campanha.</summary>
    public IReadOnlyList<AccessReviewItem> Items => _items.AsReadOnly();

    /// <summary>Concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria uma nova campanha de recertificação de acessos.</summary>
    public static AccessReviewCampaign Create(
        TenantId tenantId,
        string name,
        UserId? initiatedBy,
        DateTimeOffset now,
        TimeSpan? reviewWindow = null)
    {
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(name);

        return new AccessReviewCampaign
        {
            Id = AccessReviewCampaignId.New(),
            TenantId = tenantId,
            Name = name,
            Status = AccessReviewCampaignStatus.Open,
            CreatedAt = now,
            Deadline = now.Add(reviewWindow ?? DefaultReviewDeadline),
            InitiatedBy = initiatedBy
        };
    }

    /// <summary>
    /// Adiciona um item de revisão à campanha.
    /// Cada item representa uma combinação usuário/role que precisa ser revisada.
    /// </summary>
    public AccessReviewItem AddItem(
        UserId userId,
        RoleId roleId,
        string roleName,
        UserId reviewerId)
    {
        Guard.Against.Null(userId);
        Guard.Against.Null(roleId);
        Guard.Against.NullOrWhiteSpace(roleName);
        Guard.Against.Null(reviewerId);

        var item = AccessReviewItem.Create(Id, userId, roleId, roleName, reviewerId);
        _items.Add(item);
        return item;
    }

    /// <summary>Verifica se todos os itens foram revisados e fecha a campanha.</summary>
    public void TryComplete(DateTimeOffset now)
    {
        if (Status != AccessReviewCampaignStatus.Open)
            return;

        if (_items.All(i => i.Decision != AccessReviewDecision.Pending))
        {
            Status = AccessReviewCampaignStatus.Completed;
            CompletedAt = now;
        }
    }

    /// <summary>
    /// Processa itens não revisados após o prazo: auto-revoga e fecha a campanha.
    /// Chamado pelo job periódico de recertificação.
    /// </summary>
    public void ProcessDeadline(DateTimeOffset now)
    {
        if (Status != AccessReviewCampaignStatus.Open)
            return;

        if (now < Deadline)
            return;

        foreach (var item in _items.Where(i => i.Decision == AccessReviewDecision.Pending))
        {
            item.AutoRevoke(now);
        }

        Status = AccessReviewCampaignStatus.Completed;
        CompletedAt = now;
    }
}

/// <summary>Estados possíveis de uma campanha de revisão de acessos.</summary>
public enum AccessReviewCampaignStatus
{
    /// <summary>Campanha aberta, aguardando revisão dos itens.</summary>
    Open = 0,

    /// <summary>Campanha concluída (todos os itens revisados ou prazo expirado).</summary>
    Completed = 1
}

/// <summary>Identificador fortemente tipado de AccessReviewCampaign.</summary>
public sealed record AccessReviewCampaignId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AccessReviewCampaignId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AccessReviewCampaignId From(Guid id) => new(id);
}
