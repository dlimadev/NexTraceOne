using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

/// <summary>
/// Informação de agendamento de um batch job.
/// Encapsula tipo, frequência e janela operacional.
/// </summary>
public sealed class JobSchedule : ValueObject
{
    private JobSchedule() { }

    /// <summary>Tipo de agendamento (Daily, Weekly, Monthly, OnDemand, Event).</summary>
    public string ScheduleType { get; private set; } = string.Empty;

    /// <summary>Frequência de execução (ex: "Every 4 hours", "Daily at 02:00").</summary>
    public string Frequency { get; private set; } = string.Empty;

    /// <summary>Janela operacional permitida (ex: "02:00-06:00 UTC").</summary>
    public string Window { get; private set; } = string.Empty;

    /// <summary>Expressão cron quando aplicável.</summary>
    public string CronExpression { get; private set; } = string.Empty;

    /// <summary>
    /// Cria uma nova definição de agendamento validada.
    /// </summary>
    public static JobSchedule Create(
        string scheduleType,
        string? frequency = null,
        string? window = null,
        string? cronExpression = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleType);

        return new JobSchedule
        {
            ScheduleType = scheduleType.Trim(),
            Frequency = frequency?.Trim() ?? string.Empty,
            Window = window?.Trim() ?? string.Empty,
            CronExpression = cronExpression?.Trim() ?? string.Empty
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ScheduleType;
        yield return Frequency;
        yield return Window;
        yield return CronExpression;
    }
}
