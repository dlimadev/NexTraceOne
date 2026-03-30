using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Janela de freeze que restringe ou eleva risco de mudanças em períodos críticos.
/// Mesmo sem controlar deploy tecnicamente, o módulo deve:
/// - saber que a janela é crítica
/// - alertar
/// - elevar risco
/// - registrar exceções
/// - alimentar decisão e auditoria
/// Suporta freeze global, por tenant, domínio, ambiente ou serviço.
/// </summary>
public sealed class FreezeWindow : AggregateRoot<FreezeWindowId>
{
    /// <summary>Nome descritivo da janela de freeze (ex: "Black Friday 2026").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Justificativa de negócio para o freeze.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Escopo de aplicação do freeze.</summary>
    public FreezeScope Scope { get; private set; }

    /// <summary>Valor do escopo quando aplicável (ID do tenant, nome do domínio, nome do ambiente, etc.).</summary>
    public string? ScopeValue { get; private set; }

    /// <summary>Início da janela de freeze (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Fim da janela de freeze (UTC).</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Indica se o freeze está ativo (pode ser desativado antes do término).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Criador da janela de freeze.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Momento de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private FreezeWindow() { }

    /// <summary>
    /// Cria uma nova janela de freeze com validação de datas e escopo.
    /// </summary>
    public static FreezeWindow Create(
        string name,
        string reason,
        FreezeScope scope,
        string? scopeValue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string createdBy,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));

        if (endsAt <= startsAt)
            throw new ArgumentException("Freeze window end must be after start.");

        if (scope != FreezeScope.Global && string.IsNullOrWhiteSpace(scopeValue))
            throw new ArgumentException("Scope value is required for non-global freeze windows.");

        return new FreezeWindow
        {
            Id = FreezeWindowId.New(),
            Name = name,
            Reason = reason,
            Scope = scope,
            ScopeValue = scopeValue,
            StartsAt = startsAt,
            EndsAt = endsAt,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Verifica se a janela de freeze está em vigor num determinado momento.
    /// </summary>
    public bool IsInEffectAt(DateTimeOffset at) =>
        IsActive && at >= StartsAt && at <= EndsAt;

    /// <summary>
    /// Atualiza os dados editáveis da janela de freeze.
    /// </summary>
    public Result<Unit> Update(
        string name,
        string reason,
        FreezeScope scope,
        string? scopeValue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (endsAt <= startsAt)
            return Error.Validation(
                "change_intelligence.freeze.invalid_dates",
                "Freeze window end must be after start.");

        if (scope != FreezeScope.Global && string.IsNullOrWhiteSpace(scopeValue))
            return Error.Validation(
                "change_intelligence.freeze.scope_value_required",
                "Scope value is required for non-global freeze windows.");

        Name = name;
        Reason = reason;
        Scope = scope;
        ScopeValue = scopeValue;
        StartsAt = startsAt;
        EndsAt = endsAt;
        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Desativa a janela de freeze antes do término previsto.
    /// </summary>
    public Result<Unit> Deactivate()
    {
        if (!IsActive)
            return Error.Conflict(
                "change_intelligence.freeze.already_inactive",
                "Freeze window is already inactive.");

        IsActive = false;
        return Result<Unit>.Success(Unit.Value);
    }
}

/// <summary>Identificador fortemente tipado para FreezeWindow.</summary>
public sealed record FreezeWindowId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static FreezeWindowId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static FreezeWindowId From(Guid id) => new(id);
}
