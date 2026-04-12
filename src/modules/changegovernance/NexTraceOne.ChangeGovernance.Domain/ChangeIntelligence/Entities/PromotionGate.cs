using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Gate de promoção configurável que define regras para autorizar a promoção
/// de uma mudança entre ambientes (ex.: Dev → Staging, Staging → Production).
/// Cada gate contém um conjunto de regras serializadas em JSONB e pode ser
/// ativado ou desativado sem remoção, garantindo rastreabilidade e auditoria.
/// </summary>
public sealed class PromotionGate : Entity<PromotionGateId>
{
    private PromotionGate() { }

    /// <summary>Nome descritivo do gate (ex.: "Staging → Production Gate").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do propósito e critérios do gate.</summary>
    public string? Description { get; private set; }

    /// <summary>Ambiente de origem da promoção (ex.: "staging").</summary>
    public string EnvironmentFrom { get; private set; } = string.Empty;

    /// <summary>Ambiente de destino da promoção (ex.: "production").</summary>
    public string EnvironmentTo { get; private set; } = string.Empty;

    /// <summary>Regras do gate serializadas em formato JSON (JSONB no banco).</summary>
    public string? Rules { get; private set; }

    /// <summary>Indica se o gate está ativo e será avaliado em promoções.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica se a falha neste gate bloqueia a promoção.</summary>
    public bool BlockOnFailure { get; private set; }

    /// <summary>Identificador do utilizador ou sistema que criou o gate.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>Momento de criação do gate.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador do tenant ao qual o gate pertence.</summary>
    public string? TenantId { get; private set; }

    /// <summary>Token de concorrência otimista (xmin no PostgreSQL).</summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Cria um novo gate de promoção com validação de campos obrigatórios.
    /// </summary>
    public static PromotionGate Create(
        string name,
        string? description,
        string environmentFrom,
        string environmentTo,
        string? rules,
        bool blockOnFailure,
        string? createdBy,
        DateTimeOffset createdAt,
        string? tenantId)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 200, nameof(name));
        Guard.Against.NullOrWhiteSpace(environmentFrom, nameof(environmentFrom));
        Guard.Against.StringTooLong(environmentFrom, 100, nameof(environmentFrom));
        Guard.Against.NullOrWhiteSpace(environmentTo, nameof(environmentTo));
        Guard.Against.StringTooLong(environmentTo, 100, nameof(environmentTo));

        if (description is not null)
            Guard.Against.StringTooLong(description, 2000, nameof(description));

        if (createdBy is not null)
            Guard.Against.StringTooLong(createdBy, 200, nameof(createdBy));

        return new PromotionGate
        {
            Id = PromotionGateId.New(),
            Name = name,
            Description = description,
            EnvironmentFrom = environmentFrom,
            EnvironmentTo = environmentTo,
            Rules = rules,
            IsActive = true,
            BlockOnFailure = blockOnFailure,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Desativa o gate de promoção, impedindo que seja avaliado em novas promoções.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reativa o gate de promoção para que volte a ser avaliado em promoções.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Atualiza as regras do gate de promoção (JSONB).
    /// </summary>
    public void UpdateRules(string? rules)
    {
        Rules = rules;
    }
}

/// <summary>Identificador fortemente tipado de PromotionGate.</summary>
public sealed record PromotionGateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromotionGateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromotionGateId From(Guid id) => new(id);
}
