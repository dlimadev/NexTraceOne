using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Aggregate Root que representa uma entrada no Release Calendar.
/// Define janelas de deployment, congelamento, hotfix e manutenção por tenant/ambiente.
/// Utilizado pelo IsChangeWindowOpen para validar mudanças em produção.
/// Wave F.1 — Release Calendar.
/// </summary>
public sealed class ReleaseCalendarEntry : AuditableEntity<ReleaseCalendarEntryId>
{
    private ReleaseCalendarEntry() { }

    /// <summary>Identificador do tenant proprietário da janela.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Nome descritivo da janela (ex: "Q4 Production Freeze").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional da janela.</summary>
    public string? Description { get; private set; }

    /// <summary>Tipo de janela — controla se mudanças são permitidas ou bloqueadas.</summary>
    public ReleaseWindowType WindowType { get; private set; }

    /// <summary>Estado de ciclo de vida da janela.</summary>
    public ReleaseWindowStatus Status { get; private set; }

    /// <summary>Filtro de ambiente (ex: "production", "staging") — null = todos.</summary>
    public string? EnvironmentFilter { get; private set; }

    /// <summary>Início da janela (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Fim da janela (UTC).</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Tag de recorrência opcional (ex: "Q4-freeze", "weekend-maintenance").</summary>
    public string? RecurrenceTag { get; private set; }

    /// <summary>Utilizador que encerrou ou cancelou a janela.</summary>
    public string? ClosedByUserId { get; private set; }

    /// <summary>Data/hora em que a janela foi encerrada ou cancelada.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria uma nova entrada no Release Calendar.
    /// </summary>
    public static Result<ReleaseCalendarEntry> Register(
        string tenantId,
        string name,
        ReleaseWindowType windowType,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? environmentFilter = null,
        string? description = null,
        string? recurrenceTag = null)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);

        if (endsAt <= startsAt)
            return Error.Validation("release_calendar.invalid_window",
                "EndsAt must be after StartsAt.");

        var entry = new ReleaseCalendarEntry
        {
            Id = ReleaseCalendarEntryId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            WindowType = windowType,
            Status = ReleaseWindowStatus.Active,
            EnvironmentFilter = environmentFilter?.Trim().ToLowerInvariant(),
            StartsAt = startsAt,
            EndsAt = endsAt,
            RecurrenceTag = recurrenceTag?.Trim()
        };

        return Result<ReleaseCalendarEntry>.Success(entry);
    }

    // ── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>
    /// Encerra a janela antes do fim previsto.
    /// </summary>
    public Result<Unit> Close(string closedByUserId, DateTimeOffset now)
    {
        if (Status != ReleaseWindowStatus.Active)
            return Error.Business("release_calendar.already_closed",
                "Window is not in Active status.");

        Status = ReleaseWindowStatus.Closed;
        ClosedByUserId = closedByUserId;
        ClosedAt = now;
        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Cancela a janela antes de entrar em vigor.
    /// </summary>
    public Result<Unit> Cancel(string cancelledByUserId, DateTimeOffset now)
    {
        if (Status != ReleaseWindowStatus.Active)
            return Error.Business("release_calendar.already_closed",
                "Window is not in Active status.");

        if (now >= StartsAt)
            return Error.Business("release_calendar.window_already_started",
                "Cannot cancel a window that has already started. Use Close instead.");

        Status = ReleaseWindowStatus.Cancelled;
        ClosedByUserId = cancelledByUserId;
        ClosedAt = now;
        return Result<Unit>.Success(Unit.Value);
    }

    /// <summary>
    /// Verifica se esta janela está activa num determinado momento e ambiente.
    /// </summary>
    public bool IsActiveAt(DateTimeOffset moment, string? environment = null)
    {
        if (Status != ReleaseWindowStatus.Active)
            return false;

        if (moment < StartsAt || moment > EndsAt)
            return false;

        if (EnvironmentFilter is not null && environment is not null)
            return string.Equals(EnvironmentFilter, environment.Trim().ToLowerInvariant(),
                StringComparison.OrdinalIgnoreCase);

        return true;
    }

    /// <summary>
    /// Indica se esta janela bloqueia mudanças (Freeze ou Maintenance).
    /// </summary>
    public bool BlocksChanges => WindowType is ReleaseWindowType.Freeze or ReleaseWindowType.Maintenance;

    /// <summary>
    /// Indica se esta janela permite apenas hotfixes aprovados.
    /// </summary>
    public bool IsHotfixOnly => WindowType is ReleaseWindowType.HotfixAllowed;
}

/// <summary>Strongly-typed ID para ReleaseCalendarEntry.</summary>
public sealed record ReleaseCalendarEntryId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria um novo ID único.</summary>
    public static ReleaseCalendarEntryId New() => new(Guid.NewGuid());

    /// <summary>Cria a partir de um Guid.</summary>
    public static ReleaseCalendarEntryId From(Guid value) => new(value);
}
