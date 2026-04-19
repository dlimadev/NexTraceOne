using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para NonProdSchedule.</summary>
public sealed record NonProdScheduleId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agenda de ambiente não produtivo para otimização de custos FinOps.
/// Define janelas de atividade e estimativa de poupança por ambiente.
/// </summary>
public sealed class NonProdSchedule : Entity<NonProdScheduleId>
{
    private NonProdSchedule() { }

    /// <summary>Identificador de negócio do ambiente (ex: "staging", "qa").</summary>
    public string EnvironmentId { get; private set; } = string.Empty;

    /// <summary>Nome de exibição do ambiente.</summary>
    public string EnvironmentName { get; private set; } = string.Empty;

    /// <summary>Indica se a agenda está activa.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Dias da semana em que o ambiente deve estar activo (JSON array).</summary>
    public string ActiveDaysOfWeekJson { get; private set; } = "[]";

    /// <summary>Hora de início da janela de actividade (0-23).</summary>
    public int ActiveFromHour { get; private set; }

    /// <summary>Hora de fim da janela de actividade (0-23).</summary>
    public int ActiveToHour { get; private set; }

    /// <summary>Timezone da agenda (ex: "UTC", "Europe/Lisbon").</summary>
    public string Timezone { get; private set; } = "UTC";

    /// <summary>Estimativa de poupança em percentagem (0-100).</summary>
    public int EstimatedSavingPct { get; private set; }

    /// <summary>Override temporário: manter activo até esta data.</summary>
    public DateTimeOffset? KeepActiveUntil { get; private set; }

    /// <summary>Motivo do override temporário.</summary>
    public string? OverrideReason { get; private set; }

    /// <summary>Identificador do tenant (multi-tenant).</summary>
    public string? TenantId { get; private set; }

    /// <summary>Data de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Cria uma nova agenda de ambiente não produtivo.</summary>
    public static NonProdSchedule Create(
        string environmentId,
        string environmentName,
        bool enabled,
        IReadOnlyList<string> activeDaysOfWeek,
        int activeFromHour,
        int activeToHour,
        string timezone,
        int estimatedSavingPct,
        DateTimeOffset now,
        string? tenantId = null)
    {
        Guard.Against.NullOrWhiteSpace(environmentId);
        Guard.Against.NullOrWhiteSpace(environmentName);
        Guard.Against.NullOrWhiteSpace(timezone);
        Guard.Against.OutOfRange(activeFromHour, nameof(activeFromHour), 0, 23);
        Guard.Against.OutOfRange(activeToHour, nameof(activeToHour), 0, 23);
        Guard.Against.OutOfRange(estimatedSavingPct, nameof(estimatedSavingPct), 0, 100);

        return new NonProdSchedule
        {
            Id = new NonProdScheduleId(Guid.NewGuid()),
            EnvironmentId = environmentId.Trim(),
            EnvironmentName = environmentName.Trim(),
            Enabled = enabled,
            ActiveDaysOfWeekJson = System.Text.Json.JsonSerializer.Serialize(activeDaysOfWeek),
            ActiveFromHour = activeFromHour,
            ActiveToHour = activeToHour,
            Timezone = timezone.Trim(),
            EstimatedSavingPct = estimatedSavingPct,
            TenantId = tenantId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Atualiza a agenda do ambiente.</summary>
    public void Update(
        bool enabled,
        IReadOnlyList<string> activeDaysOfWeek,
        int activeFromHour,
        int activeToHour,
        string timezone,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(timezone);
        Guard.Against.OutOfRange(activeFromHour, nameof(activeFromHour), 0, 23);
        Guard.Against.OutOfRange(activeToHour, nameof(activeToHour), 0, 23);

        Enabled = enabled;
        ActiveDaysOfWeekJson = System.Text.Json.JsonSerializer.Serialize(activeDaysOfWeek);
        ActiveFromHour = activeFromHour;
        ActiveToHour = activeToHour;
        Timezone = timezone.Trim();
        UpdatedAt = now;
    }

    /// <summary>Aplica override temporário para manter o ambiente activo.</summary>
    public void ApplyOverride(DateTimeOffset keepActiveUntil, string reason, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(reason);
        KeepActiveUntil = keepActiveUntil;
        OverrideReason = reason;
        UpdatedAt = now;
    }

    /// <summary>Remove o override temporário.</summary>
    public void ClearOverride(DateTimeOffset now)
    {
        KeepActiveUntil = null;
        OverrideReason = null;
        UpdatedAt = now;
    }
}
