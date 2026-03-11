namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração do provedor de data/hora para testes determinísticos.
/// Em produção: DateTimeOffset.UtcNow. Em testes: valor fixo.
/// REGRA: Nunca use DateTime.Now diretamente nos handlers.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Data/hora atual em UTC.</summary>
    DateTimeOffset UtcNow { get; }
    /// <summary>Data atual em UTC (sem componente de hora).</summary>
    DateOnly UtcToday { get; }
}
